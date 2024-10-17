using ApiECommerce.Context;
using ApiECommerce.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiECommerce.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext dbContext;

    public CategoryRepository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IEnumerable<Category>> GetCategories()
    {
        return await dbContext.Categories.AsNoTracking().ToListAsync();
    }
}
