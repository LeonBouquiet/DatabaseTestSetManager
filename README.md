# DatabaseTestSetManager

Painlessly and quickly bring your test database into the correct state before each unit test.

Currently supports a combination of MSTest, SQL Server and Entity Framework Core.

## How do I use it?

1. Add the DatabaseTestSetManager nuget package to your unittest project.
2. Add one or more .sql scripts as embedded resources to your unittest project. These should remove any existing data from your test database and insert the data to be used for your unittests.
3. Create a unittest class and have it derive from DatabaseTestBase<TDbContext>, where `TDbContext` is your EF Core `DbContext`:

```
public class ProductRepositoryTest: DatabaseTestBase<AcmeDbContext>
{
	public override string ConnectionString => "Server=(LocalDB)\\MSSQLLocalDB;Initial Catalog=AcmeUnitTestDB;Integrated security=True;TrustServerCertificate=True";

	public override void OnDefineTestSets()
	{
		TestSetManager.DefineTestSet("Default", setup =>
			 setup.FromAllEmbeddedSqlScripts(inAssemblyThatDefines: this));
	}

	// Test methods go here...
}
```

That's all there is to it to have your unittest database be initialized with the same data before each unittest.

 That is, if you define a unittest like this:

```
	[TestMethod]
	public async Task GetProducts_ReturnsData()
	{
		ProductRepository productRepository = new ProductRepository(this.DbContext);

		//Act
		List<Entities.Product> products = await productRepository.GetProducts();

		//Assert
		Assert.IsTrue(products.Any());
	}
```
The DatabaseTestSetManager ensures that the sql scripts are run against your unittest database before the first unittest. 

After that, by default, every unittest is run inside a SQL Server transaction that is rolled back at the end. This ensures that no changes done by your code under test are actually persisted to the database, so that the database is back in its initial state, ready for the next unittest.

## Ways to revert database changes

You can control this behaviour by adding a `DatabaseTestSet` attribute to your unittest method (or class or assembly), specifying the name of the TestSet to use and how any changes should be cleaned up. The default values used are:

	[DatabaseTestSet("Default", CleanUpChanges = DatabaseCleanUpChanges.ByRollback)]
	public async Task GetProducts_ReturnsData()
	{
		...

For `DatabaseCleanUpChanges`, three values are supported:
1. `ByRollback` - The unittest is wrapped inside a SQL transaction, and at the end this transaction is rolled back so that any changes are reverted as well. Is very fast and usually the best choice, unless the tested code manages its own SQL transactions - then use ByReinitialize instead.
2. `ByReinitialize` - Executes the SQL scripts again to reinitialize the database. Usually slower than ByRollback but doesn't interfere with any SQL transactions from the code under test.
3. `None` - The database is left as-is; use this only for tests that don't modify the database.

## Frequently Asked Questions

# Can I see a full example of how it should be used?

Sure, check out the [Sample solution on GitHub](https://github.com/LeonBouquiet/DatabaseTestSetManager/tree/main/src/Sample).

# Does it support parallelization?

Sadly not at this moment, but hopefully I can find some time to look into that.
