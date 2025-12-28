namespace SEOBoostAI.Repository.ModelExtensions
{
    public class ScanRequest
    {
        public string Url { get; set; }
    }

    // Batch Fix - Fix all issues from a session
    public class BatchFixRequest
    {
        public int AnalysisCacheId { get; set; }
        public string RepoOwner { get; set; }
        public string RepoName { get; set; }
        public bool CreateSinglePR { get; set; } = true;  // Tạo 1 PR cho tất cả fixes
        public bool UseForkPR { get; set; } = false;      // True = fork repo rồi tạo cross-repo PR (cho public repos không có write access)
    }

    public class BatchFixResponse
    {
        public int TotalIssues { get; set; }
        public int FixedCount { get; set; }
        public int FailedCount { get; set; }
        public List<FixResult> Results { get; set; } = new List<FixResult>();
        public string PullRequestUrl { get; set; }  // URL của PR nếu CreateSinglePR = true
    }

    public class FixResult
    {
        public int ElementId { get; set; }
        public string AuditId { get; set; }
        public string Title { get; set; }
        public string FilePath { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    // Preview Issues - Xem trước issues nằm ở file nào
    public class PreviewIssuesRequest
    {
        public int AnalysisCacheId { get; set; }
        public string RepoOwner { get; set; }
        public string RepoName { get; set; }
    }

    public class PreviewIssuesResponse
    {
        public int AnalysisCacheId { get; set; }
        public string Url { get; set; }
        public int TotalIssues { get; set; }
        public List<IssueFileMapping> Mappings { get; set; } = new List<IssueFileMapping>();
    }

    public class IssueFileMapping
    {
        public int ElementId { get; set; }
        public string AuditId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string FilePath { get; set; }           // File chứa lỗi (null nếu không tìm thấy)
        public string SearchMethod { get; set; }       // "priority", "deep_scan", "github_api", "fallback"
        public List<string> Evidence { get; set; }     // Bằng chứng gốc
    }

    public class RepoDebugInfo
    {
        public string RepoName { get; set; }
        public string DefaultBranch { get; set; } // Nhánh mặc định (thủ phạm chính)
        public bool IsPrivate { get; set; }
        public List<string> AllBranches { get; set; } = new List<string>();
        public List<string> RootFiles { get; set; } = new List<string>(); // Danh sách file API nhìn thấy
        public string ErrorMessage { get; set; }
    }

    // === REPO STRUCTURE: Lưu cấu trúc thư mục của repo để search chính xác ===
    public class RepoStructure
    {
        public string Owner { get; set; }
        public string Repo { get; set; }
        public string DefaultBranch { get; set; } = "main";  // Default branch của repo (main, master, etc.)
        public string IndexHtmlPath { get; set; }           // "client/index.html", "index.html", "public/index.html"
        public string SrcRoot { get; set; }                  // "client/src", "src", ""
        public List<string> ComponentPaths { get; set; } = new List<string>();  // ["client/src/components", "src/components"]
        public List<string> PagePaths { get; set; } = new List<string>();        // ["client/src/pages", "src/pages"]
        public List<string> AllSearchableDirs { get; set; } = new List<string>(); // Tất cả thư mục có thể chứa code
        public DateTime CachedAt { get; set; }               // Thời gian cache
        public string ProjectType { get; set; }              // "vite", "nextjs", "cra", "monorepo", "unknown"
    }

    // === AUDIT ISSUE DTO: Dùng cho Puppeteer check methods ===
    public class AuditIssueDto
    {
        public string AuditId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<string>? Evidence { get; set; }

        public AuditIssueDto(string auditId, string title, string description, List<string>? evidence)
        {
            AuditId = auditId;
            Title = title;
            Description = description;
            Evidence = evidence;
        }
    }
}

