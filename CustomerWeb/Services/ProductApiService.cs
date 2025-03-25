using System.Net.Http.Json;
using CustomerWeb.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CustomerWeb.Services;

public class ProductApiService : IProductApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductApiService> _logger;
    private readonly IMemoryCache _cache;

    // Cache keys
    private const string PRODUCTS_CACHE_KEY = "products";
    private const string CATEGORIES_CACHE_KEY = "categories";
    private const string LAST_UPDATE_KEY = "last_stock_update";
    private static readonly TimeSpan DEFAULT_CACHE_DURATION = TimeSpan.FromMinutes(5);

    public ProductApiService(
        HttpClient httpClient,
        ILogger<ProductApiService> logger,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;

        // Initialize last update time if not set
        if (!_cache.TryGetValue(LAST_UPDATE_KEY, out _))
        {
            _cache.Set(LAST_UPDATE_KEY, DateTime.Now);
        }
    }

    /// <summary>
    /// Gets the timestamp of the last stock update
    /// </summary>
    public DateTime GetLastUpdateTime()
    {
        if (_cache.TryGetValue(LAST_UPDATE_KEY, out DateTime lastUpdate))
        {
            return lastUpdate;
        }

        // If for some reason it's not in cache, set it to now
        var now = DateTime.Now;
        _cache.Set(LAST_UPDATE_KEY, now);
        return now;
    }

    /// <summary>
    /// Gets products with optional filtering and search
    /// </summary>
    public async Task<IEnumerable<ProductViewModel>> GetProductsAsync(
        string? searchTerm = null,
        int? categoryId = null,
        int? brandId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null)
    {
        try
        {
            // For filtered results, we don't use cache
            if (!string.IsNullOrEmpty(searchTerm) || categoryId.HasValue ||
                brandId.HasValue || minPrice.HasValue || maxPrice.HasValue)
            {
                return await FetchProductsFromApiAsync(searchTerm, categoryId, brandId, minPrice, maxPrice);
            }

            // For all products, try to get from cache first
            if (!_cache.TryGetValue(PRODUCTS_CACHE_KEY, out List<ProductViewModel> products))
            {
                _logger.LogInformation("Products not found in cache. Fetching from API...");
                products = (await FetchProductsFromApiAsync(null, null, null, null, null)).ToList();

                // If no products found and this is a request for all products, try to sync
                if (!products.Any())
                {
                    _logger.LogWarning("No products found when requesting all products. Attempting to trigger sync...");

                    await TriggerProductSyncAsync();

                    // Try fetching again after sync
                    products = (await FetchProductsFromApiAsync(null, null, null, null, null)).ToList();

                    if (products.Any())
                    {
                        _logger.LogInformation("Successfully retrieved {Count} products after sync", products.Count);
                    }
                    else
                    {
                        _logger.LogWarning("No products found even after sync attempt");
                    }
                }

                // Store in cache
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(DEFAULT_CACHE_DURATION);

                _cache.Set(PRODUCTS_CACHE_KEY, products, cacheOptions);
                _logger.LogInformation("Cached {Count} products", products.Count);
            }
            else
            {
                _logger.LogInformation("Retrieved {Count} products from cache", products.Count);
            }

            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetProductsAsync");
            return Array.Empty<ProductViewModel>();
        }
    }

    /// <summary>
    /// Gets a single product by ID
    /// </summary>
    public async Task<ProductViewModel?> GetProductByIdAsync(int id)
    {
        string cacheKey = $"product_{id}";

        try
        {
            // Try getting from cache first
            if (!_cache.TryGetValue(cacheKey, out ProductViewModel? product))
            {
                _logger.LogInformation("Product {Id} not found in cache. Fetching from API...", id);

                try
                {
                    product = await _httpClient.GetFromJsonAsync<ProductViewModel>($"api/Product/{id}");

                    if (product != null)
                    {
                        // Store in cache
                        var cacheOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(DEFAULT_CACHE_DURATION);

                        _cache.Set(cacheKey, product, cacheOptions);
                        _logger.LogInformation("Cached product {Id}", id);
                    }
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("Product with ID {Id} not found", id);
                    return null;
                }
            }
            else
            {
                _logger.LogInformation("Retrieved product {Id} from cache", id);
            }

            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetProductByIdAsync for ID {Id}", id);
            return null;
        }
    }

    /// <summary>
    /// Gets all categories
    /// </summary>
    public async Task<IEnumerable<CategoryViewModel>> GetCategoriesAsync()
    {
        try
        {
            // Try getting from cache first
            if (!_cache.TryGetValue(CATEGORIES_CACHE_KEY, out List<CategoryViewModel> categories))
            {
                _logger.LogInformation("Categories not found in cache. Fetching from API...");

                categories = await _httpClient.GetFromJsonAsync<List<CategoryViewModel>>("api/Category")
                    ?? new List<CategoryViewModel>();

                // Store in cache with longer duration since categories change less frequently
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1));

                _cache.Set(CATEGORIES_CACHE_KEY, categories, cacheOptions);
                _logger.LogInformation("Cached {Count} categories", categories.Count);
            }
            else
            {
                _logger.LogInformation("Retrieved {Count} categories from cache", categories.Count);
            }

            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetCategoriesAsync");
            return Array.Empty<CategoryViewModel>();
        }
    }

    /// <summary>
    /// Gets a single category by ID
    /// </summary>
    public async Task<CategoryViewModel?> GetCategoryByIdAsync(int id)
    {
        string cacheKey = $"category_{id}";

        try
        {
            // Try getting from cache first
            if (!_cache.TryGetValue(cacheKey, out CategoryViewModel? category))
            {
                _logger.LogInformation("Category {Id} not found in cache. Fetching from API...", id);

                try
                {
                    category = await _httpClient.GetFromJsonAsync<CategoryViewModel>($"api/Category/{id}");

                    if (category != null)
                    {
                        // Store in cache with longer duration since categories change less frequently
                        var cacheOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromHours(1));

                        _cache.Set(cacheKey, category, cacheOptions);
                        _logger.LogInformation("Cached category {Id}", id);
                    }
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("Category with ID {Id} not found", id);
                    return null;
                }
            }
            else
            {
                _logger.LogInformation("Retrieved category {Id} from cache", id);
            }

            return category;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetCategoryByIdAsync for ID {Id}", id);
            return null;
        }
    }

    /// <summary>
    /// Refreshes stock information for all products
    /// </summary>
    public async Task RefreshStockInformationAsync()
    {
        try
        {
            _logger.LogInformation("Refreshing stock information for all products");

            // Fetch all products from API with fresh data
            var products = await FetchProductsFromApiAsync(null, null, null, null, null);

            // Update cache with fresh data
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(DEFAULT_CACHE_DURATION);

            _cache.Set(PRODUCTS_CACHE_KEY, products.ToList(), cacheOptions);

            // Also update individual product caches
            foreach (var product in products)
            {
                string productCacheKey = $"product_{product.Id}";
                _cache.Set(productCacheKey, product, cacheOptions);
            }

            // Update the last refresh timestamp
            _cache.Set(LAST_UPDATE_KEY, DateTime.Now);

            _logger.LogInformation("Successfully refreshed stock information for {Count} products at {Time}",
                products.Count(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing stock information");
            throw; // Rethrow to let the calling service handle it
        }
    }

    /// <summary>
    /// Manually triggers the API's product sync process
    /// </summary>
    public async Task TriggerProductSyncAsync()
    {
        try
        {
            _logger.LogInformation("Manually triggering product sync");

            // Call your API's debug endpoint
            var response = await _httpClient.PostAsync("api/Debug/sync-all-products", null);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Product sync triggered successfully");

                // Clear the cache to ensure fresh data
                _cache.Remove(PRODUCTS_CACHE_KEY);
            }
            else
            {
                _logger.LogWarning("Failed to trigger product sync. Status code: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering product sync");
            throw; // Rethrow to let the calling service handle it
        }
    }

    // Private helper method to fetch products from API
    private async Task<IEnumerable<ProductViewModel>> FetchProductsFromApiAsync(
    string? searchTerm = null,
    int? categoryId = null,
    int? brandId = null,
    decimal? minPrice = null,
    decimal? maxPrice = null)
{
    try
    {
        // Log the API request parameters
        _logger.LogInformation("Fetching products - SearchTerm: {SearchTerm}, CategoryId: {CategoryId}, BrandId: {BrandId}, MinPrice: {MinPrice}, MaxPrice: {MaxPrice}",
            searchTerm ?? "null", categoryId?.ToString() ?? "null", brandId?.ToString() ?? "null", minPrice?.ToString() ?? "null", maxPrice?.ToString() ?? "null");

        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            queryParams.Add($"category_name={Uri.EscapeDataString(searchTerm)}"); // Use category_name for search term
        }

        if (categoryId.HasValue)
        {
            queryParams.Add($"category_id={categoryId}"); // Send category_id
        }

        if (brandId.HasValue)
        {
            queryParams.Add($"brand_id={brandId}");
        }

        if (minPrice.HasValue)
        {
            queryParams.Add($"min_price={minPrice}");
        }

        if (maxPrice.HasValue)
        {
            queryParams.Add($"max_price={maxPrice}");
        }

        var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        var fullUrl = $"api/Product{queryString}"; // Corrected URL

        // Log the full API request URL
        _logger.LogInformation("API request URL: {Url}", fullUrl);

        // Call the API
        var response = await _httpClient.GetAsync(fullUrl);
        _logger.LogInformation("API response status code: {StatusCode}", response.StatusCode);

        var products = await response.Content.ReadFromJsonAsync<List<ProductViewModel>>();

        _logger.LogInformation("Retrieved {Count} products from API", products?.Count ?? 0);
        return products ?? new List<ProductViewModel>();
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "HTTP error occurred while fetching products: {Message}", ex.Message);
        return Array.Empty<ProductViewModel>();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching products from API: {Message}", ex.Message);
        return Array.Empty<ProductViewModel>();
    }
}
}