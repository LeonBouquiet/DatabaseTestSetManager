using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Transactions;

namespace DatabaseTestSetManager
{
	/// <summary>
	/// Interface that is passed by <see cref="TestSetManager.DefineTestSet"/>. Can be used to sticker new "From" 
	/// implementations onto via extension methods, e.g. see <see cref="SqlScriptTestSetBuilderExtensions"/>.
	/// </summary>
	public interface ITestSetBuilder
	{
	}


	public class TestSetManager
	{
		private class DummyBuilder: ITestSetBuilder { }


		private readonly string _connectionString;

		public Dictionary<string, TestSet> TestSets { get; private set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="connectionString">The connection string to the database to use when applying a TestSet.</param>
		public TestSetManager(string connectionString)
		{
			_connectionString = connectionString;
			TestSets = new Dictionary<string, TestSet>(StringComparer.OrdinalIgnoreCase);
		}

		private SqlConnection CreateOpenSqlConnection()
		{
			SqlConnection sqlConn = new SqlConnection(_connectionString);
			sqlConn.Open();

			return sqlConn;
		}

		/// <summary>
		/// Defines and adds a TestSet to use. Use it like this:
		/// <code>
		/// 	DefineTestSet("TestSet1", setup =>
		/// 		setup.FromEmbeddedSqlScripts(inAssemblyThatDefines: this)
		/// 			 .WithNamesMatching(name => name.Contains(".UnitTest.TestSet1")));
		/// </code>
		/// </summary>
		public void DefineTestSet(string name, Func<ITestSetBuilder, TestSet> setup)
		{
			TestSet testSet = setup(new DummyBuilder());
			TestSets[name] = testSet;
		}


		public void ApplyTestSet(string name)
		{
			if(TestSets.TryGetValue(name, out TestSet? testSet))
				ApplyTestSet(testSet);
			else
				throw new ArgumentException($"No TestSet found with name \"{name}\".", nameof(name));
		}

		public void ApplyTestSet(TestSet testSet)
		{
			using (SqlConnection sqlConn = CreateOpenSqlConnection())
			{
				testSet.Apply(sqlConn);
			}
		}

		/// <summary>
		/// Unittest support: Returns the current number of rows in a table.
		/// </summary>
		public int GetRowCountForTable(SqlConnection sqlConnection, string tableName)
		{
			string sql = string.Format("select count(1) from [{0}]", tableName);
			int rowCount = (int)ExecuteSqlCommand(sqlConnection, sql);

			return rowCount;
		}

		/// <summary>
		/// Executes the given sql statement(s) and returns the first column from the first resultset row, if any.
		/// </summary>
		public object ExecuteSqlCommand(SqlConnection sqlConnection, string sql)
		{
			SqlCommand sqlCmd = new SqlCommand(sql, sqlConnection);

			//During some tests the most outer transaction, that is used for rolling back changes made by unittests, 
			//gets the status "Aborted" because the test encounters an exception. During those tests we still want to 
			//query the database (for example to check if changes are rolled back correctly), before disposing the 
			//transactionscope in the cleanup step of the test.
			if (Transaction.Current != null && Transaction.Current.TransactionInformation.Status == TransactionStatus.Aborted)
			{
				//At this point using sqlConn will throw the error "The transaction associated with the current 
				//connection has completed but has not been disposed.  The transaction must be disposed before 
				//the connection can be used to execute SQL statements."
				//To prevent this, take the SqlConnection out of the transaction - DANGER: this means that any
				//modifications after this point cannot be rolled back!
				sqlCmd.Connection.EnlistTransaction(null);
			}

			object result = sqlCmd.ExecuteScalar();
			return result;
		}
	}
}
