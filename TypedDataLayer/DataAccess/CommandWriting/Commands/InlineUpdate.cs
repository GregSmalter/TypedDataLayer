using System;
using System.Collections.Generic;
using System.Text;
using TypedDataLayer.DataAccess.CommandWriting.InlineConditionAbstraction;

namespace TypedDataLayer.DataAccess.CommandWriting.Commands {
	/// <summary>
	/// This class should only be used by autogenerated code.
	/// </summary>
	public class InlineUpdate: InlineDbModificationCommand, InlineDbCommandWithConditions {
		/// <summary>
		/// Use this to get a parameter name from a number that should be unique to the query.
		/// </summary>
		internal static string GetParamNameFromNumber( int number ) => "p" + number;

		private readonly string tableName;
		private readonly int? timeout;
		private readonly List<InlineDbCommandColumnValue> columnModifications = new List<InlineDbCommandColumnValue>();
		private readonly List<InlineDbCommandCondition> conditions = new List<InlineDbCommandCondition>();

		/// <summary>
		/// Creates a modification that will execute an inline UPDATE statement.
		/// </summary>
		public InlineUpdate( string tableName, int? timeout ) {
			this.tableName = tableName;
			this.timeout = timeout;
		}

		/// <summary>
		/// Add a data parameter.
		/// </summary>
		public void AddColumnModification( InlineDbCommandColumnValue columnModification ) => columnModifications.Add( columnModification );

		/// <summary>
		/// Use at your own risk.
		/// </summary>
		public void AddCondition( InlineDbCommandCondition condition ) => conditions.Add( condition );

		/// <summary>
		/// Executes this command against the specified database connection and returns the number of rows affected.
		/// </summary>
		public int Execute( DBConnection cn ) {
			if( columnModifications.Count == 0 )
				return 0;
			if( conditions.Count == 0 )
				throw new ApplicationException( "Executing an inline update command with no parameters in the where clause is not allowed." );

			var cmd = cn.DatabaseInfo.CreateCommand();
			var sb = new StringBuilder( "UPDATE " );
			sb.Append( tableName );
			sb.Append( " SET " );
			var paramNumber = 0;
			foreach( var columnMod in columnModifications ) {
				var parameter = columnMod.GetParameter( name: GetParamNameFromNumber( paramNumber++ ) );

				sb.Append( columnMod.ColumnName );
				sb.Append( " = " );
				sb.Append( parameter.GetNameForCommandText( cn.DatabaseInfo ) );
				sb.Append( ", " );
				cmd.Parameters.Add( parameter.GetAdoDotNetParameter( cn.DatabaseInfo ) );
			}
			sb.Remove( sb.Length - 2, 2 );
			sb.Append( " WHERE " );

			foreach( var condition in conditions ) {
				condition.AddToCommand( cmd, sb, cn.DatabaseInfo, GetParamNameFromNumber( paramNumber++ ) );
				sb.Append( " AND " );
			}
			sb.Remove( sb.Length - 5, 5 );

			cmd.CommandText = sb.ToString();
			if( timeout.HasValue )
				cmd.CommandTimeout = timeout.Value;

			return cn.ExecuteNonQueryCommand( cmd );
		}
	}
}