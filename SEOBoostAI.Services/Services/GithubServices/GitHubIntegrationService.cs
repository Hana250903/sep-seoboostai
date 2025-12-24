using Microsoft.Extensions.Configuration;
using Octokit;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Service.Services.Interfaces;
using System.Text.RegularExpressions;

namespace SEOBoostAI.Service.Services.GithubServices
{
    /// <summary>
    /// GitHub Integration Service - Debug, file search, và Fork-based PR
    /// </summary>
    public class GitHubIntegrationService : IGitHubIntegrationService
    {
        private readonly GitHubClient _client;
        private readonly string _token;
        private static readonly Dictionary<string, RepoStructure> _repoStructureCache = new();

        public GitHubIntegrationService(ISystemConfigService systemConfigService)
        {
            _client = new GitHubClient(new ProductHeaderValue("SEOBoostAI"));
            _token = systemConfigService.GetValue<string>("GitHubToken", "");
            if (!string.IsNullOrEmpty(_token))
            {
                _client.Credentials = new Credentials(_token);
            }
        }

        #region Debug Methods

        public async Task<RepoDebugInfo> InspectRepoAsync(string owner, string repo, string branch = null)
        {
            var result = new RepoDebugInfo();

            try
            {
                var repository = await _client.Repository.Get(owner, repo);
                result.RepoName = repository.Name;
                result.IsPrivate = repository.Private;
                result.DefaultBranch = repository.DefaultBranch;

                var branches = await _client.Repository.Branch.GetAll(owner, repo);
                result.AllBranches = branches.Select(b => b.Name).ToList();

                string targetBranch = string.IsNullOrEmpty(branch) ? result.DefaultBranch : branch;

                try
                {
                    result.RootFiles = await ScanDirectoryAsync(owner, repo, "/", targetBranch, 0, 4);
                }
                catch (Exception ex)
                {
                    result.RootFiles.Add($"⚠️ Lỗi: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private async Task<List<string>> ScanDirectoryAsync(string owner, string repo, string path, string branch, int currentDepth, int maxDepth)
        {
            var files = new List<string>();
            string indent = new string(' ', currentDepth * 2);

            try
            {
                var contents = await _client.Repository.Content.GetAllContentsByRef(owner, repo, path, branch);

                foreach (var item in contents)
                {
                    if (item.Type == ContentType.Dir)
                    {
                        files.Add($"{indent}📁 {item.Name}/");

                        if (currentDepth < maxDepth)
                        {
                            var subFiles = await ScanDirectoryAsync(owner, repo, item.Path, branch, currentDepth + 1, maxDepth);
                            files.AddRange(subFiles);
                        }
                        else
                        {
                            files.Add($"{indent}  └── ...");
                        }
                    }
                    else
                    {
                        files.Add($"{indent}📄 {item.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                files.Add($"{indent}⚠️ Error: {ex.Message}");
            }

            return files;
        }

        #endregion

        #region File Operations

        public async Task<string?> GetFileContentAsync(string owner, string repo, string path, string branch = "main")
        {
            try
            {
                var content = await _client.Repository.Content.GetAllContentsByRef(owner, repo, path, branch);
                if (content == null || content.Count == 0) return null;
                return content[0].Content;
            }
            catch (NotFoundException)
            {
                Console.WriteLine($"[GitHub] Không tìm thấy file: {path}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GitHub] Lỗi: {ex.Message}");
                return null;
            }
        }

        public async Task<string> FindFileByEvidenceAsync(string owner, string repo, string evidence)
        {
            if (string.IsNullOrEmpty(evidence)) return null;

            // Xóa khoảng trắng thừa
            string targetString = evidence.Trim();

            // --- BƯỚC 1: TRÍCH XUẤT TỪ KHÓA & PHÁT HIỆN BUNDLED ASSETS ---
            string keyword = "";
            bool isBundledAsset = false;

            // Regex tìm file JS/CSS
            var fileMatch = Regex.Match(targetString, @"([a-zA-Z0-9\.\-_]+\.(js|css))");
            if (fileMatch.Success)
            {
                keyword = fileMatch.Groups[1].Value;

                // Phát hiện bundled asset (có hash pattern như index-k72HJK-f.js, main.abc123.js)
                if (Regex.IsMatch(keyword, @"[a-zA-Z]+-[a-zA-Z0-9]{6,}\.(js|css)") ||  // index-k72HJK-f.js
                    Regex.IsMatch(keyword, @"\.[a-f0-9]{8,}\.(js|css)") ||              // main.a1b2c3d4.js
                    keyword.Contains(".min.") ||
                    keyword.StartsWith("chunk-") ||
                    keyword.StartsWith("vendor"))
                {
                    Console.WriteLine($"[DEBUG] Phát hiện bundled asset: '{keyword}' - Sẽ scan theo anchor keyword thay thế");
                    isBundledAsset = true;
                    keyword = ""; // Reset để dùng anchor keyword thay thế
                }
            }
            else
            {
                // Nếu không có đuôi file, lấy class hoặc id
                var attrMatch = Regex.Match(targetString, "(?:class|id)=[\"'](.+?)[\"']");
                if (attrMatch.Success) keyword = attrMatch.Groups[1].Value.Split(' ')[0];
            }

            Console.WriteLine($"[DEBUG] Đang tìm: keyword='{keyword}', bundled={isBundledAsset}, evidence='{targetString}'");

            // --- BƯỚC 2: SCAN SÂU VÀO THƯ MỤC PAGES/COMPONENTS TRƯỚC ---
            // Ưu tiên dùng cached structure nếu có, fallback về hardcode
            var cachedStructure = GetCachedStructure(owner, repo);

            List<string> deepSearchDirs;
            if (cachedStructure != null && cachedStructure.AllSearchableDirs.Count > 0)
            {
                // Dùng cached structure - chính xác với project này
                deepSearchDirs = new List<string>();
                deepSearchDirs.AddRange(cachedStructure.ComponentPaths);  // Components trước
                deepSearchDirs.AddRange(cachedStructure.PagePaths);       // Pages
                if (!string.IsNullOrEmpty(cachedStructure.SrcRoot))
                    deepSearchDirs.Add(cachedStructure.SrcRoot);           // Src root
                deepSearchDirs.AddRange(cachedStructure.AllSearchableDirs); // Tất cả thư mục đã detect

                Console.WriteLine($"[DEBUG] Using cached structure: {string.Join(", ", deepSearchDirs)}");
            }
            else
            {
                // Fallback về hardcode (lần đầu hoặc chưa detect)
                deepSearchDirs = new List<string>
                {
                    "client/src/components", // Monorepo
                    "client/src/pages",
                    "client/src",
                    "src/components",        // Standard
                    "src/pages",
                    "src/views",
                    "src/screens",
                    "src",
                    "pages",                 // Next.js
                    "app",                   // Next.js 13+
                    "components"
                };
                Console.WriteLine($"[DEBUG] Using fallback hardcoded dirs");
            }

            foreach (var dir in deepSearchDirs.Distinct())
            {
                try
                {
                    var foundFile = await ScanDirectoryRecursivelyForEvidence(owner, repo, dir, "main", targetString, keyword, 0, 4);
                    if (foundFile != null)
                    {
                        Console.WriteLine($"[DEBUG] -> Tìm thấy trong deep scan: {foundFile}!");
                        return foundFile;
                    }
                }
                catch
                {
                    // Thư mục không tồn tại -> bỏ qua
                }
            }

            // --- BƯỚC 2.5: NẾU KHÔNG TÌM THẤY, CHECK PRIORITY FILES (FALLBACK) ---
            // Chỉ check entry points nếu deep scan không tìm thấy
            var priorityFiles = new List<string>
            {
                "index.html",           // HTML entry point
                "public/index.html",    // Create React App
                "src/index.html",       // Vite có thể đặt ở đây
                "src/App.jsx",          // React main component
                "src/App.tsx",          // React TypeScript
                "src/main.jsx",         // Vite React
                "src/main.tsx",         // Vite React TypeScript
                "src/index.jsx",        // CRA entry
                "src/index.tsx",        // CRA TypeScript entry
                "App.js",               // Root level React
                "App.jsx"               // Root level React
            };

            foreach (var filePath in priorityFiles)
            {
                try
                {
                    var fileContent = await GetFileContentAsync(owner, repo, filePath);

                    if (fileContent != null)
                    {
                        // Kiểm tra xem evidence hoặc keyword có trong file không
                        if (fileContent.Contains(targetString) ||
                            (!string.IsNullOrEmpty(keyword) && fileContent.Contains(keyword)))
                        {
                            Console.WriteLine($"[DEBUG] -> Tìm thấy trong {filePath}!");
                            return filePath;
                        }
                    }
                }
                catch
                {
                    // File không tồn tại -> tiếp tục check file khác
                }
            }

            // --- BƯỚC 3: GITHUB SEARCH API (NẾU BƯỚC 2 & 2.5 THẤT BẠI) ---
            if (string.IsNullOrEmpty(keyword) || keyword.Length < 3) return null;

            try
            {
                var request = new SearchCodeRequest(keyword)
                {
                    Repos = new RepositoryCollection { { owner, repo } }
                };

                var result = await _client.Search.SearchCode(request);
                if (result.TotalCount > 0)
                {
                    // Ưu tiên file trong src
                    var best = result.Items.FirstOrDefault(i => i.Path.StartsWith("src")) ?? result.Items[0];
                    Console.WriteLine($"[DEBUG] -> Search API thấy ở: {best.Path}");
                    return best.Path;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Search API Lỗi: {ex.Message}");
            }

            Console.WriteLine("[DEBUG] -> Không tìm thấy file nào.");
            return null;
        }

        private async Task<string?> ScanDirectoryForEvidenceAsync(string owner, string repo, string path, string branch,
            string targetString, string keyword, int currentDepth, int maxDepth)
        {
            if (currentDepth > maxDepth) return null;

            try
            {
                var contents = await _client.Repository.Content.GetAllContentsByRef(owner, repo, path, branch);

                foreach (var item in contents)
                {
                    if (item.Type == ContentType.File)
                    {
                        if (item.Name.EndsWith(".jsx") || item.Name.EndsWith(".tsx") ||
                            item.Name.EndsWith(".js") || item.Name.EndsWith(".ts") ||
                            item.Name.EndsWith(".html") || item.Name.EndsWith(".vue"))
                        {
                            var content = await GetFileContentAsync(owner, repo, item.Path, branch);
                            if (content != null)
                            {
                                if (content.Contains(targetString) ||
                                    (!string.IsNullOrEmpty(keyword) && content.Contains(keyword)))
                                {
                                    return item.Path;
                                }
                            }
                        }
                    }
                    else if (item.Type == ContentType.Dir)
                    {
                        var found = await ScanDirectoryForEvidenceAsync(owner, repo, item.Path, branch, targetString, keyword, currentDepth + 1, maxDepth);
                        if (found != null) return found;
                    }
                }
            }
            catch (NotFoundException) { }

            return null;
        }

        public async Task<string?> FindFileByUrlPathAsync(string owner, string repo, string urlPath)
        {
            if (string.IsNullOrEmpty(urlPath) || urlPath == "/")
            {
                return await FindFileByNamePatternAsync(owner, repo, new[] { "App", "Home", "Index", "Main" });
            }

            urlPath = urlPath.Trim('/');
            var segments = urlPath.Split('/');
            string lastSegment = segments.Last();

            string capitalizedName = char.ToUpper(lastSegment[0]) + lastSegment.Substring(1).ToLower();
            var namePatterns = new[]
            {
                capitalizedName + "Page", capitalizedName, lastSegment + "Page", lastSegment, capitalizedName + "View"
            };

            return await FindFileByNamePatternAsync(owner, repo, namePatterns);
        }

        private async Task<string?> FindFileByNamePatternAsync(string owner, string repo, string[] patterns)
        {
            var searchDirs = new[] { "src", "src/pages", "src/views", "src/components", "pages", "app" };

            foreach (var dir in searchDirs)
            {
                try
                {
                    var contents = await _client.Repository.Content.GetAllContentsByRef(owner, repo, dir, "main");

                    foreach (var pattern in patterns)
                    {
                        var matchedFile = contents.FirstOrDefault(c =>
                            c.Type == ContentType.File &&
                            (c.Name.EndsWith(".jsx") || c.Name.EndsWith(".tsx") || c.Name.EndsWith(".js")) &&
                            (c.Name.StartsWith(pattern, StringComparison.OrdinalIgnoreCase) ||
                             c.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase)));

                        if (matchedFile != null) return matchedFile.Path;
                    }
                }
                catch { }
            }

            return null;
        }

        // === SCAN SÂU TÌM FILE CHỨA EVIDENCE ===
        private async Task<string> ScanDirectoryRecursivelyForEvidence(
            string owner, string repo, string path, string branch,
            string targetString, string keyword, int currentDepth, int maxDepth)
        {
            if (currentDepth > maxDepth) return null;

            try
            {
                var contents = await _client.Repository.Content.GetAllContentsByRef(owner, repo, path, branch);

                foreach (var item in contents)
                {
                    if (item.Type == Octokit.ContentType.File)
                    {
                        // Chỉ check file .jsx, .tsx, .js, .ts, .html
                        if (item.Name.EndsWith(".jsx") || item.Name.EndsWith(".tsx") ||
                            item.Name.EndsWith(".js") || item.Name.EndsWith(".ts") ||
                            item.Name.EndsWith(".html") || item.Name.EndsWith(".vue"))
                        {
                            try
                            {
                                var content = await GetFileContentAsync(owner, repo, item.Path);
                                if (content != null)
                                {
                                    // Kiểm tra evidence trong file
                                    bool found = false;

                                    // 1. Exact match
                                    if (content.Contains(targetString))
                                    {
                                        found = true;
                                    }

                                    // 2. Keyword match
                                    if (!found && !string.IsNullOrEmpty(keyword) && content.Contains(keyword))
                                    {
                                        found = true;
                                    }

                                    // 3. Fuzzy match cho img/a tags - extract src/alt từ evidence
                                    if (!found && targetString.Contains("<img") || targetString.Contains("<a "))
                                    {
                                        // Extract src từ evidence
                                        var srcMatch = Regex.Match(targetString, @"src=[""']([^""']+)[""']");
                                        if (srcMatch.Success && content.Contains(srcMatch.Groups[1].Value))
                                        {
                                            Console.WriteLine($"[DEEP SCAN] Fuzzy match (src): {srcMatch.Groups[1].Value} in {item.Path}");
                                            found = true;
                                        }

                                        // Extract alt từ evidence
                                        if (!found)
                                        {
                                            var altMatch = Regex.Match(targetString, @"alt=[""']([^""']+)[""']");
                                            if (altMatch.Success && content.Contains($"alt=\"{altMatch.Groups[1].Value}\""))
                                            {
                                                Console.WriteLine($"[DEEP SCAN] Fuzzy match (alt): {altMatch.Groups[1].Value} in {item.Path}");
                                                found = true;
                                            }
                                        }
                                    }

                                    // 4. Fuzzy match cho text content (H1, button text, etc.)
                                    if (!found && !targetString.StartsWith("<"))
                                    {
                                        // Evidence là plain text (vd: "Demo Website", "Hero")
                                        if (content.Contains(targetString))
                                        {
                                            Console.WriteLine($"[DEEP SCAN] Fuzzy match (text): {targetString} in {item.Path}");
                                            found = true;
                                        }
                                    }

                                    if (found)
                                    {
                                        Console.WriteLine($"[DEEP SCAN] Found evidence in: {item.Path}");
                                        return item.Path;
                                    }
                                }
                            }
                            catch
                            {
                                // Bỏ qua lỗi đọc file
                            }
                        }
                    }
                    else if (item.Type == Octokit.ContentType.Dir)
                    {
                        // Đệ quy vào thư mục con
                        var found = await ScanDirectoryRecursivelyForEvidence(
                            owner, repo, item.Path, branch,
                            targetString, keyword, currentDepth + 1, maxDepth);

                        if (found != null) return found;
                    }
                }
            }
            catch (Octokit.NotFoundException)
            {
                // Thư mục không tồn tại
            }

            return null;
        }

        #endregion

        #region Repo Structure Detection

        public async Task<RepoStructure> DetectRepoStructureAsync(string owner, string repo)
        {
            string cacheKey = $"{owner}/{repo}";

            if (_repoStructureCache.TryGetValue(cacheKey, out var cached))
            {
                if ((DateTime.UtcNow - cached.CachedAt).TotalMinutes < 60)
                    return cached;
            }

            var structure = new RepoStructure
            {
                Owner = owner,
                Repo = repo,
                CachedAt = DateTime.UtcNow
            };

            try
            {
                var rootContents = await _client.Repository.Content.GetAllContents(owner, repo);
                var rootItems = rootContents.Select(c => c.Name).ToList();

                string srcPrefix = "";
                if (rootItems.Contains("client"))
                {
                    srcPrefix = "client/";
                    structure.ProjectType = "monorepo";
                }
                else
                {
                    structure.ProjectType = "standard";
                }

                // Find index.html
                var indexPaths = new[] { $"{srcPrefix}index.html", $"{srcPrefix}public/index.html", "index.html", "public/index.html" };
                foreach (var path in indexPaths)
                {
                    var content = await GetFileContentAsync(owner, repo, path);
                    if (content != null)
                    {
                        structure.IndexHtmlPath = path;
                        break;
                    }
                }

                // Find src root
                var srcPaths = new[] { $"{srcPrefix}src", "src" };
                foreach (var path in srcPaths)
                {
                    try
                    {
                        await _client.Repository.Content.GetAllContents(owner, repo, path);
                        structure.SrcRoot = path;
                        break;
                    }
                    catch { }
                }

                // Scan subdirectories
                if (!string.IsNullOrEmpty(structure.SrcRoot))
                {
                    var subDirs = new[] { "components", "pages", "views", "screens", "features", "modules", "lib", "hooks" };
                    foreach (var sub in subDirs)
                    {
                        var fullPath = $"{structure.SrcRoot}/{sub}";
                        try
                        {
                            await _client.Repository.Content.GetAllContents(owner, repo, fullPath);
                            structure.AllSearchableDirs.Add(fullPath);

                            if (sub == "components") structure.ComponentPaths.Add(fullPath);
                            if (sub == "pages" || sub == "views" || sub == "screens") structure.PagePaths.Add(fullPath);
                        }
                        catch { }
                    }
                }

                // Detect special dirs
                var specialDirs = new[] { "app", "pages" };
                foreach (var dir in specialDirs)
                {
                    try
                    {
                        await _client.Repository.Content.GetAllContents(owner, repo, dir);
                        structure.AllSearchableDirs.Add(dir);
                        structure.PagePaths.Add(dir);

                        if (dir == "app") structure.ProjectType = "nextjs-app";
                        else if (dir == "pages" && structure.ProjectType != "monorepo") structure.ProjectType = "nextjs";
                    }
                    catch { }
                }

                // Detect from config files
                if (rootItems.Contains("next.config.js") || rootItems.Contains("next.config.mjs"))
                    structure.ProjectType = "nextjs";
                else if (rootItems.Contains("vite.config.js") || rootItems.Contains("vite.config.ts"))
                    structure.ProjectType = "vite";
                else if (rootItems.Contains("angular.json"))
                    structure.ProjectType = "angular";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RepoStructure] Error: {ex.Message}");
                structure.ProjectType = "unknown";
            }

            _repoStructureCache[cacheKey] = structure;
            return structure;
        }

        public RepoStructure? GetCachedStructure(string owner, string repo)
        {
            string cacheKey = $"{owner}/{repo}";
            return _repoStructureCache.TryGetValue(cacheKey, out var cached) ? cached : null;
        }

        #endregion

        #region User Methods

        public async Task<string> GetCurrentUserLoginAsync()
        {
            try
            {
                var currentUser = await _client.User.Current();
                return currentUser.Login;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GitHub] Error getting current user: {ex.Message}");
                return string.Empty;
            }
        }

        #endregion

        #region Pull Request Operations

        public async Task<string> CreateBatchPullRequestAsync(string owner, string repo, Dictionary<string, string> fileContents, string message)
        {
            var repoInfo = await _client.Repository.Get(owner, repo);
            string defaultBranch = repoInfo.DefaultBranch;

            string newBranch = "fix/ai-batch-" + Guid.NewGuid().ToString().Substring(0, 6);
            var masterRef = await _client.Git.Reference.Get(owner, repo, "heads/" + defaultBranch);
            await _client.Git.Reference.Create(owner, repo, new NewReference("refs/heads/" + newBranch, masterRef.Object.Sha));

            foreach (var kvp in fileContents)
            {
                string path = kvp.Key;
                string content = kvp.Value;

                try
                {
                    var fileRef = await _client.Repository.Content.GetAllContentsByRef(owner, repo, path, newBranch);
                    await _client.Repository.Content.UpdateFile(owner, repo, path,
                        new UpdateFileRequest($"Fix: {path}", content, fileRef[0].Sha, newBranch));
                    Console.WriteLine($"[BATCH PR] Updated: {path}");
                }
                catch (NotFoundException)
                {
                    await _client.Repository.Content.CreateFile(owner, repo, path,
                        new CreateFileRequest($"Add: {path}", content, newBranch));
                    Console.WriteLine($"[BATCH PR] Created: {path}");
                }
            }

            var pr = await _client.PullRequest.Create(owner, repo,
                new NewPullRequest(message, newBranch, defaultBranch)
                {
                    Body = $"## AI Auto-Fix\n\nThis PR contains fixes for {fileContents.Count} file(s):\n\n" +
                           string.Join("\n", fileContents.Keys.Select(f => $"- `{f}`"))
                });

            return pr.HtmlUrl;
        }

        public async Task<string> ForkAndCreatePullRequestAsync(string owner, string repo, Dictionary<string, string> fileContents, string message)
        {
            Console.WriteLine($"[FORK PR] Starting for {owner}/{repo}...");

            var currentUser = await _client.User.Current();
            string forkOwner = currentUser.Login;

            // 1. Kiểm tra xem fork đã tồn tại chưa
            Octokit.Repository? fork = null;
            bool forkExists = false;

            try
            {
                fork = await _client.Repository.Get(forkOwner, repo);
                // Kiểm tra đây có phải là fork của repo gốc không
                if (fork != null && fork.Fork && fork.Parent?.FullName == $"{owner}/{repo}")
                {
                    forkExists = true;
                    Console.WriteLine($"[FORK PR] ✓ Fork đã tồn tại: {forkOwner}/{repo}");
                }
                else if (fork != null)
                {
                    // Repo tồn tại nhưng không phải fork của repo gốc
                    Console.WriteLine($"[FORK PR] ⚠ Repo {forkOwner}/{repo} tồn tại nhưng không phải fork của {owner}/{repo}");
                    throw new Exception($"Bạn đã có repo '{repo}' nhưng nó không phải fork của {owner}/{repo}. Vui lòng đổi tên hoặc xóa repo đó.");
                }
            }
            catch (NotFoundException)
            {
                // Expected - fork chưa tồn tại
                Console.WriteLine($"[FORK PR] Fork chưa tồn tại, sẽ tạo mới...");
            }

            // 2. Tạo fork mới nếu chưa tồn tại
            if (!forkExists)
            {
                try
                {
                    Console.WriteLine($"[FORK PR] Đang tạo fork...");
                    fork = await _client.Repository.Forks.Create(owner, repo, new NewRepositoryFork());
                    Console.WriteLine($"[FORK PR] Fork đã được tạo, đợi GitHub xử lý...");
                    
                    // Đợi GitHub tạo xong fork
                    await Task.Delay(5000);

                    // Retry lấy fork info
                    for (int i = 0; i < 10; i++)
                    {
                        try
                        {
                            fork = await _client.Repository.Get(forkOwner, repo);
                            if (fork != null)
                            {
                                Console.WriteLine($"[FORK PR] ✓ Fork sẵn sàng: {fork.HtmlUrl}");
                                break;
                            }
                        }
                        catch
                        {
                            Console.WriteLine($"[FORK PR] Đợi fork... (attempt {i + 1}/10)");
                            await Task.Delay(2000);
                        }
                    }
                }
                catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
                {
                    // Lỗi 422 - có thể fork đã tồn tại nhưng check trước đó không thấy
                    Console.WriteLine($"[FORK PR] 422 - Có thể fork đã tồn tại, thử lấy lại...");
                    try
                    {
                        fork = await _client.Repository.Get(forkOwner, repo);
                        if (fork != null && fork.Fork)
                        {
                            Console.WriteLine($"[FORK PR] ✓ Tìm thấy fork có sẵn: {forkOwner}/{repo}");
                        }
                        else
                        {
                            throw new Exception($"Không thể tạo hoặc tìm fork: {ex.Message}");
                        }
                    }
                    catch (NotFoundException)
                    {
                        throw new Exception($"Lỗi 422 nhưng không tìm thấy fork: {ex.Message}");
                    }
                }
            }

            var upstreamRepo = await _client.Repository.Get(owner, repo);
            string defaultBranch = upstreamRepo.DefaultBranch;

            try
            {
                var upstreamRef = await _client.Git.Reference.Get(owner, repo, $"heads/{defaultBranch}");
                await _client.Git.Reference.Update(forkOwner, repo, $"heads/{defaultBranch}",
                    new ReferenceUpdate(upstreamRef.Object.Sha, true));
            }
            catch { }

            string newBranch = "fix/ai-autofix-" + Guid.NewGuid().ToString().Substring(0, 6);
            var forkMainRef = await _client.Git.Reference.Get(forkOwner, repo, $"heads/{defaultBranch}");
            await _client.Git.Reference.Create(forkOwner, repo, new NewReference($"refs/heads/{newBranch}", forkMainRef.Object.Sha));

            foreach (var kvp in fileContents)
            {
                string path = kvp.Key;
                string content = kvp.Value;

                try
                {
                    var fileRef = await _client.Repository.Content.GetAllContentsByRef(forkOwner, repo, path, newBranch);
                    await _client.Repository.Content.UpdateFile(forkOwner, repo, path,
                        new UpdateFileRequest($"Fix: {path}", content, fileRef[0].Sha, newBranch));
                }
                catch (NotFoundException)
                {
                    await _client.Repository.Content.CreateFile(forkOwner, repo, path,
                        new CreateFileRequest($"Add: {path}", content, newBranch));
                }
            }

            var pr = await _client.PullRequest.Create(owner, repo,
                new NewPullRequest(message, $"{forkOwner}:{newBranch}", defaultBranch)
                {
                    Body = $"## 🤖 AI Auto-Fix\n\nThis PR contains fixes for {fileContents.Count} file(s):\n\n" +
                           string.Join("\n", fileContents.Keys.Select(f => $"- `{f}`")) +
                           $"\n\n---\n_Generated via Fork: [{forkOwner}/{repo}](https://github.com/{forkOwner}/{repo})_"
                });

            Console.WriteLine($"[FORK PR] Created: {pr.HtmlUrl}");
            return pr.HtmlUrl;
        }

        #endregion
    }
}
