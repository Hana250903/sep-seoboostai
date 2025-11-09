using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface ISystemConfigService
    {
        // Lấy giá trị (luôn là đồng bộ vì đọc từ bộ nhớ)
        T GetValue<T>(string key, T defaultValue);

        // Cập nhật giá trị (bất đồng bộ vì ghi vào DB)
        Task UpdateValueAsync(string key, string newValue);

        // (Tùy chọn) Một hàm để lấy tất cả về cho trang Admin
        Dictionary<string, string> GetAllSettings();
    }
}
