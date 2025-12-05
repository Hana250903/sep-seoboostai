using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Helpers
{
    /// <summary>
    /// Helper để fetch HTML từ URL, hỗ trợ bypass bot protection bằng Selenium ChromeDriver
    /// </summary>
    public class HtmlFetchHelper
    {
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
                catch (Exception ex)
                {
                    // Nếu gặp lỗi 403, 429, hoặc bất kỳ lỗi nào -> Fallback sang Selenium
                    Console.WriteLine($"⚠️ HttpClient failed ({ex.Message}). Falling back to Selenium...");
                }
            }

            // Dùng Selenium để bypass bot protection
            return await FetchWithSeleniumAsync(url, waitTimeMs);
        }

        /// <summary>
        /// Fetch HTML bằng HttpClient (nhanh nhưng dễ bị chặn)
        /// </summary>
        private static async Task<string> FetchWithHttpClientAsync(string url)
        {
            using (var client = new System.Net.Http.HttpClient())
            {
                // Giả lập User-Agent như browser thực
                client.DefaultRequestHeaders.Add("User-Agent", 
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9,vi;q=0.8");
                client.Timeout = TimeSpan.FromSeconds(30);

                var response = await client.GetAsync(url);
                
                // Nếu bị chặn (403, 429), throw exception để fallback sang Selenium
                if (!response.IsSuccessStatusCode)
                {
                    throw new System.Net.Http.HttpRequestException($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
                }

                return await response.Content.ReadAsStringAsync();
            }
        }

        /// <summary>
        /// Fetch HTML bằng Selenium ChromeDriver (chậm hơn nhưng bypass được bot protection)
        /// </summary>
        private static async Task<string> FetchWithSeleniumAsync(string url, int waitTimeMs)
        {
            // Cấu hình Chrome Driver
            var options = new ChromeOptions();
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

            try
            {
                using (var driver = new ChromeDriver(service, options))
                {
                    // Đặt timeout load trang là 30s
                    driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
                    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

                    // Điều hướng đến URL
                    driver.Navigate().GoToUrl(url);

                    // Đợi để JavaScript render xong (quan trọng với trang SPA)
                    await Task.Delay(waitTimeMs);

                    // Lấy toàn bộ Source HTML sau khi JS đã chạy
                    htmlContent = driver.PageSource;

                    Console.WriteLine($"✅ Selenium: Tải trang thành công! URL: {url} (Độ dài: {htmlContent.Length} ký tự)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Selenium Error: {ex.Message}");
                throw new Exception($"Không thể tải trang với Selenium: {ex.Message}", ex);
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
                using (var service = ChromeDriverService.CreateDefaultService())
                {
                    service.SuppressInitialDiagnosticInformation = true;
                    service.HideCommandPromptWindow = true;
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
