namespace CustomerWeb.Models;

public class ProductsListViewModel
{
    public IEnumerable<ProductViewModel> Products { get; set; } = Array.Empty<ProductViewModel>();
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public List<CategoryViewModel> Categories { get; set; } = new List<CategoryViewModel>();
}