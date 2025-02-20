using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

namespace DatabaseTestSetManager
{
	/// <summary>
	/// Stateful part of the unittest database support. Have your test classes (attributed with [TestClass]) derive from 
	/// this to let the test database be initialized prior to each unittest.
	/// </summary>
	public abstract class DatabaseTestBase<TDbContext> : IDisposable where TDbContext: DbContext
	{
		/// <summary>
		/// Gets set by MSTest prior to each test method.
		/// </summary>
		public TestContext TestContext { get; set; } = null!;

		/// <summary>
		/// Gets the TDbContext to use during unittesting; for tests with <see cref="DatabaseCleanUpChanges.ByRollback"/>, 
		/// it is also used to to begin/rollback transactions with.
		/// </summary>
		public TDbContext DbContext { get; private set; } = null!;

		public TestSetManager TestSetManager { get; private set; }

		private static string? PreviousTestSetName { get; set; } = null;

		private static DatabaseCleanUpChanges PreviousCleanUpChanges { get; set; } = DatabaseCleanUpChanges.None;

		/// <summary>
		/// Is used to keep a pointer to the current transaction so we can dispose it later on.
		/// </summary>
		public IDbContextTransaction? EncapsulatingTransaction { get; private set; } = null;


		/// <summary>
		/// Implement this property to have it return the connection string to the test database.
		/// </summary>
		public abstract string ConnectionString { get; }

		/// <summary>
		/// Constructor.
		/// </summary>
		public DatabaseTestBase()
		{
			TestSetManager = new TestSetManager(ConnectionString);
			OnDefineTestSets();
		}

		/// <summary>
		/// Creates and returns a TDbContext based on the <see cref="ConnectionString"/> to use during unittesting; this 
		/// TDbContext is also used to to begin/rollback transactions.
		/// Override this method if you want control over how the TDbContext is instantiated.
		/// </summary>
		protected virtual TDbContext CreateDbContext()
		{
			string connectionString = this.ConnectionString;

			DbContextOptionsBuilder<TDbContext> optionsBuilder = new DbContextOptionsBuilder<TDbContext>();
			optionsBuilder.UseSqlServer(connectionString);

			return CreateDbContextFromOptions(optionsBuilder.Options);
		}

		private TDbContext CreateDbContextFromOptions(DbContextOptions<TDbContext> options)
		{
			ConstructorInfo? optionsConstructor = typeof(TDbContext).GetConstructor(new[] { typeof(DbContextOptions<TDbContext>) });
			if (optionsConstructor == null)
				throw new Exception($"Can't dynamically instantiate an {typeof(TDbContext).FullName}; it is missing a single-argument constructor that takes a DbContextOptions<{typeof(TDbContext).FullName}>.");

			TDbContext result = (TDbContext)optionsConstructor.Invoke(new[] { options });
			return result;
		}

		/// <summary>
		/// Override this method and have it call <see cref="TestSetManager.DefineTestSet()"> to define one or more 
		/// test sets.
		/// </summary>
		public virtual void OnDefineTestSets()
		{
		}

		/// <summary>
		/// For the given combination of "fullyQualifiedTestClassName.methodName", looks for DatabaseTestSetAttributes 
		/// on the assembly, class and method level, and combines them into a single effective version where lower-level 
		/// values override the higher ones.
		/// </summary>
		public static (string testSetName, DatabaseCleanUpChanges cleanUpChanges) GetEffectiveTestSettings(
			string fullyQualifiedTestClassName, string methodName)
		{
			//Resolve the class name and method name to the Assembly, Type and MethodInfo of the given unittest method.
			Type testClass = GetTypeFromAppDomain(fullyQualifiedTestClassName);
			Assembly testAsm = testClass.Assembly;
			MethodInfo? testMethod = testClass.GetMethod(methodName);       //Can return null or throw an AmbiguousMatchException.
			if (testMethod == null)
				throw new ArgumentException($"Couldn't locate the method \"{methodName}\" on type \"{fullyQualifiedTestClassName}\".");

			//Collect all DatabaseTestSetAttributes on the assembly, class and method level, most detailed first.
			//This uses the fact that GetCustomAttributes() returns the attribute on the most derived class first.
			List<DatabaseTestSetAttribute> inheritanceChain = new List<DatabaseTestSetAttribute>();
			inheritanceChain.AddRange(testMethod.GetCustomAttributes<DatabaseTestSetAttribute>(inherit: true));
			inheritanceChain.AddRange(testClass.GetCustomAttributes<DatabaseTestSetAttribute>(inherit: true));
			inheritanceChain.AddRange(testAsm.GetCustomAttributes<DatabaseTestSetAttribute>());
			inheritanceChain.Add(new DatabaseTestSetAttribute("Default", DatabaseCleanUpChanges.ByRollback));

			//Get the most specific values to use
			string testSetName = inheritanceChain
				.First(attr => attr.TestSetName != null)
				.TestSetName!;
			DatabaseCleanUpChanges cleanUpChanges = inheritanceChain
				.First(attr => attr.CleanUpChanges != DatabaseCleanUpChanges.Inherited)
				.CleanUpChanges;

			return (testSetName, cleanUpChanges);
		}

		/// <summary>
		/// Returns the Type for the given <paramref name="fullyQualifiedClassName"/> by scanning through all Assemblies 
		/// loaded in the AppDomain, or throws an ArgumentException if not found.
		/// </summary>
		/// <param name="fullyQualifiedClassName">A class name with its namespace, e.g. "AcmeCorp.UnitTest.ProductRepositoryTest".</param>
		private static Type GetTypeFromAppDomain(string fullyQualifiedClassName)
		{
			//Based on https://stackoverflow.com/a/11811046

			//See if we can find it in the current assembly or in mscorlib.
			Type? type = Type.GetType(fullyQualifiedClassName, throwOnError: false);
			if (type != null) 
				return type;

			//Search all loaded assemblies
			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				type = asm.GetType(fullyQualifiedClassName, throwOnError: false);
				if (type != null)
					return type;
			}

			throw new ArgumentException($"Couldn't locate the type \"{fullyQualifiedClassName}\" in the current AppDomain.");
		}

		/// <summary>
		/// Initializes the DataContext and starts a transaction in which all database modifications can be safely made.
		/// </summary>
		[TestInitialize]
		public virtual void Initialize()
		{
			DbContext = CreateDbContext();

			//Determine the TestSetName and DatabaseCleanUpChanges to use, as declared by the DatabaseTestSetAttribute(s)
			(string testSetName, DatabaseCleanUpChanges cleanUpChanges) = GetEffectiveTestSettings(TestContext.FullyQualifiedTestClassName!, TestContext.ManagedMethod!);

			//Apply the TestSet if either the current test uses a different TestSet, or if the previous test declared
			//that its changes should be undone by reinitializing.
			if(testSetName != PreviousTestSetName || PreviousCleanUpChanges == DatabaseCleanUpChanges.ByReinitialize)
			{
				TestContext.WriteLine($"Applying TestSet \"{testSetName}\"...");
				TestSetManager.ApplyTestSet(testSetName);
			}

			if (cleanUpChanges == DatabaseCleanUpChanges.ByRollback)
			{
				TestContext.WriteLine($"Beginning EncapsulatingTransaction...");
				EncapsulatingTransaction = DbContext.Database.BeginTransaction();
			}
			else
			{
				EncapsulatingTransaction = null;
			}

			PreviousTestSetName = testSetName;
			PreviousCleanUpChanges = cleanUpChanges;
		}

		/// <summary>
		/// Rolls back the outermost transaction, thereby undoing all database modifications.
		/// </summary>
		[TestCleanup]
		public virtual void Cleanup()
		{		
			if (EncapsulatingTransaction != null)
			{
				TestContext.WriteLine($"Rolling back EncapsulatingTransaction...");

				//Dispose without completing the transactionscope will result in a rollback.
				EncapsulatingTransaction.Dispose();
				EncapsulatingTransaction = null;
			}

			DbContext.Dispose();
		}

		/// <summary>
		/// IDisposable support: same as Cleanup().
		/// </summary>
		public void Dispose()
		{
			Cleanup();
		}

	}
}
