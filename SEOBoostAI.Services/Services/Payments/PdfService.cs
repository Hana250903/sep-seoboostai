using DinkToPdf;
using DinkToPdf.Contracts;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Payments
{
	public class PdfService : IPdfService
	{
		private readonly IConverter _converter;

		// Inject DinkToPdf Converter vào Service
		public PdfService(IConverter converter)
		{
			_converter = converter;
		}

		public byte[] GenerateReceiptPdf(PaymentReceiptDto data, string templatePath)
		{
			// 1. Kiểm tra file template có tồn tại không
			if (!File.Exists(templatePath))
			{
				throw new FileNotFoundException($"Không tìm thấy file mẫu tại: {templatePath}");
			}

			// 2. Đọc nội dung HTML (Đọc Sync vì DinkToPdf cũng chạy Sync)
			string htmlContent = File.ReadAllText(templatePath);

			// 3.Đọc file ảnh từ ổ cứng
			byte[] imageArray = File.ReadAllBytes("Resources/Images/image.png");
			string base64ImageRepresentation = Convert.ToBase64String(imageArray);

			// 4. Điền dữ liệu vào HTML (Replace)
			var sb = new StringBuilder(htmlContent);
			sb.Replace("{{TransactionCode}}", data.TransactionCode);
			sb.Replace("{{CustomerName}}", data.PayerName);
			sb.Replace("{{CustomerEmail}}", data.PayerEmail);
			sb.Replace("{{PaymentDate}}", data.PaymentDate.ToString("dd/MM/yyyy"));
			sb.Replace("{{ServiceName}}", data.ServiceName);
			sb.Replace("{{Description}}", data.Description);
			// Format tiền tệ N0: 100,000
			sb.Replace("{{Quantity}}", data.Quantity.ToString());
			sb.Replace("{{Amount}}", data.Amount.ToString("N0"));
			sb.Replace("{{TotalAmount}}", data.TotalAmount.ToString("N0"));

			// Bạn có thể thêm các field VAT nếu DTO có
			sb.Replace("{{VatRate}}", data.VatRate.ToString());
			sb.Replace("{{VatAmount}}", data.VatAmount.ToString("N0"));

			// Replace trong HTML
			sb.Replace("{{LogoBase64}}", "data:image/png;base64," + base64ImageRepresentation);

			// 5. Cấu hình PDF
			var doc = new HtmlToPdfDocument()
			{
				GlobalSettings = {
				ColorMode = ColorMode.Color,
				Orientation = Orientation.Portrait,
				PaperSize = PaperKind.A4,
				Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
			},
				Objects = {
				new ObjectSettings {
					PagesCount = true,
					HtmlContent = sb.ToString(), // HTML đã có dữ liệu
                    WebSettings = { DefaultEncoding = "utf-8" }
				}
			}
			};

			// 6. Convert sang byte[]
			return _converter.Convert(doc);
		}
	}
}
