using ApiECommerce.Context;
using ApiECommerce.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiECommerce.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext dbContext;

    public OrdersController(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    // GET: api/Orders/OrderDetails/5
    // Retorna os detalhes de um pedido específico, incluindo informações sobre
    // os produtos associados a esse pedido.
    [HttpGet("[action]/{orderId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> OrderDetails(int orderId)
    {
        var orderDetails = await (from orderDetail in dbContext.OrderDetails
                                  join order in dbContext.Orders on orderDetail.OrderId equals order.Id
                                  join product in dbContext.Products on orderDetail.ProductId equals product.Id
                                  where orderDetail.OrderId == orderId
                                  select new
                                  {
                                      Id = orderDetail.Id,
                                      Quantity = orderDetail.Quantity,
                                      SubTotal = orderDetail.Total,
                                      ProductName = product.Name,
                                      ProductImage = product.UrlImage,
                                      ProductPrice = product.Price
                                  }).ToListAsync();

        if (orderDetails == null || orderDetails.Count == 0)
        {
            return NotFound("Detalhes do pedido não encontrados.");
        }

        return Ok(orderDetails);
    }


    // GET: api/Orders/OrdersPerUser/5
    // Obtêm todos os pedidos de um usuário específico com base no ID do usuário.
    [HttpGet("[action]/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> OrdersPerUser(int userId)
    {
        var orders = await (from order in dbContext.Orders
                            where order.UserId == userId
                            orderby order.OrderDate descending
                            select new
                            {
                                Id = order.Id,
                                OrderTotal = order.Total,
                                OrderDate = order.OrderDate,
                            }).ToListAsync();


        if (orders is null || orders.Count == 0)
        {
            return NotFound("Não foram encontrados pedidos para o usuário especificado.");
        }

        return Ok(orders);
    }

   
    //---------------------------------------------------------------------------
    // Neste codigo a criação do pedido, a adição dos detalhes do pedido
    // e a remoção dos itens do carrinho são agrupados dentro de uma transação única.
    // Se alguma operação falhar, a transação será revertida e nenhuma alteração será
    // persistida no banco de dados. Isso garante a consistência dos dados e evita a
    // possibilidade de criar um pedido sem itens no carrinho ou deixar itens
    // no carrinho após criar o pedido.
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post([FromBody] Order order)
    {
        order.OrderDate = DateTime.Now;

        var itemsShoppingCart = await dbContext.ItemsShoppingCart
            .Where(cart => cart.ClientId == order.UserId)
            .ToListAsync();

        // Verifica se há itens no carrinho
        if (itemsShoppingCart.Count == 0)
        {
            return NotFound("Não há itens no carrinho para criar o pedido.");
        }

        using (var transaction = await dbContext.Database.BeginTransactionAsync())
        {
            try
            {
                dbContext.Orders.Add(order);
                await dbContext.SaveChangesAsync();

                foreach (var item in itemsShoppingCart)
                {
                    var orderDetail = new OrderDetail()
                    {
                        Price = item.UnitPrice,
                        Total = item.Total,
                        Quantity = item.Quantity,
                        ProductId = item.ProductId,
                        OrderId = order.Id,
                    };
                    dbContext.OrderDetails.Add(orderDetail);
                }

                await dbContext.SaveChangesAsync();
                dbContext.ItemsShoppingCart.RemoveRange(itemsShoppingCart);
                await dbContext.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new { OrderId = order.Id });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return BadRequest("Ocorreu um erro ao processar o pedido.");
            }
        }
    }
}
