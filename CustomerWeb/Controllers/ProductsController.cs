using CustomerWeb.Models;
using Microsoft.AspNetCore.Mvc;
using ThAmCo.Web.Services;

namespace ThAmCo.Web.Controllers;

public class ProductsController : Controller
{
    private readonly ProductApiService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        ProductApiService productService,
        ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? searchTerm = null, int? categoryId = null)
    {
        try
        {
            var products = await _productService.GetProductsAsync(searchTerm, categoryId);
            var categories = await _productService.GetCategoriesAsync();

            var viewModel = new ProductsListViewModel
            {
                Products = products,
                SearchTerm = searchTerm,
                CategoryId = categoryId,
                Categories = categories.ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products");
            ViewBag.ErrorMessage = "Could not retrieve products. Please try again later.";
            return View(new ProductsListViewModel());
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var product = await _productService.GetProductByIdAsync(id);
            
            if (product == null)
            {
                return NotFound();
            }
            
            return View(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product details for ID {Id}", id);
            return RedirectToAction(nameof(Index));
        }
    }
}