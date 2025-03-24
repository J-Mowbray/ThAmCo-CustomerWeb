using System.Net.Http.Json;
using CustomerWeb.Models;
using Microsoft.Extensions.Logging;


namespace ThAmCo.Web.Services;

public class ProductApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductApiService> _logger;

    public ProductApiService(HttpClient httpClient, ILogger<ProductApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
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
            // Build query parameters to match UnderCutters API format
            var queryParams = new List<string>();
            
            if (!string.IsNullOrEmpty(searchTerm))
                queryParams.Add($"category_name={Uri.EscapeDataString(searchTerm)}");
                
            if (categoryId.HasValue)
                queryParams.Add($"category_id={categoryId}");
                
            if (brandId.HasValue)
                queryParams.Add($"brand_id={brandId}");
                
            if (minPrice.HasValue)
                queryParams.Add($"min_price={minPrice}");
                
            if (maxPrice.HasValue)
                queryParams.Add($"max_price={maxPrice}");
                
            var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            
            // Call the API
            var products = await _httpClient.GetFromJsonAsync<List<ProductViewModel>>($"api/Product{queryString}");
            
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

    /// <summary>
    /// Gets a single product by ID
    /// </summary>
    public async Task<ProductViewModel?> GetProductByIdAsync(int id)
    {
        try
        {
            var product = await _httpClient.GetFromJsonAsync<ProductViewModel>($"api/Product/{id}");
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
        try
        {
            var categories = await _httpClient.GetFromJsonAsync<List<CategoryViewModel>>("api/Category");
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
        try
        {
            var category = await _httpClient.GetFromJsonAsync<CategoryViewModel>($"api/Category/{id}");
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