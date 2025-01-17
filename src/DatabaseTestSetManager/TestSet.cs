using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace DatabaseTestSetManager
{
	/// <summary>
	/// Defines a set of test data with which a database can be initialized so that it is in a known state during 
	/// unittesting.
	/// </summary>
	/// <remarks>Needs to be an abstract class rather than an interface, because the implicit conversion from a builder 
	/// to the TestSet it Builds is not allowed for interface types.</remarks>
	public abstract class TestSet
	{
		/// <summary>
		/// Fills the database behind the <paramref name="sqlConnection"/> with the data in this TestSet.
		/// </summary>
		public abstract void Apply(SqlConnection sqlConnection);
	}
}
