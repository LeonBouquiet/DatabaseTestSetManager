using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Acme;

public class ProductRepository(AcmeDbContext dbContext)
{
	private readonly AcmeDbContext _dbContext = dbContext;

	/// <summary>
	/// Returns all Products.
	/// </summary>
	public Task<List<Product>> GetAllProducts()
	{
		return _dbContext.Products
			.ToListAsync();
	}

	/// <summary>
	/// Inserts the given <paramref name="product"/> into the database.
	/// </summary>
	public Task<int> AddProduct(Product product)
	{
		_dbContext.Products.Add(product);
		return _dbContext.SaveChangesAsync();
	}
}
