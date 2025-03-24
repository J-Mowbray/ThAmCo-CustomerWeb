namespace CustomerWeb.Models;

public class ProductViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Ean { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int BrandId { get; set; }
    public string? BrandName { get; set; }
    public decimal Price { get; set; }
    public bool InStock { get; set; }
    public DateTime? ExpectedRestock { get; set; }
}