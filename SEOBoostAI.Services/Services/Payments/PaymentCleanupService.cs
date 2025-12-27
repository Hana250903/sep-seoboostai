using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SEOBoostAI.Repository.Enums;

namespace SEOBoostAI.Service.Services.Payments
{
	public class PaymentCleanupService : BackgroundService
	{
		// 1. CHỈ INJECT IServiceProvider (Singleton safe)
		// KHÔNG ĐƯỢC inject ITransactionRepository ở đây
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<PaymentCleanupService> _logger;

		public PaymentCleanupService(IServiceProvider serviceProvider, ILogger<PaymentCleanupService> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Payment Cleanup Service have start...");

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					// 2. TẠO SCOPE MỚI (Đây là chìa khóa để sửa lỗi)
					// Mỗi lần vòng lặp chạy, ta tạo 1 scope ngắn hạn để dùng Repository
					using (var scope = _serviceProvider.CreateScope())
					{
						// 3. LẤY REPOSITORY TỪ SCOPE NÀY
						var transactionRepository = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
						var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

						// --- BẮT ĐẦU LOGIC QUÉT DỌN ---
						var timeThreshold = DateTime.UtcNow.AddHours(7).AddMinutes(-15);

						//Bạn đã viết hàm GetExpiredPendingTransactionsAsync trong Repo
						// Nếu chưa có thì dùng: .Where(x => x.Status == "PENDING" && x.RequestTime < timeThreshold)
						var pendingTransactions = await transactionRepository.GetExpiredPendingTransactionsAsync(timeThreshold);

						if (pendingTransactions.Any())
						{
							foreach (var trans in pendingTransactions)
							{
								trans.Status = PaymentStatus.CANCELED.ToString();
								trans.Description += " [Auto-cancel by System]";
								trans.CompletedTime = DateTime.UtcNow.AddHours(7);

								await transactionRepository.UpdateAsync(trans);
							}

							await unitOfWork.SaveChangesAsync();
							_logger.LogInformation($"Đã dọn dẹp {pendingTransactions.Count} đơn hàng treo.");
						}
						// --- KẾT THÚC LOGIC ---
					}
					// Kết thúc khối 'using': Scope bị hủy, Repository và DbContext được giải phóng an toàn.
				}
				catch (Exception ex)
				{
					_logger.LogError($"Lỗi trong quá trình dọn dẹp: {ex.Message}");
				}

				// Ngủ 5 phút
				await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
			}
		}
	}
}