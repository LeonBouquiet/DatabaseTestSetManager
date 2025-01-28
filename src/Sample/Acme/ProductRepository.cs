using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Acme;

/// <summary>
/// Handles querying and inserting <see cref="Product"/>s.
/// </summary>
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
	public Task AddProduct(Product product)
	{
		_dbContext.Products.Add(product);
		return _dbContext.SaveChangesAsync();
	}

	/// <summary>
	/// Inserts all <paramref name="products"/> in the database, as a single transaction.
	/// </summary>
	public async Task AddBatch(IEnumerable<Product> products)
	{
		//This is a bit contrived since EF6+ already wraps every SaveChanges() call inside a transaction so this isn't
		//actually needed, but it does demonstrate the fact that EF Core doesn't allow nested transactions and that 
		//DatabaseCleanUpChanges.ByReinitialize can be used to work around this.
		using (var tx = _dbContext.Database.BeginTransaction())
		{
			foreach (var product in products)
			{
				_dbContext.Products.Add(product);
			}

			await _dbContext.SaveChangesAsync();
			await tx.CommitAsync();
		}
	}

}
