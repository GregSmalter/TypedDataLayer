using System.Data.Common;

namespace TypedDataLayer.DatabaseSpecification {
	/// <summary>
	/// Contains information about a database.
	/// </summary>
	public interface DatabaseInfo {
		/// <summary>
		/// The connection string to connect to this database.
		/// </summary>
		string ConnectionString { get; }

		/// <summary>
		/// Returns the prefix used for parameters in SQL queries (@, :, etc.).
		/// </summary>
		string ParameterPrefix { get; }

		/// <summary>
		/// Returns the empty string if the database does not support auto-increment columns.
		/// </summary>
		string LastAutoIncrementValueExpression { get; }

		/// <summary>
		/// Returns the hint used in SELECT statements to instruct the database to cache the results.
		/// </summary>
		string QueryCacheHint { get; }

		/// <summary>
		/// Creates an ADO.NET database connection to the database.
		/// </summary>
		DbConnection CreateConnection();

		/// <summary>
		/// Creates an ADO.NET command for the database.
		/// </summary>
		DbCommand CreateCommand(int? commandTimeout);

		/// <summary>
		/// Creates an ADO.NET command parameter for the database.
		/// </summary>
		DbParameter CreateParameter();

		/// <summary>
		/// Gets the string that represents the specified database-specific type.
		/// </summary>
		string GetDbTypeString( object databaseSpecificType );

		/// <summary>
		/// Sets the specified parameter's database-specific type to the type represented by the specified string.
		/// </summary>
		void SetParameterType( DbParameter parameter, string dbTypeString );
	}
}