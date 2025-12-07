using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Helpers
{
    /// <summary>
    /// Helper để fetch HTML từ URL, hỗ trợ bypass bot protection bằng Selenium ChromeDriver
    /// </summary>
    public class HtmlFetchHelper
    {
        private static readonly HttpClient _httpClient;

        static HtmlFetchHelper()
        {
            var handler = new HttpClientHandler
            {
                // Tự động giải nén (Gzip/Brotli) để đọc được data từ các web hiện đại
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                AllowAutoRedirect = true
            };

            _httpClient = new HttpClient(handler);

            // Cấu hình Headers mặc định (Fake Browser)
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9,vi;q=0.8");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Fetch HTML từ URL với khả năng tự động fallback sang Selenium nếu bị chặn
        /// </summary>
        /// <param name="url">URL cần fetch</param>
        /// <param name="useSelenium">Bắt buộc dùng Selenium ngay (mặc định: false - thử HttpClient trước)</param>
        /// <param name="waitTimeMs">Thời gian đợi JavaScript render (ms), mặc định 2000ms</param>
        /// <returns>HTML content</returns>
        public static async Task<string> FetchHtmlAsync(string url, bool useSelenium = false, int waitTimeMs = 2000)
        {
            // Nếu không bắt buộc dùng Selenium, thử HttpClient trước (nhanh hơn)
            if (!useSelenium)
            {
                try
                {
                    return await FetchWithHttpClientAsync(url);
                }
                catch (HttpRequestException ex)
                {
                    // Logic thông minh: Chỉ Fallback khi bị Chặn (403) hoặc Quá tải (429)
                    // Nếu lỗi 404 (Not Found) thì Selenium cũng không cứu được, nên không cần thử.
                    if (ex.StatusCode == HttpStatusCode.Forbidden ||
                        ex.StatusCode == HttpStatusCode.TooManyRequests ||
                        ex.StatusCode == null) // null thường là lỗi mạng/DNS -> cứ thử Selenium cho chắc
                    {
                        Console.WriteLine($"HttpClient failed ({ex.StatusCode}). Falling back to Selenium...");
                        // Fallback xuống dưới để chạy Selenium
                    }
                    else
                    {
                        throw; // Lỗi 404, 500 thì ném ra luôn
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"General HttpClient Error: {ex.Message}. Falling back to Selenium...");
                }
            }

            // Dùng Selenium để bypass bot protection
            return await FetchWithSeleniumAsync(url, waitTimeMs);
        }

        /// <summary>
        /// Fetch HTML bằng HttpClient (nhanh nhưng dễ bị chặn) - Đã tối ưu Static
        /// </summary>
        private static async Task<string> FetchWithHttpClientAsync(string url)
        {
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                // Truyền StatusCode ra ngoài để hàm cha quyết định có fallback không
                throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}", null, response.StatusCode);
            }

            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Fetch HTML bằng Selenium ChromeDriver (chậm hơn nhưng bypass được bot protection)
        /// </summary>
        private static async Task<string> FetchWithSeleniumAsync(string url, int waitTimeMs)
        {
            // Cấu hình Chrome Driver
            var options = new ChromeOptions();
            options.PageLoadStrategy = PageLoadStrategy.Eager;
            options.AddArgument("--headless=new"); // Headless mode mới (Chrome 109+)
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage"); // Tránh lỗi thiếu RAM trên Linux/Docker
            options.AddArgument("--window-size=1920,1080"); // Giả lập màn hình PC full HD
            options.AddArgument("--disable-blink-features=AutomationControlled"); // Ẩn dấu vết Selenium

            // Giả lập User-Agent xịn để qua mặt Cloudflare
            options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36");

            // Thêm preferences để bypass detection
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalOption("useAutomationExtension", false);

            // Tắt bớt log rác của Selenium trên Console
            var service = ChromeDriverService.CreateDefaultService();
            service.SuppressInitialDiagnosticInformation = true;
            service.HideCommandPromptWindow = true;

            string htmlContent = null;

            using (var driver = new ChromeDriver(service, options))
            {
                try
                {
                    // Đặt timeout load trang là 30s
                    driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);

                    // Điều hướng đến URL
                    driver.Navigate().GoToUrl(url);

                    try
                    {
                        var wait = new WebDriverWait(driver, TimeSpan.FromMilliseconds(waitTimeMs));
                        wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
                    }
                    catch (WebDriverTimeoutException)
                    {
                        // Nếu hết giờ mà chưa complete thì kệ nó, vẫn lấy HTML (vì dùng Eager mode)
                        Console.WriteLine($"Selenium: Wait timeout ({waitTimeMs}ms), continuing...");
                    }

                    // Lấy toàn bộ Source HTML
                    htmlContent = driver.PageSource;
                    Console.WriteLine($"Selenium: Tải trang thành công! URL: {url} (Độ dài: {htmlContent.Length} ký tự)");
                }
                catch (WebDriverTimeoutException)
                {
                    // --- XỬ LÝ LỖI RENDERER TIMEOUT ---
                    // Nếu Chrome bị treo do script quảng cáo, ép dừng để lấy nội dung đã tải được
                    Console.WriteLine("Selenium: Renderer timeout. Executing window.stop()...");
                    try
                    {
                        ((IJavaScriptExecutor)driver).ExecuteScript("window.stop();");
                        htmlContent = driver.PageSource;
                    }
                    catch (Exception stopEx)
                    {
                        Console.WriteLine($"Failed to recover from timeout: {stopEx.Message}");
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Selenium Error: {ex.Message}");
                    throw new Exception($"Không thể tải trang với Selenium: {ex.Message}", ex);
                }
                finally
                {
                    driver.Quit();
                }
            }
            return htmlContent;
        }

        /// <summary>
        /// Kiểm tra xem ChromeDriver có sẵn không
        /// </summary>
        public static bool IsChromeDriverAvailable()
        {
            try
            {
                // Chỉ cần check file tồn tại hoặc thử khởi tạo service nhẹ
                using (var service = ChromeDriverService.CreateDefaultService())
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
