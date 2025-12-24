using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Services.Interfaces;

namespace SEOBoostAI.Service.Services.PerformanceAnalysis
{
    /// <summary>
    /// AutoFix Service - Batch fix all issues từ AnalysisCache
    /// Adapted từ mẫu: AuditSession → AnalysisCache, AuditIssue → Element
    /// </summary>
    public class AutoFixService : IAutoFixService
    {
        private readonly IGitHubIntegrationService _git;
        private readonly IGeminiFixService _ai;
        private readonly IAnalysisCacheRepository _cacheRepo;
        private readonly IElementRepository _elementRepo;
        private readonly IUnitOfWork _unitOfWork;

        public AutoFixService(
            IGitHubIntegrationService git,
            IGeminiFixService ai,
            IAnalysisCacheRepository cacheRepo,
            IElementRepository elementRepo,
            IUnitOfWork unitOfWork)
        {
            _git = git;
            _ai = ai;
            _cacheRepo = cacheRepo;
            _elementRepo = elementRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<BatchFixResponse> BatchFixAsync(BatchFixRequest req)
        {
            var response = new BatchFixResponse();

            // 1. Lấy AnalysisCache và Elements
            var cache = await _cacheRepo.GetByIdAsync(req.AnalysisCacheId);
            if (cache == null)
            {
                throw new Exception($"Không tìm thấy AnalysisCache với ID {req.AnalysisCacheId}");
            }

            var elements = await _elementRepo.GetElementsByAnalysisCacheIdAsync(req.AnalysisCacheId);
            if (elements == null || !elements.Any())
            {
                Console.WriteLine($"[BATCH FIX] No issues found for cache {req.AnalysisCacheId}");
                return response;
            }

            Console.WriteLine($"[BATCH FIX] URL: {cache.Url}, Issues: {elements.Count()}");
            response.TotalIssues = elements.Count();

            // 2. Auto-detect repo structure
            await _git.DetectRepoStructureAsync(req.RepoOwner, req.RepoName);

            // 3. Group elements theo file
            var issuesByFile = new Dictionary<string, List<(Element, List<string>)>>();
            var cachedStructure = _git.GetCachedStructure(req.RepoOwner, req.RepoName);

            foreach (var element in elements)
            {
                var evidences = TryDeserializeEvidences(element.ExtractedEvidenceJson);
                string targetFileType = GetTargetFileTypeForIssue(element.AuditId);

                if (targetFileType == "index.html")
                {
                    string indexPath = cachedStructure?.IndexHtmlPath ?? "index.html";

                    if (string.IsNullOrEmpty(indexPath) || indexPath == "index.html")
                    {
                        var fallbackPaths = cachedStructure != null
                            ? new[] { $"{cachedStructure.SrcRoot?.Replace("/src", "")}/index.html", "client/index.html", "public/index.html", "index.html" }
                            : new[] { "index.html", "public/index.html", "src/index.html" };

                        foreach (var tryPath in fallbackPaths.Where(p => !string.IsNullOrEmpty(p)))
                        {
                            var content = await _git.GetFileContentAsync(req.RepoOwner, req.RepoName, tryPath);
                            if (content != null) { indexPath = tryPath; break; }
                        }
                    }

                    if (!issuesByFile.ContainsKey(indexPath))
                        issuesByFile[indexPath] = new List<(Element, List<string>)>();
                    issuesByFile[indexPath].Add((element, evidences));
                }
                else
                {
                    var evidencesByFile = new Dictionary<string, List<string>>();

                    foreach (var ev in evidences)
                    {
                        if (string.IsNullOrEmpty(ev)) continue;
                        var filePath = await _git.FindFileByEvidenceAsync(req.RepoOwner, req.RepoName, ev);
                        if (filePath != null)
                        {
                            if (!evidencesByFile.ContainsKey(filePath))
                                evidencesByFile[filePath] = new List<string>();
                            evidencesByFile[filePath].Add(ev);
                        }
                    }

                    if (evidencesByFile.Count == 0)
                    {
                        string fallbackPath = "index.html";
                        try
                        {
                            var uri = new Uri(cache.Url);
                            var urlPath = await _git.FindFileByUrlPathAsync(req.RepoOwner, req.RepoName, uri.AbsolutePath);
                            if (urlPath != null) fallbackPath = urlPath;
                        }
                        catch { }
                        evidencesByFile[fallbackPath] = evidences;
                    }

                    foreach (var kvp in evidencesByFile)
                    {
                        if (!issuesByFile.ContainsKey(kvp.Key))
                            issuesByFile[kvp.Key] = new List<(Element, List<string>)>();
                        issuesByFile[kvp.Key].Add((element, kvp.Value));
                    }
                }
            }

            Console.WriteLine($"[BATCH FIX] Grouped into {issuesByFile.Count} files");

            // 4. Xử lý từng file
            var fileFixMap = new Dictionary<string, string>();

            foreach (var fileGroup in issuesByFile)
            {
                string filePath = fileGroup.Key;
                var fileIssues = fileGroup.Value;

                Console.WriteLine($"[BATCH FIX] Processing {filePath} with {fileIssues.Count} issues");

                try
                {
                    string code = await _git.GetFileContentAsync(req.RepoOwner, req.RepoName, filePath);
                    if (string.IsNullOrEmpty(code))
                    {
                        foreach (var (element, _) in fileIssues)
                        {
                            response.Results.Add(new FixResult
                            {
                                ElementId = element.ElementID,
                                AuditId = element.AuditId,
                                Title = element.Title,
                                FilePath = filePath,
                                Success = false,
                                ErrorMessage = "Không đọc được file"
                            });
                            response.FailedCount++;
                        }
                        continue;
                    }

                    var issueDescriptions = fileIssues.Select(x =>
                        $"- {x.Item1.Title} ({x.Item1.AuditId}): {string.Join(", ", x.Item2.Take(3))}"
                    ).ToList();

                    string combinedTitle = $"{fileIssues.Count} issues: " +
                        string.Join(", ", fileIssues.Select(x => x.Item1.Title).Distinct());

                    string fixedCode = await _ai.FixCodeAsync(code, combinedTitle, string.Join("\n", issueDescriptions));
                    fileFixMap[filePath] = fixedCode;

                    foreach (var (element, _) in fileIssues)
                    {
                        response.Results.Add(new FixResult
                        {
                            ElementId = element.ElementID,
                            AuditId = element.AuditId,
                            Title = element.Title,
                            FilePath = filePath,
                            Success = true
                        });
                        response.FixedCount++;
                    }

                    Console.WriteLine($"[BATCH FIX] ✓ Fixed {fileIssues.Count} issues in {filePath}");
                }
                catch (Exception ex)
                {
                    foreach (var (element, _) in fileIssues)
                    {
                        response.Results.Add(new FixResult
                        {
                            ElementId = element.ElementID,
                            AuditId = element.AuditId,
                            Title = element.Title,
                            FilePath = filePath,
                            Success = false,
                            ErrorMessage = ex.Message
                        });
                        response.FailedCount++;
                    }
                    Console.WriteLine($"[BATCH FIX] ✗ Failed {filePath}: {ex.Message}");
                }
            }

            // 5. Tạo PR
            if (req.CreateSinglePR && fileFixMap.Count > 0)
            {
                try
                {
                    string prUrl;
                    string prMessage = $"AI Auto-Fix: {response.FixedCount} issues in {fileFixMap.Count} files";

                    // Lấy username của GitHub token owner để so sánh
                    string tokenOwner = await _git.GetCurrentUserLoginAsync();
                    bool isOwnerSameAsTokenOwner = !string.IsNullOrEmpty(tokenOwner) && 
                        tokenOwner.Equals(req.RepoOwner, StringComparison.OrdinalIgnoreCase);

                    if (isOwnerSameAsTokenOwner)
                    {
                        // RepoOwner trùng với Token Owner -> dùng Direct PR (có write access)
                        Console.WriteLine($"[BATCH FIX] Owner '{req.RepoOwner}' = Token Owner '{tokenOwner}' -> Using Direct PR...");
                        prUrl = await _git.CreateBatchPullRequestAsync(req.RepoOwner, req.RepoName, fileFixMap, prMessage);
                    }
                    else if (req.UseForkPR || !isOwnerSameAsTokenOwner)
                    {
                        // RepoOwner khác Token Owner -> phải Fork trước
                        Console.WriteLine($"[BATCH FIX] Owner '{req.RepoOwner}' ≠ Token Owner '{tokenOwner}' -> Using Fork-based PR...");
                        prUrl = await _git.ForkAndCreatePullRequestAsync(req.RepoOwner, req.RepoName, fileFixMap, prMessage);
                    }
                    else
                    {
                        Console.WriteLine($"[BATCH FIX] Using Direct PR (UseForkPR=false)...");
                        prUrl = await _git.CreateBatchPullRequestAsync(req.RepoOwner, req.RepoName, fileFixMap, prMessage);
                    }

                    response.PullRequestUrl = prUrl;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BATCH FIX] PR Error: {ex.Message}");

                    // Fallback: nếu Direct PR thất bại (403, 404) -> thử Fork PR
                    if (ex.Message.Contains("403") || ex.Message.Contains("404") || ex.Message.Contains("Not Found"))
                    {
                        Console.WriteLine($"[BATCH FIX] Fallback to Fork-based PR...");
                        try
                        {
                            var prUrl = await _git.ForkAndCreatePullRequestAsync(req.RepoOwner, req.RepoName, fileFixMap,
                                $"AI Auto-Fix: {response.FixedCount} issues");
                            response.PullRequestUrl = prUrl;
                        }
                        catch (Exception ex2)
                        {
                            Console.WriteLine($"[BATCH FIX] Fork PR also failed: {ex2.Message}");
                        }
                    }
                }
            }

            return response;
        }

        public async Task<PreviewIssuesResponse> PreviewIssuesAsync(PreviewIssuesRequest req)
        {
            var response = new PreviewIssuesResponse { AnalysisCacheId = req.AnalysisCacheId };

            var cache = await _cacheRepo.GetByIdAsync(req.AnalysisCacheId);
            if (cache == null)
            {
                throw new Exception($"Không tìm thấy AnalysisCache với ID {req.AnalysisCacheId}");
            }

            var elements = await _elementRepo.GetElementsByAnalysisCacheIdAsync(req.AnalysisCacheId);
            if (elements == null)
            {
                return response;
            }

            await _git.DetectRepoStructureAsync(req.RepoOwner, req.RepoName);

            response.Url = cache.Url;
            response.TotalIssues = elements.Count();

            var cachedStructure = _git.GetCachedStructure(req.RepoOwner, req.RepoName);

            foreach (var element in elements)
            {
                try
                {
                    var evidences = TryDeserializeEvidences(element.ExtractedEvidenceJson);
                    string targetFileType = GetTargetFileTypeForIssue(element.AuditId);

                    if (targetFileType == "index.html")
                    {
                        string path = cachedStructure?.IndexHtmlPath ?? "index.html";
                        string method = "meta_tag_file";

                        if (string.IsNullOrEmpty(path))
                        {
                            var fallbackPaths = new[] { "client/index.html", "public/index.html", "index.html" };
                            foreach (var indexPath in fallbackPaths)
                            {
                                var content = await _git.GetFileContentAsync(req.RepoOwner, req.RepoName, indexPath);
                                if (content != null) { path = indexPath; break; }
                            }
                        }

                        response.Mappings.Add(new IssueFileMapping
                        {
                            ElementId = element.ElementID,
                            AuditId = element.AuditId,
                            Title = element.Title,
                            Description = element.Description,
                            Evidence = evidences,
                            FilePath = path ?? "index.html",
                            SearchMethod = method
                        });
                    }
                    else
                    {
                        var evidenceByFile = new Dictionary<string, List<string>>();
                        var methodByFile = new Dictionary<string, string>();

                        foreach (var ev in evidences)
                        {
                            if (string.IsNullOrEmpty(ev)) continue;

                            var filePath = await _git.FindFileByEvidenceAsync(req.RepoOwner, req.RepoName, ev);
                            if (filePath != null)
                            {
                                if (!evidenceByFile.ContainsKey(filePath))
                                {
                                    evidenceByFile[filePath] = new List<string>();
                                    methodByFile[filePath] = "evidence_search";
                                }
                                evidenceByFile[filePath].Add(ev);
                            }
                        }

                        if (evidenceByFile.Count == 0)
                        {
                            string fallbackPath = "index.html";
                            try
                            {
                                var uri = new Uri(cache.Url);
                                fallbackPath = await _git.FindFileByUrlPathAsync(req.RepoOwner, req.RepoName, uri.AbsolutePath) ?? "index.html";
                            }
                            catch { }

                            evidenceByFile[fallbackPath] = evidences;
                            methodByFile[fallbackPath] = "fallback";
                        }

                        foreach (var kvp in evidenceByFile)
                        {
                            response.Mappings.Add(new IssueFileMapping
                            {
                                ElementId = element.ElementID,
                                AuditId = element.AuditId,
                                Title = $"{element.Title} ({kvp.Value.Count} items)",
                                Description = element.Description,
                                Evidence = kvp.Value,
                                FilePath = kvp.Key,
                                SearchMethod = methodByFile[kvp.Key]
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    response.Mappings.Add(new IssueFileMapping
                    {
                        ElementId = element.ElementID,
                        AuditId = element.AuditId,
                        Title = element.Title,
                        Description = element.Description,
                        FilePath = null,
                        SearchMethod = "error: " + ex.Message
                    });
                }
            }

            return response;
        }

        #region Helpers

        private List<string> TryDeserializeEvidences(string? json)
        {
            if (string.IsNullOrEmpty(json)) return new List<string>();

            try
            {
                return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string> { json };
            }
        }

        private string GetTargetFileTypeForIssue(string issueKey)
        {
            var key = issueKey?.ToLower() ?? "";

            if (key.Contains("meta-") ||
                key.Contains("viewport") ||
                key.Contains("og-") ||
                key.Contains("canonical") ||
                key.Contains("lang") ||
                key.Contains("preconnect") ||
                key.Contains("render-blocking") ||
                key.Contains("external-fonts"))
            {
                return "index.html";
            }

            return "component";
        }

        #endregion
    }
}
