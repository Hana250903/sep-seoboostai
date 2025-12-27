using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Service.Services.Interfaces;
using System.IO;

namespace SEOBoostAI.Service.Services.Payments
{
	public class PdfService : IPdfService
	{
		public PdfService() { }

		public byte[] GenerateReceiptPdf(PaymentReceiptDto data)
		{
			// 1. Load Logo
			byte[] logoBytes = null;
			string logoPath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Images", "image.png");

			if (File.Exists(logoPath))
			{
				logoBytes = File.ReadAllBytes(logoPath);
			}

			var primaryColor = "#2980b9";

			// 2. Tạo Document
			var document = Document.Create(container =>
			{
				container.Page(page =>
				{
					page.Size(PageSizes.A5);
					page.Margin(1, Unit.Centimetre);
					page.PageColor(Colors.White);
					page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

					// === HEADER ===
					page.Header().PaddingBottom(10).Row(row =>
					{
						if (logoBytes != null)
							row.ConstantItem(100).Image(logoBytes);
						else
							row.ConstantItem(100).Text("SEO BOOST AI").Bold().FontSize(14).FontColor(primaryColor);

						row.RelativeItem().AlignRight().Column(col =>
						{
							col.Item().Text("HÓA ĐƠN").FontSize(20).Bold().FontColor("#2c3e50");
							col.Item().PaddingTop(2).Background("#27ae60").PaddingHorizontal(10).PaddingVertical(2)
							   .Text("ĐÃ THANH TOÁN").FontColor(Colors.White).Bold().FontSize(9);
							col.Item().PaddingTop(5).Text($"Mã đơn: #{data.TransactionCode}").Bold();
							col.Item().Text($"Ngày: {data.PaymentDate:dd/MM/yyyy HH:mm}");
						});
					});

					// === CONTENT ===
					page.Content().PaddingVertical(10).Column(col =>
					{
						// Info Khách hàng
						col.Item().Column(c =>
						{
							c.Item().Text("KHÁCH HÀNG").FontSize(9).Bold().FontColor(primaryColor);
							c.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten5);
							c.Item().PaddingTop(5).Text(data.PayerName).Bold().FontSize(11);
							c.Item().Text($"Email: {data.PayerEmail}");
						});

						col.Spacing(20);

						// Bảng chi tiết
						col.Item().Table(table =>
						{
							table.ColumnsDefinition(columns =>
							{
								columns.RelativeColumn(4);  // Diễn giải
								columns.ConstantColumn(30); // SL
								columns.RelativeColumn(3);  // Giá gốc
								columns.RelativeColumn(3);  // VAT(%)
								columns.RelativeColumn(3);  // Tiền thuế
								columns.RelativeColumn(3);  // Thành tiền
							});

							table.Header(header =>
							{
								header.Cell().Element(HeaderStyle).Text("Diễn giải");
								header.Cell().Element(HeaderStyle).AlignRight().Text("SL");
								header.Cell().Element(HeaderStyle).AlignRight().Text("Giá gốc");
								header.Cell().Element(HeaderStyle).AlignRight().Text("VAT(%)");
								header.Cell().Element(HeaderStyle).AlignRight().Text("Tiền thuế");
								header.Cell().Element(HeaderStyle).AlignRight().Text("Thành tiền");

								IContainer HeaderStyle(IContainer container)
								{
									return container.Background(primaryColor)
													.PaddingVertical(5).PaddingHorizontal(5)
													.DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White));
								}
							});

							// Dữ liệu dòng
							table.Cell().Element(ItemStyle).Column(c => {
								c.Item().Text(data.ServiceName).Bold();
								if (!string.IsNullOrEmpty(data.Description))
									c.Item().Text(data.Description).FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
							});
							table.Cell().Element(ItemStyle).AlignRight().Text(data.Quantity.ToString());
							table.Cell().Element(ItemStyle).AlignRight().Text($"{data.Amount:N0} đ");
							table.Cell().Element(ItemStyle).AlignRight().Text($"{data.VatRate}%");
							table.Cell().Element(ItemStyle).AlignRight().Text($"{data.VatAmount:N0} đ");
							table.Cell().Element(ItemStyle).AlignRight().Text($"{data.TotalAmount:N0} đ");

							static IContainer ItemStyle(IContainer container)
							{
								return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
												.PaddingVertical(5).PaddingHorizontal(5);
							}

							// === PHẦN FOOTER CÓ HIỂN THỊ CÔNG THỨC ===
							table.Footer(footer =>
							{
								// 1. Cộng tiền hàng
								footer.Cell().ColumnSpan(5).AlignRight().PaddingVertical(2).Text("Cộng tiền hàng:").FontSize(10);
								footer.Cell().AlignRight().PaddingVertical(2).Text($"{data.Amount:N0} đ").FontSize(10).Bold();

								// 2. Thuế GTGT
								if (data.VatRate >= 0)
								{
									footer.Cell().ColumnSpan(5).AlignRight().PaddingVertical(2).Text($"Tiền thuế GTGT ({data.VatRate}%):").FontSize(10);
									footer.Cell().AlignRight().PaddingVertical(2).Text($"{data.VatAmount:N0} đ").FontSize(10).Bold();
								}

								// 3. TỔNG THANH TOÁN (Kèm công thức)
								footer.Cell().ColumnSpan(6).PaddingTop(10).Element(GrandTotalStyle).Row(row =>
								{
									// Bên trái: Tiêu đề + Công thức nhỏ bên dưới
									row.RelativeItem().Column(c =>
									{
										c.Item().Text("TỔNG THANH TOÁN:").Bold().FontColor(Colors.White);
										// --- DÒNG CÔNG THỨC Ở ĐÂY ---
										c.Item().Text("(Cộng tiền hàng + Tiền thuế)").FontSize(8).Italic().FontColor(Colors.White);
									});

									// Bên phải: Số tiền
									row.RelativeItem().AlignRight().Text($"{data.TotalAmount:N0} đ").FontSize(14).Bold().FontColor(Colors.White);
								});

								IContainer GrandTotalStyle(IContainer container)
								{
									return container.Background("#2980b9").Padding(10);
								}
							});
						});
					});

					// === FOOTER ===
					page.Footer().AlignCenter().Column(col =>
					{
						col.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten5);
						col.Item().PaddingTop(5).Text("Cảm ơn quý khách đã sử dụng dịch vụ của SEOBoostAI.").FontSize(9);
						col.Item().Text("Biên lai điện tử được xuất tự động.").FontSize(9).Italic();
					});
				});
			});

			return document.GeneratePdf();
		}
	}
}