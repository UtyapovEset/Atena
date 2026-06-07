using System.Net.Http.Json;
using System.Text.Json.Serialization;
using WebAtena.Models;

namespace WebAtena.Services
{
    public class RecommendationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public RecommendationService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["AiServiceSettings:BaseUrl"];
        }

        public async Task<List<Product>> RankProducts(string userId, List<Product> products, int activityScore)
        {
            try
            {
                var requestProducts = products.Select(p => new
                {
                    product_id = p.Id,
                    price = (double)p.Price,
                    product_popularity = p.Stock > 5 ? 10 : 1,
                    user_activity = activityScore
                }).ToList();

             
                long numericUserId = userId == "anonymous" ? 0 : Math.Abs(userId.GetHashCode());

                var url = $"{_baseUrl}/rank_feed?user_id={numericUserId}";

                var response = await _httpClient.PostAsJsonAsync(url, requestProducts);

                if (!response.IsSuccessStatusCode) return products;

                var result = await response.Content.ReadFromJsonAsync<List<RankedProduct>>();

                if (result == null || !result.Any()) return products;

                var sortedIds = result.Select(x => x.ProductId).ToList();

                return products
                    .OrderBy(p => sortedIds.IndexOf(p.Id))
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Service Error: {ex.Message}");
                return products;
            }
        }
    }

    public class RankedProduct
    {
        [JsonPropertyName("product_id")]
        public int ProductId { get; set; }
        [JsonPropertyName("class_id")]
        public int ClassId { get; set; }
    }
}