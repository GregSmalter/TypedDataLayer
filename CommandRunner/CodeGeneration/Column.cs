using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using CommandRunner.DatabaseAbstraction;
using TypedDataLayer.DataAccess;
using TypedDataLayer.DatabaseSpecification;
using TypedDataLayer.DatabaseSpecification.Databases;
using TypedDataLayer.Tools;

namespace CommandRunner.CodeGeneration {
	internal class Column {
		/// <summary>
		/// If includeKeyInfo is true, all key columns for involved tables will be returned even if they were not selected.
		/// </summary>
		internal static List<Column> GetColumnsInQueryResults( DBConnection cn, string commandText, bool includeKeyInfo ) {
			var cmd = DataAccessStatics.GetCommandFromRawQueryText( cn, commandText );
			var columns = new List<Column>();

			var readerMethod = new Action<DbDataReader>(
				r => {
					foreach( DataRow row in r.GetSchemaTable().Rows )
						columns.Add( new Column( row, includeKeyInfo, cn.DatabaseInfo ) );
				} );
			if( includeKeyInfo )
				cn.ExecuteReaderCommandWithKeyInfoBehavior( cmd, readerMethod );
			else
				cn.ExecuteReaderCommandWithSchemaOnlyBehavior( cmd, readerMethod );

			return columns;
		}

		private readonly int ordinal;
		private readonly ValueContainer valueContainer;
		private readonly bool isIdentity;
		private readonly bool isRowVersion;
		private readonly bool? isKey;

		private Column( DataRow schemaTableRow, bool includeKeyInfo, DatabaseInfo databaseInfo ) {
			ordinal = (int)schemaTableRow[ "ColumnOrdinal" ];

			// MySQL incorrectly uses one-based ordinals; see http://bugs.mysql.com/bug.php?id=61477.
			if( databaseInfo is MySqlInfo )
				ordinal -= 1;

			valueContainer = new ValueContainer(
				(string)schemaTableRow[ "ColumnName" ],
				(Type)schemaTableRow[ "DataType" ],
				databaseInfo.GetDbTypeString( schemaTableRow[ "ProviderType" ] ),
				(int)schemaTableRow[ "ColumnSize" ],
				(bool)schemaTableRow[ "AllowDBNull" ],
				databaseInfo );
			isIdentity = databaseInfo is SqlServerInfo && (bool)schemaTableRow[ "IsIdentity" ] || databaseInfo is MySqlInfo && (bool)schemaTableRow[ "IsAutoIncrement" ];
			isRowVersion = databaseInfo is SqlServerInfo && (bool)schemaTableRow[ "IsRowVersion" ];
			if( includeKeyInfo )
				isKey = (bool)schemaTableRow[ "IsKey" ];
		}

		internal string Name => valueContainer.Name;
		internal string PascalCasedName => valueContainer.PascalCasedName;
		internal string PascalCasedNameExceptForOracle => valueContainer.PascalCasedNameExceptForOracle;
		internal string CamelCasedName => valueContainer.CamelCasedName;

		/// <summary>
		/// Gets the name of the data type for this column, or the nullable data type if the column allows null.
		/// </summary>
		internal string DataTypeName => valueContainer.DataTypeName;

		/// <summary>
		/// Gets the name of the nullable data type for this column, regardless of whether the column allows null. The nullable data type is equivalent to the data
		/// type if the latter is a reference type or if the null value is represented with an expression other than "null".
		/// </summary>
		internal string NullableDataTypeName => valueContainer.NullableDataTypeName;

		internal string NullValueExpression => valueContainer.NullValueExpression;
		internal string UnconvertedDataTypeName => valueContainer.UnconvertedDataTypeName;

		internal string GetIncomingValueConversionExpression( string valueExpression ) => valueContainer.GetIncomingValueConversionExpression( valueExpression );

		internal object ConvertIncomingValue( object value ) => valueContainer.ConvertIncomingValue( value );

		internal int Size => valueContainer.Size;
		internal bool AllowsNull => valueContainer.AllowsNull;
		internal bool IsIdentity => isIdentity;
		internal bool IsRowVersion => isRowVersion;
		internal bool IsKey => isKey.Value;

		// NOTE: It would be best to use primary keys here, but unfortunately we don't always have that information.
		//internal bool UseToUniquelyIdentifyRow { get { return !allowsNull && dataType.IsValueType /*We could use IsPrimitive if not for Oracle resolving to System.Decimal.*/; } }
		// Right now we assume that at least one column in table (or query) returns true for UseToUniquelyIdentifyRow. This might not always be the case, for example if you have a query
		// that selects file contents only. If we re-implement this in a way that makes our assumption false, we'll need to modify DataAccessStatics to detect the case where no
		// columns return true for this and provide a useful exception.
		internal bool UseToUniquelyIdentifyRow => !valueContainer.DataType.IsArray && !isRowVersion;

		internal string GetCommandColumnValueExpression( string valueExpression )
			=> "new InlineDbCommandColumnValue( \"{0}\", {1} )".FormatWith( valueContainer.Name, valueContainer.GetParameterValueExpression( valueExpression ) );

		internal string GetDataReaderValueExpression( string readerName, int? ordinalOverride = null ) {
			var getValueExpression = valueContainer.GetIncomingValueConversionExpression( $"{readerName}.GetValue( {ordinalOverride ?? ordinal} )" );
			var o = valueContainer.NullValueExpression.Any() ? valueContainer.NullValueExpression : $"({valueContainer.NullableDataTypeName})null";
			return valueContainer.AllowsNull ? $"{readerName}.IsDBNull( {ordinalOverride ?? ordinal} ) ? {o} : {getValueExpression}" : getValueExpression;
		}
	}
}