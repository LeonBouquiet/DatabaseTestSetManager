using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseTestSetManager
{
	/// <summary>
	/// Defines the ways the database can be restored to its initial state.
	/// </summary>
	public enum DatabaseCleanUpChanges
	{
		/// <summary>Use the value from the higher-level attribute.</summary>
		Inherited = 0,
		/// <summary>The database is left as-is; use this only for tests that don't modify the database.</summary>
		None = 1,
		/// <summary>The unittest is wrapped inside a SQL transaction, and at the end this transaction is rolled back 
		/// so that any changes are reverted as well. Is very fast and usually the best choice, unless the tested code 
		/// manages its own SQL transactions - then use ByReinitialize instead.</summary>
		ByRollback = 2, 
		/// <summary>
		/// Runs the <see cref="TestSet.Apply"/> again to reinitialize the database. Usually slower than ByRollback, 
		/// but doesn't interfere with any SQL transactions from the code under test.
		/// </summary>
		ByReinitialize = 3
	}

	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, 
		Inherited = true, AllowMultiple = true)]    // AllowMultiple is needed when inheriting attributes from base classes.
	public class DatabaseTestSetAttribute: Attribute
	{
		/// <summary>
		/// The TestSetName to use; defaults to "Default" if not specified on both class and method level.
		/// </summary>
		public string? TestSetName { get; set; }

		/// <summary>
		/// How to revert the database changes made by this unittest; defaults to ByRollback if not specified on both 
		/// class and method level.
		/// </summary>
		public DatabaseCleanUpChanges CleanUpChanges { get; set; }

		/// <summary>
		/// Default constructor.
		/// </summary>
		public DatabaseTestSetAttribute(string? testSetName = null, DatabaseCleanUpChanges cleanUpChanges = DatabaseCleanUpChanges.Inherited) 
		{ 
			TestSetName = testSetName;
			CleanUpChanges = cleanUpChanges;
		}
	}
}
