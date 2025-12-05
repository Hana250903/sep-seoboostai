using Microsoft.Extensions.DependencyInjection;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Configurations
{
    public class GeminiRateLimitManager : IGeminiRateLimitManager
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        // In-memory tracking cho mỗi key
        private class KeyUsageTracker
        {
            public GeminiKey Key { get; set; }
            public Queue<DateTime> RequestTimestamps { get; set; } = new Queue<DateTime>();
            public long TokensUsedInMinute { get; set; } = 0;
            public DateTime LastMinuteReset { get; set; } = DateTime.UtcNow;
            public bool IsRateLimited { get; set; } = false;
            public DateTime? RateLimitedUntil { get; set; }
        }

        private ConcurrentDictionary<int, KeyUsageTracker> _keyTrackers = new ConcurrentDictionary<int, KeyUsageTracker>();

        public GeminiRateLimitManager(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        // Hàm helper để lấy Repository trong một scope ngắn hạn
        private async Task InitializeKeysAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IGeminiKeyRepository>();
                var keys = await repo.GetAllActiveKeysAsync();

                foreach (var key in keys)
                {
                    // Dùng TryAdd để tránh lỗi nếu key đã tồn tại
                    if (!_keyTrackers.ContainsKey(key.Id))
                    {
                        _keyTrackers.TryAdd(key.Id, new KeyUsageTracker { Key = key });
                    }
                }
            }
        }

        public async Task<GeminiKey> GetAvailableKeyAsync()
        {
            var maxWaitTime = TimeSpan.FromSeconds(10);
            var startTime = DateTime.UtcNow;
            while (true)
            {
                GeminiKey selectedKey = null;

                // 1. Vào vùng an toàn (Critical Section)
                await _semaphore.WaitAsync();
                try
                {
                    // Lazy load lần đầu
                    if (_keyTrackers.IsEmpty)
                    {
                        await InitializeKeysAsync();
                        if (_keyTrackers.IsEmpty) throw new InvalidOperationException("DB không có Key nào active.");
                    }

                    var now = DateTime.UtcNow;
                    var today = DateTime.UtcNow.Date;

                    // 2. Tìm kiếm Key khả dụng (Logic Round-Robin hoặc Least Used có thể áp dụng ở đây, hiện tại dùng First Available)
                    // Sắp xếp theo RequestUsedToday để ưu tiên key ít dùng trước (Load balancing đơn giản)
                    foreach (var tracker in _keyTrackers.Values.OrderBy(t => t.Key.RequestsUsedToday))
                    {
                        // --- A. Kiểm tra trạng thái bị khóa tạm thời (429/428) ---
                        if (tracker.IsRateLimited && tracker.RateLimitedUntil.HasValue)
                        {
                            if (now > tracker.RateLimitedUntil.Value)
                            {
                                // Hết hạn phạt -> Mở khóa
                                tracker.IsRateLimited = false;
                                tracker.RateLimitedUntil = null;
                            }
                            else continue;
                        }

                        // --- B. Reset bộ đếm theo phút ---
                        if ((now - tracker.LastMinuteReset).TotalMinutes >= 1)
                        {
                            tracker.RequestTimestamps.Clear();
                            tracker.TokensUsedInMinute = 0;
                            tracker.LastMinuteReset = now;
                        }

                        // --- C. Reset bộ đếm theo ngày (Sync DB nếu cần) ---
                        if (tracker.Key.LastResetDate.Date < today)
                        {
                            tracker.Key.RequestsUsedToday = 0;
                            tracker.Key.TokensUsedToday = 0;
                            tracker.Key.LastResetDate = today;

                            await ResetKeyDailyUsageInDb(tracker.Key.Id, today);
                        }

                        // --- D. Kiểm tra giới hạn Quota ---
                        // 1. Check RPM (Requests Per Minute) - Clean timestamp cũ
                        while (tracker.RequestTimestamps.Count > 0 && (now - tracker.RequestTimestamps.Peek()).TotalMinutes >= 1)
                        {
                            tracker.RequestTimestamps.Dequeue();
                        }

                        // 5. Kiểm tra các giới hạn
                        if (tracker.RequestTimestamps.Count >= tracker.Key.RpmLimit) continue;
                        if (tracker.Key.RequestsUsedToday >= tracker.Key.RpdLimit) continue;
                        if (tracker.TokensUsedInMinute >= tracker.Key.TpmLimit) continue;

                        // ===> TÌM THẤY KEY KHẢ DỤNG <===
                        selectedKey = tracker.Key;
                        break;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }

                // 3. Xử lý kết quả tìm kiếm (Ở ngoài Semaphore)
                if (selectedKey != null)
                {
                    return selectedKey;
                }

                // Nếu chưa tìm thấy key, kiểm tra timeout
                if (DateTime.UtcNow - startTime > maxWaitTime)
                {
                    throw new InvalidOperationException("Tất cả API keys đều đang bận.");
                }

                // Chờ một chút trước khi thử lại (KHÔNG GIỮ KHÓA KHI NGỦ)
                await Task.Delay(100);
            }
        }

        private async Task ResetKeyDailyUsageInDb(int keyId, DateTime today)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IGeminiKeyRepository>();
                await repo.UpdateKeyUsageAsync(keyId, 0, today); // Reset về 0
            }
        }

        public async Task RecordUsageAsync(int keyId, int estimatedTokens)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_keyTrackers.TryGetValue(keyId, out var tracker))
                {
                    var now = DateTime.UtcNow;
                    tracker.RequestTimestamps.Enqueue(now);
                    tracker.TokensUsedInMinute += estimatedTokens;

                    // Update tracker object
                    tracker.Key.RequestsUsedToday++;
                    tracker.Key.TokensUsedToday += estimatedTokens;
                }
            }
            finally
            {
                _semaphore.Release();
            }

            // Update DB: Làm async bên ngoài semaphore để trả khóa nhanh cho request khác
            // Lưu ý: Có thể dùng cơ chế batch update hoặc background job nếu lượng request quá lớn
            _ = Task.Run(async () =>
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var repo = scope.ServiceProvider.GetRequiredService<IGeminiKeyRepository>();

                        await repo.UpdateKeyUsageAsync(keyId, estimatedTokens, DateTime.UtcNow.Date);
                    }
                }
                catch
                {
                    // Log error update DB (không throw ra ngoài làm crash app)
                }
            });
        }

        public async Task MarkKeyRateLimitedAsync(int keyId)
        {
            DateTime lockedUntil;

            await _semaphore.WaitAsync();
            try
            {
                if (_keyTrackers.TryGetValue(keyId, out var tracker))
                {
                    // Phạt 1 phút (hoặc tùy chỉnh)
                    lockedUntil = DateTime.UtcNow.AddMinutes(1);

                    tracker.IsRateLimited = true;
                    tracker.RateLimitedUntil = lockedUntil;
                }
                else return;
            }
            finally
            {
                _semaphore.Release();
            }

            // Update DB async
            _ = Task.Run(async () =>
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var repo = scope.ServiceProvider.GetRequiredService<IGeminiKeyRepository>();
                    await repo.MarkKeyRateLimitedAsync(keyId, lockedUntil);
                }
            });
        }

        public async Task UpdateActualTokensAsync(int keyId, int actualTokens, int estimatedTokens)
            {
            int tokenDifference = actualTokens - estimatedTokens;

            // Update in-memory tracker
            await _semaphore.WaitAsync();
            try
            {
                if (_keyTrackers.TryGetValue(keyId, out var tracker))
                {
                    // Điều chỉnh tokens theo hiệu số thực tế vs ước tính
                    tracker.TokensUsedInMinute += tokenDifference;
                    tracker.Key.TokensUsedToday += tokenDifference;
                }
            }
            finally
            {
                _semaphore.Release();
            }

            // Update DB async
            _ = Task.Run(async () =>
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var repo = scope.ServiceProvider.GetRequiredService<IGeminiKeyRepository>();
                        // Gọi update để điều chỉnh token count
                        await repo.AdjustTokenUsageAsync(keyId, tokenDifference);
                    }
                }
                catch
                {
                    // Log error (không throw để tránh crash app)
                }
            });
        }

        public async Task ReloadKeysAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                _keyTrackers.Clear();
                // Cần gọi lại init trong này luôn vì method này explicit reload
                using (var scope = _scopeFactory.CreateScope())
                {
                    var repo = scope.ServiceProvider.GetRequiredService<IGeminiKeyRepository>();
                    var keys = await repo.GetAllActiveKeysAsync();
                    foreach (var key in keys)
                    {
                        _keyTrackers.TryAdd(key.Id, new KeyUsageTracker { Key = key });
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}