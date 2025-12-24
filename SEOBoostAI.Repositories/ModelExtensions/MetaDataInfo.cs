using System;
using System.Collections.Generic;

namespace SEOBoostAI.Repository.ModelExtensions
{
    /// <summary>
    /// DTO để chứa metadata được extract từ HTML (dùng trong code logic, không phải EF entity)
    /// </summary>
    public class MetaDataInfo
    {
        // Basic Meta Tags
        public string Title { get; set; }
        public string Description { get; set; }
        public string Keywords { get; set; }
        public string Charset { get; set; }
        public string Viewport { get; set; }
        public string Canonical { get; set; }
        public string Robots { get; set; }

        // Open Graph Tags (Facebook/LinkedIn) - stored as dictionary for flexibility
        public Dictionary<string, string> OpenGraph { get; set; } = new Dictionary<string, string>();

        // Twitter Card Tags
        public Dictionary<string, string> TwitterCard { get; set; } = new Dictionary<string, string>();

        // Other Important Tags
        public Dictionary<string, string> OtherMeta { get; set; } = new Dictionary<string, string>();
    }
}
