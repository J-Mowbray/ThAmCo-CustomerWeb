// Services/IProductApiService.cs
using CustomerWeb.Models;

namespace CustomerWeb.Services;

public interface IProductApiService
{
    Task<IEnumerable<ProductViewModel>> GetProductsAsync(string? searchTerm = null, int? categoryId = null, int? brandId = null, decimal? minPrice = null, decimal? maxPrice = null);
    Task<ProductViewModel?> GetProductByIdAsync(int id);
    Task<IEnumerable<CategoryViewModel>> GetCategoriesAsync();
    Task<CategoryViewModel?> GetCategoryByIdAsync(int id);
}