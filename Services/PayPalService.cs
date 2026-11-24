using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace OnlineCoursePlatform.Services
{
    public class PayPalService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public PayPalService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        private string GetBaseUrl()
        {
            var mode = _configuration["PayPalSettings:Mode"];
            return mode == "live" ? "https://api-m.paypal.com" : "https://api-m.sandbox.paypal.com";
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var clientId = _configuration["PayPalSettings:ClientId"];
            var secret = _configuration["PayPalSettings:Secret"];

            var authToken = Encoding.ASCII.GetBytes($"{clientId}:{secret}");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));

            var body = new Dictionary<string, string> { { "grant_type", "client_credentials" } };
            var content = new FormUrlEncodedContent(body);

            var response = await _httpClient.PostAsync($"{GetBaseUrl()}/v1/oauth2/token", content);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);
            return doc.RootElement.GetProperty("access_token").GetString();
        }

        public async Task<string> CreateOrderAsync(decimal amount, string currency, string returnUrl, string cancelUrl)
        {
            var accessToken = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var orderRequest = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new
                    {
                        amount = new
                        {
                            currency_code = currency,
                            value = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                        }
                    }
                },
                application_context = new
                {
                    return_url = returnUrl,
                    cancel_url = cancelUrl
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(orderRequest), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{GetBaseUrl()}/v2/checkout/orders", content);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);

            // استخراج رابط الموافقة (Approval URL) لتوجيه المستخدم إليه
            var links = doc.RootElement.GetProperty("links").EnumerateArray();
            foreach (var link in links)
            {
                if (link.GetProperty("rel").GetString() == "approve")
                {
                    return link.GetProperty("href").GetString();
                }
            }

            throw new Exception("لم يتم العثور على رابط الموافقة من باي بال.");
        }

        public async Task<bool> CaptureOrderAsync(string orderId)
        {
            var accessToken = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var content = new StringContent("", Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{GetBaseUrl()}/v2/checkout/orders/{orderId}/capture", content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);
                var status = doc.RootElement.GetProperty("status").GetString();
                return status == "COMPLETED";
            }

            return false;
        }
    }
}