using DatabaseTestSetManager;

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

	private ProductRepository CreateProductRepository()
	{
		return new ProductRepository(base.DbContext);
	}

	[TestMethod]
	public async Task GetAllProducts_ReturnsData()
	{
		//Arrange
		ProductRepository repository = CreateProductRepository();

		//Act
		List<Product> products = await repository.GetAllProducts();

		//Assert
		Assert.IsNotNull(products);
		Assert.IsTrue(products.Any());
	}

}
