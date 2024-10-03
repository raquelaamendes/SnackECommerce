using ApiECommerce.Context;
using ApiECommerce.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ApiECommerce.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ItemsShoppingCartController : ControllerBase
{
    private readonly AppDbContext dbContext;

    public ItemsShoppingCartController(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    // GET: api/ItemsShoppingCart/1
    [HttpGet("{userId}")]
    public async Task<IActionResult> Get(int userId)
    {
        var user = await dbContext.Users.FindAsync(userId);

        if (user is null)
        {
            return NotFound($"Utilizador com o id = {userId} não encontrado");
        }

        var itemsShoppingCart = await (from s in dbContext.ItemsShoppingCart.Where(s => s.ClientId == userId)
                                   join p in dbContext.Products on s.ProductId equals p.Id
                                   select new
                                   {
                                       Id = s.Id,
                                       Price = s.UnitPrice,
                                       Total = s.Total,
                                       Quantity = s.Quantity,
                                       ProductId = p.Id,
                                       ProductName = p.Name,
                                       UrlImage = p.UrlImage
                                   }).ToListAsync();

        return Ok(itemsShoppingCart);
    }

    // POST: api/ItemsShoppingCart
    // Este método Action trata de uma requisição HTTP do tipo POST para adicionar um
    // novo item ao carrinho de compra ou atualizar a quantidade de um item existente
    // no carrinho. Ele verifica se o item já está no carrinho com base no ID do produto
    // e no ID do cliente. Se o item já estiver no carrinho, sua quantidade é atualizada
    // e o valor total é recalculado. Caso contrário, um novo item é adicionado ao carrinho
    // com as informações fornecidas. Após as operações no banco de dados, o método retorna
    // um código de status 201 (Created).
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ItemShoppingCart itemShoppingCart)
    {
        try
        {
            var shoppingCart = await dbContext.ItemsShoppingCart.FirstOrDefaultAsync(s =>
                                    s.ProductId == itemShoppingCart.ProductId &&
                                    s.ClientId == itemShoppingCart.ClientId);

            if (shoppingCart is not null)
            {
                shoppingCart.Quantity += itemShoppingCart.Quantity;
                shoppingCart.Total = shoppingCart.UnitPrice * shoppingCart.Quantity;
            }
            else
            {
                var product = await dbContext.Products.FindAsync(itemShoppingCart.ProductId);

                var cart = new ItemShoppingCart()
                {
                    ClientId = itemShoppingCart.ClientId,
                    ProductId = itemShoppingCart.ProductId,
                    UnitPrice = itemShoppingCart.UnitPrice,
                    Quantity = itemShoppingCart.Quantity,
                    Total = (product!.Price) * (itemShoppingCart.Quantity)
                };
                dbContext.ItemsShoppingCart.Add(cart);
            }
            await dbContext.SaveChangesAsync();
            return StatusCode(StatusCodes.Status201Created);
        }
        catch (Exception)
        {
            // Aqui você pode lidar com a exceção, seja registrando-a, enviando uma resposta de erro adequada para o cliente, etc.
            // Por exemplo, você pode retornar uma resposta de erro 500 (Internal Server Error) com uma mensagem genérica para o cliente.
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "Ocorreu um erro ao processar a solicitação.");
        }
    }

    // PUT /api/ItensshoppingCart?ProductId = 1 & action = "aumentar"
    // PUT /api/ItensshoppingCart?ProductId = 1 & action = "diminuir"
    // PUT /api/ItensshoppingCart?ProductId = 1 & action = "deletar"
    //--------------------------------------------------------------------
    // Este código manipula itens no carrinho de compras de um usuário com base em uma
    // ação("aumentar", "diminuir" ou "deletar") e um ID de produto.
    // Obtém o usuário logado:
    //    Usa o e-mail do usuário logado para buscar o usuário no banco de dados.
    // Busca o item do carrinho do produto:
    // Procura o item no carrinho com base no ID do produto e no ID do cliente (usuário logado).
    // Realiza a ação especificada:
    //    Aumentar:
    //        Se a quantidade for maior que 0, aumenta a quantidade do item em 1.
    //    Diminuir:
    //        Se a quantidade for maior que 1, diminui a quantidade do item em 1.
    //        Se a quantidade for 1, remove o item do carrinho.
    //    Deletar:
    //        Remove o item do carrinho.
    // Atualiza o valor total do item:
    //    Multiplica o preço unitário pela quantidade, atualizando o valor total do item no carrinho.
    // Salva as alterações no banco de dados:
    //    Salva as alterações feitas no item do carrinho no banco de dados.
    // Retorna o resultado:
    //    Se a ação for bem-sucedida, retorna "Ok".
    //    Se o item não for encontrado, retorna "NotFound".
    //    Se a ação for inválida, retorna "BadRequest".
    /// <summary>
    /// Atualiza a quantidade de um item no carrinho de compras do usuário.
    /// </summary>
    /// <param name="ProductId">O ID do produto.</param>
    /// <param name="action">A ação a ser realizada no item do carrinho. Opções: 'aumentar', 'diminuir' ou 'deletar'.</param>
    /// <returns>Um objeto IActionResult representando o resultado da operação.</returns>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)] 
    //[HttpPut("{productId}/{action}")]
    public async Task<IActionResult> Put(int productId, string action)
    {
        // Este codigo recupera o endereço de e-mail do usuário autenticado do token JWT decodificado,
        // Claims representa as declarações associadas ao usuário autenticado
        // Assim somente usuários autenticados poderão acessar este endpoint
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

        if (user is null)
        {
            return NotFound("Utilizador não encontrado."); 
        }

        var itemShoppingCart = await dbContext.ItemsShoppingCart.FirstOrDefaultAsync(s =>
                                               s.ClientId == user!.Id && s.ProductId == productId);

        if (itemShoppingCart != null)
        {
            if (action.ToLower() == "aumentar")
            {
                itemShoppingCart.Quantity += 1;
            }
            else if (action.ToLower() == "diminuir")
            {
                if (itemShoppingCart.Quantity > 1)
                {
                    itemShoppingCart.Quantity -= 1;
                }
                else
                {
                    dbContext.ItemsShoppingCart.Remove(itemShoppingCart);
                    await dbContext.SaveChangesAsync();
                    return Ok();
                }
            }
            else if (action.ToLower() == "deletar")
            {
                // Remove o item do carrinho
                dbContext.ItemsShoppingCart.Remove(itemShoppingCart);
                await dbContext.SaveChangesAsync();
                return Ok();
            }
            else
            {
                return BadRequest("Ação Inválida. Use : 'aumentar', 'diminuir', ou 'deletar' para realizar uma ação");
            }

            itemShoppingCart.Total = itemShoppingCart.UnitPrice * itemShoppingCart.Quantity;
            await dbContext.SaveChangesAsync();
            return Ok($"Operacao : {action} realizada com sucesso");
        }
        else
        {
            return NotFound("Nenhum item encontrado no carrinho");
        }
    }
}