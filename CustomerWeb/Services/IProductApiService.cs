using CustomerWeb.Models;

namespace CustomerWeb.Services;

public interface IProductApiService
{
    /// <summary>
    /// Gets products with optional filtering and search
    /// </summary>
    Task<IEnumerable<ProductViewModel>> GetProductsAsync(
        string? searchTerm = null,
        int? categoryId = null,
        int? brandId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null);

    /// <summary>
    /// Gets a single product by ID
    /// </summary>
    Task<ProductViewModel?> GetProductByIdAsync(int id);

    /// <summary>
    /// Gets all categories
    /// </summary>
    Task<IEnumerable<CategoryViewModel>> GetCategoriesAsync();

    /// <summary>
    /// Gets a single category by ID
    /// </summary>
    Task<CategoryViewModel?> GetCategoryByIdAsync(int id);

    /// <summary>
    /// Refreshes stock information for all products
    /// </summary>
    Task RefreshStockInformationAsync();

    /// <summary>
    /// Manually triggers the API's product sync process
    /// </summary>
    Task TriggerProductSyncAsync();

    /// <summary>
    /// Gets the timestamp of the last stock update
    /// </summary>
    DateTime GetLastUpdateTime();
}