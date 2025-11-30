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

namespace SEOBoostAI.Service.Services
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
        private int _currentKeyIndex = 0;

        public GeminiRateLimitManager(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            // KHÔNG gọi InitializeKeysAsync() ở đây để tránh async void và lỗi scope
            // Chúng ta sẽ load lazy trong GetAvailableKeyAsync
        }

        // Hàm helper để lấy Repository trong một scope ngắn hạn
        private async Task InitializeKeysAsync()
        {
            // Tạo một scope mới chỉ tồn tại trong block using này
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
            } // Scope kết thúc tại đây, DbContext được giải phóng
        }

        public async Task<GeminiKey> GetAvailableKeyAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                // Lazy loading: Chỉ load keys lần đầu tiên khi cần dùng
                if (_keyTrackers.IsEmpty)
                {
                    await InitializeKeysAsync();
                }

                if (_keyTrackers.IsEmpty)
                {
                    throw new InvalidOperationException("Không có API key nào khả dụng trong database.");
                }

                var now = DateTime.UtcNow;
                var today = DateTime.UtcNow.Date;
                var maxWaitTime = TimeSpan.FromSeconds(10);
                var startTime = DateTime.UtcNow;

                while (true)
                {
                    foreach (var tracker in _keyTrackers.Values.OrderBy(t => t.Key.Id))
                    {
                        // 1. Check Rate Limit tạm thời
                        if (tracker.IsRateLimited && tracker.RateLimitedUntil.HasValue)
                        {
                            if (now > tracker.RateLimitedUntil.Value)
                            {
                                tracker.IsRateLimited = false;
                                tracker.RateLimitedUntil = null;
                            }
                            else continue;
                        }

                        // 2. Reset counter theo phút
                        if ((now - tracker.LastMinuteReset).TotalMinutes >= 1)
                        {
                            tracker.RequestTimestamps.Clear();
                            tracker.TokensUsedInMinute = 0;
                            tracker.LastMinuteReset = now;
                        }

                        // 3. Reset counter theo ngày (Database update cần scope)
                        if (tracker.Key.LastResetDate.Date < today)
                        {
                            using (var scope = _scopeFactory.CreateScope())
                            {
                                var repo = scope.ServiceProvider.GetRequiredService<IGeminiKeyRepository>();
                                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                                tracker.Key.RequestsUsedToday = 0;
                                tracker.Key.TokensUsedToday = 0;
                                tracker.Key.LastResetDate = today;

                                await repo.UpdateKeyUsageAsync(tracker.Key.Id, 0, 0, today);
                                await uow.SaveChangesAsync();
                            }
                        }

                        // 4. Clean up timestamps cũ
                        while (tracker.RequestTimestamps.Count > 0 && (now - tracker.RequestTimestamps.Peek()).TotalMinutes >= 1)
                        {
                            tracker.RequestTimestamps.Dequeue();
                        }

                        // 5. Kiểm tra các giới hạn
                        if (tracker.RequestTimestamps.Count >= tracker.Key.RpmLimit) continue;
                        if (tracker.Key.RequestsUsedToday >= tracker.Key.RpdLimit) continue;
                        if (tracker.TokensUsedInMinute >= tracker.Key.TpmLimit) continue;

                        return tracker.Key;
                    }

                    if (DateTime.UtcNow - startTime > maxWaitTime)
                    {
                        throw new InvalidOperationException("Tất cả API keys đều đang bận.");
                    }

                    await Task.Delay(100);
                }
            }
            finally
            {
                _semaphore.Release();
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

                    // Ghi xuống DB (Tạo Scope Mới)
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var repo = scope.ServiceProvider.GetRequiredService<IGeminiKeyRepository>();
                        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                        await repo.UpdateKeyUsageAsync(keyId, 1, estimatedTokens, DateTime.UtcNow.Date);
                        await uow.SaveChangesAsync();
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task MarkKeyRateLimitedAsync(int keyId)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_keyTrackers.TryGetValue(keyId, out var tracker))
                {
                    tracker.IsRateLimited = true;
                    tracker.RateLimitedUntil = DateTime.UtcNow.AddMinutes(1);

                    // Ghi xuống DB (Tạo Scope Mới)
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var repo = scope.ServiceProvider.GetRequiredService<IGeminiKeyRepository>();
                        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                        await repo.MarkKeyRateLimitedAsync(keyId, tracker.RateLimitedUntil.Value);
                        await uow.SaveChangesAsync();
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task ReloadKeysAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                // Clear trackers cũ
                _keyTrackers.Clear();

                // Gọi hàm InitializeKeysAsync (hàm này đã tự tạo scope bên trong)
                await InitializeKeysAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}