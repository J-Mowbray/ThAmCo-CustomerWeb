// Services/ProductApiService.cs
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using CustomerWeb.Models;

namespace CustomerWeb.Services;

public class ProductApiService : IProductApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductApiService> _logger;
    private readonly IMemoryCache _cache;

    public ProductApiService(HttpClient httpClient, ILogger<ProductApiService> logger, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
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
        // Create cache key based on parameters
        string cacheKey = $"products_{searchTerm}_{categoryId}_{brandId}_{minPrice}_{maxPrice}";
        
        // Check if in cache
        if (_cache.TryGetValue(cacheKey, out IEnumerable<ProductViewModel>? cachedProducts))
        {
            _logger.LogInformation("Retrieved products from cache");
            return cachedProducts!;
        }
        
        try
        {
            // Build query parameters to match API format
            var queryParams = new List<string>();
            
            if (!string.IsNullOrEmpty(searchTerm))
                queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");
                
            if (categoryId.HasValue)
                queryParams.Add($"categoryId={categoryId}");
                
            if (brandId.HasValue)
                queryParams.Add($"brandId={brandId}");
                
            if (minPrice.HasValue)
                queryParams.Add($"minPrice={minPrice}");
                
            if (maxPrice.HasValue)
                queryParams.Add($"maxPrice={maxPrice}");
                
            var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            
            // Call the API
            var products = await _httpClient.GetFromJsonAsync<List<ProductViewModel>>($"api/Product{queryString}");
            
            _logger.LogInformation("Retrieved {Count} products from API", products?.Count ?? 0);
            
            // Store in cache for 5 minutes (per requirement to check stock every 5 min)
            if (products != null)
            {
                _cache.Set(cacheKey, products, TimeSpan.FromMinutes(5));
            }
            
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

    /// <summary>
    /// Gets a single product by ID
    /// </summary>
    public async Task<ProductViewModel?> GetProductByIdAsync(int id)
    {
        // Create cache key for this product
        string cacheKey = $"product_{id}";
        
        // Check if in cache
        if (_cache.TryGetValue(cacheKey, out ProductViewModel? cachedProduct))
        {
            _logger.LogInformation("Retrieved product {Id} from cache", id);
            return cachedProduct;
        }
        
        try
        {
            var product = await _httpClient.GetFromJsonAsync<ProductViewModel>($"api/Product/{id}");
            
            // Store in cache for 5 minutes
            if (product != null)
            {
                _cache.Set(cacheKey, product, TimeSpan.FromMinutes(5));
            }
            
            return product;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Product with ID {Id} not found", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product {Id} from API", id);
            return null;
        }
    }

    /// <summary>
    /// Gets all categories
    /// </summary>
    public async Task<IEnumerable<CategoryViewModel>> GetCategoriesAsync()
    {
        // Categories change less frequently, so we can cache them longer
        const string cacheKey = "all_categories";
        
        // Check if in cache
        if (_cache.TryGetValue(cacheKey, out IEnumerable<CategoryViewModel>? cachedCategories))
        {
            _logger.LogInformation("Retrieved categories from cache");
            return cachedCategories!;
        }
        
        try
        {
            var categories = await _httpClient.GetFromJsonAsync<List<CategoryViewModel>>("api/Category");
            
            // Cache for longer (15 minutes) since categories don't change as often
            if (categories != null)
            {
                _cache.Set(cacheKey, categories, TimeSpan.FromMinutes(15));
            }
            
            return categories ?? new List<CategoryViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching categories from API");
            return Array.Empty<CategoryViewModel>();
        }
    }

    /// <summary>
    /// Gets a single category by ID
    /// </summary>
    public async Task<CategoryViewModel?> GetCategoryByIdAsync(int id)
    {
        // Create cache key for this category
        string cacheKey = $"category_{id}";
        
        // Check if in cache
        if (_cache.TryGetValue(cacheKey, out CategoryViewModel? cachedCategory))
        {
            _logger.LogInformation("Retrieved category {Id} from cache", id);
            return cachedCategory;
        }
        
        try
        {
            var category = await _httpClient.GetFromJsonAsync<CategoryViewModel>($"api/Category/{id}");
            
            // Cache for longer (15 minutes) since categories don't change as often
            if (category != null)
            {
                _cache.Set(cacheKey, category, TimeSpan.FromMinutes(15));
            }
            
            return category;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Category with ID {Id} not found", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching category {Id} from API", id);
            return null;
        }
    }
}