using DatabaseTestSetManager;
using Microsoft.EntityFrameworkCore;

namespace Acme.UnitTest;

[TestClass]
public class ProductRepositoryTest: DatabaseTestBase<AcmeDbContext>
{
	public override string ConnectionString => "Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog=AcmeDB;Integrated Security=True;Trust Server Certificate=True";

	/// <summary>
	/// Defines the Default test set.
	/// </summary>
	public override void OnDefineTestSets()
	{
		TestSetManager.DefineTestSet("Default", setup =>
			 setup.FromAllEmbeddedSqlScripts(inAssemblyThatDefines: this));
	}

	private ProductRepository CreateProductRepository() => new ProductRepository(base.DbContext);

	/// <summary>
	/// GetAllProducts() should return all Products in the database.
	/// </summary>
	[TestMethod]
	public async Task GetAllProducts_ReturnsData()
	{
		//Act
		ProductRepository repository = CreateProductRepository();
		List<Product> products = await repository.GetAllProducts();

		//Assert: We expect to get some products, at least including the Hen Grenade.
		Assert.IsNotNull(products);
		Assert.IsNotNull(products.FirstOrDefault(prd => prd.Code == "HG"));
	}

	/// <summary>
	/// AddProduct() should add a Product to the database.
	/// </summary>
	[TestMethod]
	public async Task AddProduct_InsertsData()
	{
		//Arrange
		Product tornadoSeeds = new Product() { Code = "TOR", Name = "Tornado Seeds" };

		//Act: Insert this new Product
		ProductRepository repository = CreateProductRepository();
		await repository.AddProduct(tornadoSeeds);

		//Assert: When reading back the products, TOR should be present
		List<Product> products = await repository.GetAllProducts();
		Assert.IsNotNull(products.FirstOrDefault(prd => prd.Code == "TOR"));
	}

	/// <summary>
	/// AddProduct should fail if the Product.Code is already in use.
	/// </summary>
	[TestMethod, ExpectedException(typeof(DbUpdateException))]
	public async Task AddProduct_BreaksOnDuplicateCode()
	{
		//Arrange: Prepare a Product with a Code that already exists (BS = Bird Seed).
		Product bedSprings = new Product() { Code = "BS", Name = "Bed Springs" };

		//Act: Adding it should fail due to the UQ_ProductCode constraint.
		ProductRepository repository = CreateProductRepository();
		await repository.AddProduct(bedSprings);
	}
}
