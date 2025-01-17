using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace DatabaseTestSetManager
{


	public class SqlScriptPart
	{
		public string Filename { get; private set; }

		public int PartNr { get; private set; }

		public string Contents { get; private set; }

		public SqlScriptPart(string filename, int partNr, string contents)
		{
			Filename = filename;
			PartNr = partNr;
			Contents = contents;
		}
	}

	public class SqlScriptTestSet: TestSet
	{
		public List<SqlScriptPart> Parts { get; private set; } = new List<SqlScriptPart>();

		public SqlScriptTestSet() 
		{ 
		}

		public void AddSqlScript(string filename, string contents)
		{
			int partNr = 1;
			Parts.AddRange(SplitSqlScriptOnGo(contents)
				.Select(partContents => new SqlScriptPart(filename, partNr++, partContents)));
		}

		/// <summary>
		/// Splits a single sql script into batches by splitting the script on "GO". The "GO" keyword is not valid SQL, 
		/// but rather something supported by SQL Server Management Studio.
		/// </summary>
		public static List<string> SplitSqlScriptOnGo(string sqlScript)
		{
			//The part before and after the "GO" is a non-capturing group of at least 1 whitespace or (CR)LF.
			return Regex.Split(sqlScript, @"(?:\s|\r?\n)+GO(?:\s|\r?\n)+", RegexOptions.IgnoreCase)
				.Where(sql => string.IsNullOrWhiteSpace(sql) == false)
				.ToList();
		}

		public override void Apply(SqlConnection sqlConnection)
		{
			foreach (SqlScriptPart scriptPart in Parts)
			{
				SqlCommand sqlCmd = new SqlCommand(scriptPart.Contents, sqlConnection);
				sqlCmd.ExecuteScalar();
			}
		}
	}
}
