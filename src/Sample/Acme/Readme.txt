To regenerate the AcmeDbContext and the Product class, use:

  dotnet ef dbcontext scaffold "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=AcmeDB" Microsoft.EntityFrameworkCore.SqlServer --no-onconfiguring --force

See https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/?tabs=dotnet-core-cli
	https://learn.microsoft.com/en-us/ef/core/cli/dotnet