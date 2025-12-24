using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Enums
{
	// Enum 1: Trạng thái thanh toán
	public enum PaymentStatus
	{
		PENDING,
		COMPLETED,
		FAILED,
		CANCELED,
		PAID,
		EXPIRED
	}

	// Enum 2: Loại thanh toán
	public enum PaymentType
	{
		DEPOSIT,
		PURCHASE
	}

	// Enum 3: Vai trò người dùng
	public enum UserRole
	{
		Admin,
		Member,
		Staff
	}
}
