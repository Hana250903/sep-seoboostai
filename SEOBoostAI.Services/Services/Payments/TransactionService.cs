using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Payments
{
	public class TransactionService : ITransactionService
	{
		private readonly ITransactionRepository _transactionRepository;
		private readonly IFeatureRepository _featureRepository;
		private readonly IWalletRepository _walletRepository;
		private readonly IPurchasedFeatureRepository _purchasedFeatureRepository;
		private readonly IUserMonthlyFreeQuotaRepository _userMonthlyFreeQuotaRepository;
		private readonly IUnitOfWork _unitOfWork;
		public TransactionService(ITransactionRepository transactionRepository, IUnitOfWork unitOfWork, 
			IFeatureRepository featureRepository, IUserMonthlyFreeQuotaRepository userMonthlyFreeQuotaRepository, 
			IWalletRepository walletRepository, IPurchasedFeatureRepository purchasedFeatureRepository)
		{
			_transactionRepository = transactionRepository;
			_unitOfWork = unitOfWork;
			_featureRepository = featureRepository;
			_userMonthlyFreeQuotaRepository = userMonthlyFreeQuotaRepository;
			_walletRepository = walletRepository;
			_purchasedFeatureRepository = purchasedFeatureRepository;
		}
		public async Task<PaginationResult<List<Transaction>>> GetTransactionsWithPaginateAsync(int currentPage, int pageSize)
		{
			return await _transactionRepository.GetTransactionsWithPaginateAsync(currentPage, pageSize);
		}
		public async Task<Transaction> GetTransactionByIdAsync(int id)
		{
			return await _transactionRepository.GetByIdAsync(id);
		}
		public async Task<List<Transaction>> GetTransactionsAsync()
		{
			return await _transactionRepository.GetAllAsync();
		}
		public async Task CreateAsync(Transaction transaction)
		{
			try
			{
				await _transactionRepository.CreateAsync(transaction);
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				throw;
			}
		}
		public async Task UpdateAsync(Transaction transaction)
		{
			try
			{
				_transactionRepository.UpdateAsync(transaction);
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				throw;
			}
		}
		public async Task DeleteAsync(int id)
		{
			try
			{
				var transaction = await _transactionRepository.GetByIdAsync(id);
				if (transaction != null)
				{
					_transactionRepository.RemoveAsync(transaction);
					await _unitOfWork.SaveChangesAsync();
				}
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		// HÀM MỚI CHO PAYOS
		public async Task<Transaction> CreatePendingDeposit(int walletId, decimal amount, string paymentMethod, string gatewayTransactionId)
		{
			var newTransaction = new Transaction
			{
				WalletID = walletId,
				Money = amount,
				PaymentMethod = paymentMethod,
				Type = "DEPOSIT",
				Description = "Nạp tiền vào ví qua PayOS",
				GatewayTransactionId = gatewayTransactionId,
				Status = "PENDING", // Trạng thái quan trọng
				RequestTime = DateTime.UtcNow.AddHours(7),
				IsDeleted = false
				// GatewayTransactionId, BankTransId, CompletedTime sẽ được cập nhật bởi Webhook
			};

			// Dùng hàm CreateAsync đã có của bạn (vì nó tự SaveChanges)
			// Điều này tốt vì chúng ta cần ID ngay lập tức
			await CreateAsync(newTransaction);

			// Sau khi CreateAsync, newTransaction đã có TransactionID từ CSDL
			return newTransaction;
		}

		// HÀM MỚI: Xử lý cập nhật trạng thái thanh toán
		public async Task UpdateTransactionStatusAsync(string gatewayTransactionId, string status, string payOSReference, string bankTransId)
		{
			try
			{
				// Tìm trong DB bằng cái mã chuỗi "628f..."
				var transaction = await _transactionRepository.GetByGatewayTransactionIdAsync(gatewayTransactionId);

				if (transaction == null)
				{
					// Lúc này sẽ không bị lỗi nữa vì DB và Webhook đều dùng chung 1 mã chuỗi
					throw new Exception($"Giao dịch với mã {gatewayTransactionId} không tồn tại.");
				}

				if (transaction.Status == "PENDING")
				{
					transaction.Status = status;
					transaction.BankTransId = bankTransId;
					transaction.CompletedTime = DateTime.UtcNow.AddHours(7);
					await UpdateAsync(transaction);
				}
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public async Task<PaginationResult<List<PaymentHistoryDto>>> GetUserPaymentHistoryAsync(int userId, int currentPage, int pageSize)
		{
			// 1. Gọi Repository
			var paginateResult = await _transactionRepository.GetSuccessfulDepositsByUserIdAsync(userId, currentPage, pageSize);

			// 2. Map (Chuyển đổi) từ Entity sang DTO
			var historyDtos = paginateResult.Items.Select(t => new PaymentHistoryDto
			{
				TransactionId = t.TransactionID,
				Amount = t.Money,
				Description = t.Description,
				Status = t.Status,
				PaymentDate = t.CompletedTime,
				PaymentMethod = t.PaymentMethod,
				GatewayTransactionId = t.GatewayTransactionId
			}).ToList();

			// 3. Trả về kết quả phân trang mới chứa DTO
			return new PaginationResult<List<PaymentHistoryDto>>
			{
				TotalItems = paginateResult.TotalItems,
				TotalPages = paginateResult.TotalPages,
				CurrentPage = paginateResult.CurrentPage,
				PageSize = paginateResult.PageSize,
				Items = historyDtos
			};
		}

		public async Task PurchaseFeatureAsync(int userId, int featureId, int quantity)
		{
			// 1. Lấy thông tin tính năng và giá tiền
			var feature = await _featureRepository.GetFeatureByIdAsync(featureId);
			if (feature == null) throw new Exception("Tính năng không tồn tại.");

			decimal totalCost = feature.Price * quantity;

			// 2. Lấy Ví người dùng
			// (Giả sử bạn đã viết hàm GetWalletByUserId trong Repository)
			var wallet = await _walletRepository.GetWalletByUserIdAsync(userId);
			if (wallet == null) throw new Exception("Ví không tồn tại.");

			// 3. KIỂM TRA SỐ DƯ
			if (wallet.Currency < totalCost)
			{
				throw new InvalidOperationException("Số dư trong ví không đủ để thực hiện giao dịch.");
			}

			// --- BẮT ĐẦU GIAO DỊCH (UnitOfWork sẽ đảm bảo tất cả cùng thành công hoặc cùng thất bại) ---

			// 4. Trừ tiền trong Ví
			wallet.Currency -= totalCost;
			wallet.UpdatedAt = DateTime.UtcNow.AddHours(7);
			_walletRepository.UpdateAsync(wallet);

			// 5. Tạo Transaction ghi nhận việc trừ tiền
			var transaction = new Transaction
			{
				WalletID = wallet.WalletID,
				Money = totalCost, // Số tiền bị trừ
				GatewayTransactionId = "W_" + Guid.NewGuid().ToString("N"), 
				BankTransId = null,
				Type = "PURCHASE", // Loại giao dịch Mua hàng
				Status = "COMPLETED", // Mua bằng ví nên thành công ngay
				Description = $"Mua {quantity} lượt {feature.Name}",
				PaymentMethod = "Wallet Balance",
				RequestTime = DateTime.UtcNow.AddHours(7),
				CompletedTime = DateTime.UtcNow.AddHours(7),
				IsDeleted = false
			};
			await _transactionRepository.CreateAsync(transaction);
			// Lưu ý: Phải SaveChanges 1 lần ở đây để lấy TransactionID cho bảng PurchasedFeatures
			// Hoặc nếu EF Core thông minh, nó sẽ tự map. An toàn nhất là SaveChanges luôn.
			await _unitOfWork.SaveChangesAsync();

			// 6. Lưu vào bảng PurchasedFeatures (Lịch sử mua hàng chi tiết)
			var purchasedFeature = new PurchasedFeature
			{
				FeatureID = featureId,
				TransactionID = transaction.TransactionID, // Link với giao dịch vừa tạo
				TotalQuantity = quantity,
				RemainingQuantity = quantity, // (Nếu bạn muốn track riêng)
				PurchaseDate = DateTime.UtcNow.AddHours(7),
				IsDeleted = false
			};
			// Giả sử bạn có Repository cho bảng này
			await _purchasedFeatureRepository.CreateAsync(purchasedFeature);

			// 8. Lưu tất cả thay đổi cuối cùng
			await _unitOfWork.SaveChangesAsync();
		}

		public async Task<Transaction> GetByGatewayTransactionIdAsync(string gatewayTransactionId)
		{
			return await _transactionRepository.GetByGatewayTransactionIdAsync(gatewayTransactionId);
		}
	}
}