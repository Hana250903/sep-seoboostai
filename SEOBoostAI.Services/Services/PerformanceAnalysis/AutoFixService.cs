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
    /// AutoFixService - Tự động sửa code và tạo Pull Request
    /// 
    /// FLOW CHÍNH:
    /// 1. Lấy AnalysisCache + Elements (danh sách issues)
    /// 2. Detect cấu trúc repo (Vite/Next.js/CRA?, branch?)
    /// 3. Mapping: Issue → File (tìm file chứa code lỗi)
    /// 4. Gemini AI fix code
    /// 5. Tạo Pull Request (Direct hoặc Fork-based)
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

        /// <summary>
        /// BATCH FIX - SỬa tất cả issues và tạo PR
        /// 
        /// Input: AnalysisCacheId, RepoOwner, RepoName, CreateSinglePR, UseForkPR
        /// Output: BatchFixResponse (FixedCount, FailedCount, PullRequestUrl)
        /// </summary>
        public async Task<BatchFixResponse> BatchFixAsync(BatchFixRequest req)
        {
            var response = new BatchFixResponse();

            // ===== BƯỚC 1: LẤY DỮ LIỆU =====
            // Lấy AnalysisCache và danh sách Elements (issues cần fix)
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

            // ===== BƯỚC 2: DETECT CẤU TRÚC REPO =====
            // Phát hiện loại project (Vite, Next.js, CRA...)
            // Tìm default branch (main/master)
            // Xác định vị trí index.html, src/, components/...
            await _git.DetectRepoStructureAsync(req.RepoOwner, req.RepoName);

            // ===== BƯỚC 3: MAPPING ISSUE → FILE =====
            // Với mỗi issue, xác định file cần sửa:
            // - meta-tag, viewport, canonical... → index.html
            // - img, script, component... → scan tìm file chứa evidence
            var issuesByFile = new Dictionary<string, List<(Element, List<string>)>>();
            var cachedStructure = _git.GetCachedStructure(req.RepoOwner, req.RepoName);
            string branch = cachedStructure?.DefaultBranch ?? "main";  // Sử dụng branch đúng của repo

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
                            var content = await _git.GetFileContentAsync(req.RepoOwner, req.RepoName, tryPath, branch);
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

            // ===== BƯỚC 4: AI FIX CODE =====
            // Với mỗi file:
            // 1. Đọc code hiện tại từ GitHub
            // 2. Gửi code + issues cho Gemini AI
            // 3. AI trả về code đã fix
            var fileFixMap = new Dictionary<string, string>();

            foreach (var fileGroup in issuesByFile)
            {
                string filePath = fileGroup.Key;
                var fileIssues = fileGroup.Value;

                Console.WriteLine($"[BATCH FIX] Processing {filePath} with {fileIssues.Count} issues");

                try
                {
                    string code = await _git.GetFileContentAsync(req.RepoOwner, req.RepoName, filePath, branch);
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

            // ===== BƯỚC 5: TẠO PULL REQUEST =====
            // Có 2 modes:
            // A) Direct PR: RepoOwner = Token Owner (có write access)
            // B) Fork-based PR: RepoOwner ≠ Token Owner (cần fork trước)
            if (req.CreateSinglePR && fileFixMap.Count > 0)
            {
                try
                {
                    string prUrl;
                    string prMessage = $"AI Auto-Fix: {response.FixedCount} issues in {fileFixMap.Count} files";

                    // Kiểm tra xem token owner có phải là repo owner không
                    string tokenOwner = await _git.GetCurrentUserLoginAsync();
                    bool isOwnerSameAsTokenOwner = !string.IsNullOrEmpty(tokenOwner) && 
                        tokenOwner.Equals(req.RepoOwner, StringComparison.OrdinalIgnoreCase);

                    if (isOwnerSameAsTokenOwner)
                    {
                        // MODE A: DIRECT PR
                        // RepoOwner trùng với Token Owner -> có write access
                        // Tạo branch mới, commit, tạo PR trực tiếp trên repo
                        Console.WriteLine($"[BATCH FIX] Owner '{req.RepoOwner}' = Token Owner '{tokenOwner}' -> Using Direct PR...");
                        prUrl = await _git.CreateBatchPullRequestAsync(req.RepoOwner, req.RepoName, fileFixMap, prMessage);
                    }
                    else if (req.UseForkPR || !isOwnerSameAsTokenOwner)
                    {
                        // MODE B: FORK-BASED PR
                        // RepoOwner khác Token Owner -> không có write access
                        // Phải: Fork repo -> Tạo branch trên fork -> Commit -> Tạo cross-repo PR
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

        /// <summary>
        /// PREVIEW ISSUES - Xem trước danh sách issues và file tương ứng
        /// 
        /// Giúp user biết trước:
        /// - Issue nào sẽ được fix?
        /// - Sửa ở file nào?
        /// - Tìm thấy bằng cách nào (evidence_search, meta_tag_file, fallback)?
        /// </summary>
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
            string branch = cachedStructure?.DefaultBranch ?? "main";  // Sử dụng branch đúng của repo

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
                                var content = await _git.GetFileContentAsync(req.RepoOwner, req.RepoName, indexPath, branch);
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

        /// <summary>
        /// Parse JSON evidence thành list string
        /// Evidence là đoạn code HTML cụ thể cần sửa
        /// Ví dụ: ["<img src='...'>", "<script src='...'>"] 
        /// </summary>
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

        /// <summary>
        /// Xác định file target dựa vào loại issue:
        /// - meta-*, viewport, og-*, canonical, lang... -> index.html
        /// - Các issue khác -> component (cần scan tìm file)
        /// </summary>
        private string GetTargetFileTypeForIssue(string issueKey)
        {
            var key = issueKey?.ToLower() ?? "";

            // Những issue này thường nằm trong <head> của index.html
            if (key.Contains("meta-") ||      // meta tags
                key.Contains("viewport") ||   // viewport configuration
                key.Contains("og-") ||        // Open Graph tags
                key.Contains("canonical") ||  // canonical URL
                key.Contains("lang") ||       // html lang attribute
                key.Contains("preconnect") || // preconnect hints
                key.Contains("render-blocking") || // render-blocking resources
                key.Contains("external-fonts"))    // external font loading
            {
                return "index.html";
            }

            // Các issue khác (img, script trong component...) cần tìm file
            return "component";
        }

        #endregion
    }
}
