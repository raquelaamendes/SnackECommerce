using ApiECommerce.Entities;
using ApiECommerce.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ApiECommerce.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepository;

    public ProductsController(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts(string typeProduct, int? categoryId = null)
    {
        IEnumerable<Product> products;

        if (typeProduct == "category" && categoryId != null)
        {
            products = await _productRepository.GetProductsByCategoryAsync(categoryId.Value);
        }
        else if (typeProduct == "popular")
        {
            products = await _productRepository.GetPopularProductsAsync();
        }
        else if (typeProduct == "bestseller")
        {
            products = await _productRepository.GetBestSellerProductsAsync();
        }
        else
        {
            return BadRequest("Invalid product type");
        }

        var productDetails = products.Select(v => new
        {
            Id = v.Id,
            Name = v.Name,
            Price = v.Price,
            UrlImage = v.UrlImage
        });

        return Ok(productDetails);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProductDetails(int id)
    {
        var product = await _productRepository.GetProductDetailsAsync(id);

        if (product is null)
        {
            return NotFound($"Produto com id={id} não encontrado");
        }

        var dadosProduto = new
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Details = product.Details,
            UrlImage = product.UrlImage
        };

        return Ok(dadosProduto);
    }
}
