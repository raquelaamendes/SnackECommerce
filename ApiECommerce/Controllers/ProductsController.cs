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
    public async Task<IActionResult> GetProducts(string tipoProduto, int? categoryId = null)
    {
        IEnumerable<Product> products;

        if (tipoProduto == "categoria" && categoryId != null)
        {
            products = await _productRepository.GetProductsByCategoryAsync(categoryId.Value);
        }
        else if (tipoProduto == "popular")
        {
            products = await _productRepository.GetPopularProductsAsync();
        }
        else if (tipoProduto == "maisvendido")
        {
            products = await _productRepository.GetBestSellerProductsAsync();
        }
        else
        {
            return BadRequest("Tipo de produto inválido");
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
            Detalhe = product.Details,
            UrlImage = product.UrlImage
        };

        return Ok(dadosProduto);
    }
}
