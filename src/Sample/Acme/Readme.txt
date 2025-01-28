** Ensure LocalDB is available

Before running the unittests, LocalDB must be installed (part of Visual Studio or SQL Server Express) and running.
To verify this, use:
  sqllocaldb info


** Publish AcmeDB to the LocalDB instance

Either by double-clicking the AcmeDB.UnitTest.publish.xml file from Visual Studio and selecting Publish,
or using the commandline (note that SqlPackage.exe is usually not part of the %PATH% environment variable):
  sqlpackage /Action:Publish /SourceFile:bin\Debug\AcmeDB.dacpac /Profile:AcmeDB.UnitTest.publish.xml

See https://learn.microsoft.com/en-us/sql/tools/sqlpackage/sqlpackage-publish?view=sql-server-ver16


** Regenerate AcmeDbContext class

To regenerate the AcmeDbContext and the Product class, use:
  dotnet ef dbcontext scaffold "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=AcmeDB" Microsoft.EntityFrameworkCore.SqlServer --no-onconfiguring --force

See https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/?tabs=dotnet-core-cli
	https://learn.microsoft.com/en-us/ef/core/cli/dotnet