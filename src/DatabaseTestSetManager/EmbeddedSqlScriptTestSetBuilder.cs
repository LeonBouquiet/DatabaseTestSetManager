using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace DatabaseTestSetManager
{
	/// <summary>
	/// Defines the available methods that can be used to setup a <see cref="SqlScriptTestSet"/>.
	/// See the <see cref="EmbeddedSqlScriptTestSetBuilder"/> for its usage.
	/// </summary>
	public static class SqlScriptTestSetBuilderExtensions
	{
		/// <summary>
		/// Allows building a <see cref="SqlScriptTestSet"/> from embedded resources defined in the assembly that also 
		/// defines the type of the given object. The result still needs to be specified further using one of the 
		/// <see cref="IUninitializedEmbeddedSqlScriptTestSetBuilder"/> methods before it can be Build().
		/// </summary>
		public static IUninitializedEmbeddedSqlScriptTestSetBuilder FromEmbeddedSqlScripts(this ITestSetBuilder testSetBuilder, object inAssemblyThatDefines)
		{
			return FromEmbeddedSqlScripts(testSetBuilder, inAssemblyThatDefines.GetType().Assembly);
		}

		/// <summary>
		/// Allows building a <see cref="SqlScriptTestSet"/> from embedded resources defined in the given assembly. 
		/// The result still needs to be specified further using one of the 
		/// <see cref="IUninitializedEmbeddedSqlScriptTestSetBuilder"/> methods before it can be Build().
		/// </summary>
		public static IUninitializedEmbeddedSqlScriptTestSetBuilder FromEmbeddedSqlScripts(this ITestSetBuilder testSetBuilder, Assembly containedInAssembly)
		{
			return new EmbeddedSqlScriptTestSetBuilder(containedInAssembly);
		}

		/// <summary>
		/// Allows building a <see cref="SqlScriptTestSet"/> from all embedded resources defined in the assembly that 
		/// also defines the type of the given object.
		/// </summary>
		public static EmbeddedSqlScriptTestSetBuilder FromAllEmbeddedSqlScripts(this ITestSetBuilder testSetBuilder, object inAssemblyThatDefines)
		{
			return FromAllEmbeddedSqlScripts(testSetBuilder, inAssemblyThatDefines.GetType().Assembly);
		}

		/// <summary>
		/// Allows building a <see cref="SqlScriptTestSet"/> from all embedded resources defined in the given assembly. 
		/// </summary>
		public static EmbeddedSqlScriptTestSetBuilder FromAllEmbeddedSqlScripts(this ITestSetBuilder testSetBuilder, Assembly containedInAssembly)
		{
			IUninitializedEmbeddedSqlScriptTestSetBuilder uninitialized = new EmbeddedSqlScriptTestSetBuilder(containedInAssembly);
			return uninitialized.All();
		}

	}

	/// <summary>
	/// Return type used for the <see cref="EmbeddedSqlScriptTestSetBuilder"/> when it still needs to have its embedded 
	/// resources assigned to it.
	/// </summary>
	public interface IUninitializedEmbeddedSqlScriptTestSetBuilder
	{
		/// <summary>
		/// Uses only the embedded resource names that match the given predicate. The embedded resources are sorted 
		/// and used alphabetically on name.
		/// </summary>
		public EmbeddedSqlScriptTestSetBuilder WithNamesMatching(Predicate<string> embeddedResourceName);

		/// <summary>
		/// The caller is passed all available embedded resource names and should select ones to be used, and in which 
		/// ordering.
		/// </summary>
		public EmbeddedSqlScriptTestSetBuilder Use(Func<IEnumerable<string>, IEnumerable<string>> embeddedResourceNames);

		/// <summary>
		/// All embedded resources are sorted and used alphabetically on name.
		/// </summary>
		public EmbeddedSqlScriptTestSetBuilder All();
	}

	/// <summary>
	/// Builder that creates <see cref="SqlScriptTestSet"/>s from embedded resources. Is used from 
	/// <see cref="TestSetManager.DefineTestSet"/> like this:
	/// <code>
	/// 	TestSetManager.DefineTestSet("TestSet1", setup =>
	/// 		setup.FromEmbeddedSqlScripts(inAssemblyThatDefines: this)
	/// 			 .WithNamesMatching(name => name.Contains(".UnitTest.TestSet1")));
	/// </code>
	/// </summary>
	public class EmbeddedSqlScriptTestSetBuilder :
		IUninitializedEmbeddedSqlScriptTestSetBuilder
	{

		private Assembly _assembly;

		private List<string>? _resourceNames;

		public EmbeddedSqlScriptTestSetBuilder(Assembly assembly)
		{
			_assembly = assembly;
		}

		/// <summary>
		/// Uses only the embedded resource names that match the given predicate. The embedded resources are sorted 
		/// and used alphabetically on name.
		EmbeddedSqlScriptTestSetBuilder IUninitializedEmbeddedSqlScriptTestSetBuilder.WithNamesMatching(Predicate<string> predicate)
		{
			_resourceNames = _assembly.GetManifestResourceNames()
				.Where(name => predicate(name))
				.ToList();

			return this;
		}

		/// <summary>
		/// The caller is passed all available embedded resource names and should select ones to be used, and in which 
		/// ordering.
		/// </summary>
		EmbeddedSqlScriptTestSetBuilder IUninitializedEmbeddedSqlScriptTestSetBuilder.Use(Func<IEnumerable<string>, IEnumerable<string>> selectResourceNames)
		{
			_resourceNames = selectResourceNames(_assembly.GetManifestResourceNames())
				.ToList();

			return this;
		}

		/// <summary>
		/// All embedded resources are sorted and used alphabetically on name.
		/// </summary>
		EmbeddedSqlScriptTestSetBuilder IUninitializedEmbeddedSqlScriptTestSetBuilder.All()
		{
			_resourceNames = _assembly.GetManifestResourceNames()
				.ToList();

			return this;
		}

		/// <summary>
		/// Builds the SqlScriptTestSet according to the specified criteria. Because this builder is implicitly 
		/// convertible to SqlScriptTestSet, the call to Build() can usually be omitted. 
		/// </summary>
		public SqlScriptTestSet Build()
		{
			if (_resourceNames == null)
				throw new InvalidOperationException("No resources have been added, call one of the IUninitializedEmbeddedResourceTestSetBuilder methods first.");

			SqlScriptTestSet result = new SqlScriptTestSet();
			foreach(string resourceName in _resourceNames)
			{
				string sqlScript = ReadEmbeddedResource(resourceName);
				result.AddSqlScript(resourceName, sqlScript);
			}

			return result;
		}

		private string ReadEmbeddedResource(string resourceName)
		{
			Stream? stream = _assembly.GetManifestResourceStream(resourceName);
			if (stream == null)
				throw new ArgumentException($"No embedded resource found named \"{resourceName}\".");

			using (StreamReader sr = new StreamReader(stream))
			{
				string sqlScript = sr.ReadToEnd();
				return sqlScript;
			}
		}

		public static implicit operator SqlScriptTestSet(EmbeddedSqlScriptTestSetBuilder builder)
		{
			return builder.Build();
		}
	}
}
