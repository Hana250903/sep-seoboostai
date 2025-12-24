using SEOBoostAI.Repository.Enums;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
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
		private readonly IUserRepository _userRepository;
		private readonly IPurchasedFeatureRepository _purchasedFeatureRepository;
		private readonly IUserMonthlyFreeQuotaRepository _userMonthlyFreeQuotaRepository;
		private readonly ISystemConfigService _systemConfigService;
		private readonly IUnitOfWork _unitOfWork;
		private readonly decimal _vatRate;
		public TransactionService(ITransactionRepository transactionRepository, IUnitOfWork unitOfWork, IUserRepository userRepository,
			IFeatureRepository featureRepository, IUserMonthlyFreeQuotaRepository userMonthlyFreeQuotaRepository, 
			IPurchasedFeatureRepository purchasedFeatureRepository, ISystemConfigService systemConfigService)
		{
			_transactionRepository = transactionRepository;
			_unitOfWork = unitOfWork;
			_featureRepository = featureRepository;
			_userRepository = userRepository;
			_userMonthlyFreeQuotaRepository = userMonthlyFreeQuotaRepository;
			_purchasedFeatureRepository = purchasedFeatureRepository;
			_systemConfigService = systemConfigService;
			_vatRate = _systemConfigService.GetValue<decimal>("VAT", 0);
		}
		public async Task<PaginationResult<List<Transaction>>> GetTransactionsWithPaginateAsync(int currentPage, int pageSize)
		{
			return await _transactionRepository.GetTransactionsWithPaginateAsync(currentPage, pageSize);
		}
		public async Task<PaginationResult<List<Transaction>>> GetTransactionsByUserWithPaginateAsync(int currentPage, int pageSize)
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
		public async Task<Transaction> CreatePendingDeposit(int userId, decimal amount, string paymentMethod, string gatewayTransactionId, long orderCode)
		{
			var newTransaction = new Transaction
			{
				UserID = userId,
				Money = amount,
				PaymentMethod = paymentMethod,
				Type = PaymentType.DEPOSIT.ToString(),
				Description = "Nạp tiền vào ví qua PayOS",
				GatewayTransactionId = gatewayTransactionId,
				Status = PaymentStatus.PENDING.ToString(), // Trạng thái quan trọng
				RequestTime = DateTime.UtcNow,
				IsDeleted = false,
				OrderCode = orderCode,
				Quantity = 1
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
				var transaction = await _transactionRepository.GetByGatewayTransactionIdAsync(gatewayTransactionId);

				if (transaction == null)
				{
					// Lúc này sẽ không bị lỗi nữa vì DB và Webhook đều dùng chung 1 mã chuỗi
					throw new Exception($"Giao dịch với mã {gatewayTransactionId} không tồn tại.");
				}

				if (transaction.Status == PaymentStatus.PENDING.ToString())
				{
					transaction.Status = status;
					transaction.BankTransId = bankTransId;
					transaction.CompletedTime = DateTime.UtcNow;
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
				BalanceAfter = (decimal)t.BalanceAfter, // Giả sử tất cả đều là VND
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

			// 2. Tính toán tiền và thuế 
			decimal basePrice = feature.Price * quantity;
			// Tính tiền thuế
			decimal taxAmount = basePrice * (_vatRate / 100m);
			// Tổng tiền phải trả
			decimal totalCost = basePrice + taxAmount;

			// 2. Lấy Currency của người dùng
			var user = await _userRepository.GetByIdAsync(userId);

			// 3. KIỂM TRA SỐ DƯ
			if (user.Currency < totalCost)
			{
				throw new InvalidOperationException($"Số dư không đủ. Tổng đơn: {totalCost:N0}đ (Đã bao gồm VAT {_vatRate}%). Số dư hiện tại: {user.Currency:N0}đ.");
			}

			// --- BẮT ĐẦU GIAO DỊCH (UnitOfWork sẽ đảm bảo tất cả cùng thành công hoặc cùng thất bại) ---

			// 4. Trừ tiền
			user.Currency -= totalCost;
			await _userRepository.UpdateAsync(user);

			long orderCode = long.Parse(DateTime.UtcNow.ToString("yyMMddHHmmss") + new Random().Next(100, 999));

			// 5. Tạo Transaction ghi nhận việc trừ tiền
			var transaction = new Transaction
			{
				UserID = user.UserID,
				Money = totalCost, // Số tiền bị trừ
				GatewayTransactionId = "U_" + Guid.NewGuid().ToString("N"), // Mã giao dịch nội bộ
				BankTransId = null,
				Type = PaymentType.PURCHASE.ToString(), // Loại giao dịch Mua hàng
				Status = PaymentStatus.COMPLETED.ToString(), // Mua bằng Currency của user nên thành công ngay
				Description = $"Mua {quantity} lượt {feature.Name} (Giá: {basePrice:N0}đ + VAT {_vatRate}%: {taxAmount:N0}đ)",
				PaymentMethod = "Account Balance",
				RequestTime = DateTime.UtcNow,
				CompletedTime = DateTime.UtcNow,
				IsDeleted = false,
				BalanceAfter = user.Currency,
				OrderCode = orderCode,
				Quantity = quantity
			};
			await _transactionRepository.CreateAsync(transaction);
			// Lưu ý: Phải SaveChanges 1 lần ở đây để lấy TransactionID cho bảng PurchasedFeatures
			await _unitOfWork.SaveChangesAsync();

			// 6. Lưu vào bảng PurchasedFeatures
			var purchasedFeature = new PurchasedFeature
			{
				FeatureID = featureId,
				TransactionID = transaction.TransactionID, // Link với giao dịch vừa tạo
				TotalQuantity = quantity,
				RemainingQuantity = quantity, // (Nếu bạn muốn track riêng)
				PurchaseDate = DateTime.UtcNow,
				IsDeleted = false
			};

			await _purchasedFeatureRepository.CreateAsync(purchasedFeature);

			// 8. Lưu tất cả thay đổi cuối cùng
			await _unitOfWork.SaveChangesAsync();
		}

		public async Task<Transaction> GetByGatewayTransactionIdAsync(string gatewayTransactionId)
		{
			return await _transactionRepository.GetByGatewayTransactionIdAsync(gatewayTransactionId);
		}


		public async Task<Transaction> CreateAdminDepositAsync(int userId, decimal amount, string description)
		{
			// 1. Tìm User để cộng tiền
			var user = await _userRepository.GetByIdAsync(userId);
			if (user == null)
			{
				throw new Exception($"Không tìm thấy User có ID: {userId}");
			}

			// 2. TÍNH TOÁN: Cộng tiền vào ví
			user.Currency += amount;
			await _userRepository.UpdateAsync(user);

			// 3. TẠO TRANSACTION (Ghi log lịch sử)
			var transaction = new Transaction
			{
				UserID = userId,
				Money = amount,
				GatewayTransactionId = "ADMIN_" + Guid.NewGuid().ToString("N"), // Random ngẫu nhiên
				PaymentMethod = "Account Balance (by Admin)",
				Type = PaymentType.DEPOSIT.ToString(),
				Status = PaymentStatus.COMPLETED.ToString(), // Thành công ngay lập tức
				Description = string.IsNullOrEmpty(description) ? "Admin nạp tiền thủ công" : description,
				IsDeleted = false,
				BalanceAfter = user.Currency,
				RequestTime = DateTime.UtcNow,
				CompletedTime = DateTime.UtcNow
			};

			await _transactionRepository.CreateAsync(transaction);

			// 4. Lưu tất cả thay đổi (User + Transaction) cùng lúc
			await _unitOfWork.SaveChangesAsync();

			return transaction;
		}

		public async Task<PaymentReceiptDto> GetReceiptAsync(int transactionId, int currentUserId, string userRole)
		{
			// 1. Lấy dữ liệu từ Repo
			var trans = await _transactionRepository.GetTransactionDetailAsync(transactionId);

			if (trans == null) throw new Exception("Không tìm thấy giao dịch.");

			// 2. Bảo mật: Chỉ chủ sở hữu hoặc Admin mới được xem hóa đơn này
			if (userRole != UserRole.Admin.ToString() && trans.UserID != currentUserId)
			{
				throw new Exception("Bạn không có quyền xem hóa đơn này.");
			}

			// 3. Xử lý tên dịch vụ và tính toán 
			string serviceName = "";
			string description = "";
			decimal totalAmount = trans.Money;
			decimal subTotal = Math.Round(totalAmount / (1 + _vatRate / 100m), 0);
			decimal vatAmount = totalAmount - subTotal;

			if (trans.Type == PaymentType.DEPOSIT.ToString())
			{
				serviceName = "Nạp tiền vào tài khoản";
				description = trans.Description;	
			}

			if(trans.Type == PaymentType.PURCHASE.ToString()) // PURCHASE
			{
				// Bạn có thể lấy description
				serviceName = "Mua dịch vụ";
				description = trans.Description;

				if (_vatRate > 0)
				{
					subTotal = Math.Round(totalAmount / (1 + _vatRate / 100m), 0);
					vatAmount = totalAmount - subTotal;
				}
			}

			// 5. Map sang DTO
			return new PaymentReceiptDto
			{
				TransactionCode = trans.OrderCode.ToString(), 
				Status = trans.Status,
				PaymentDate = trans.CompletedTime ?? trans.RequestTime,

				PayerName = trans.User.FullName,
				PayerEmail = trans.User.Email,

				PaymentMethod = trans.PaymentMethod,
				BankName = "Chuyển khoản ngân hàng",

				ServiceName = serviceName,
				Description = description,
				Quantity = trans.Quantity,
				Amount = subTotal, // Tạm tính

				VatRate = _vatRate,
				VatAmount = vatAmount,
				TotalAmount = totalAmount // Tổng cộng
			};
		}
	}
}