using System;
using System.Collections.Generic;
using TypedDataLayer.DataAccess.CommandWriting.InlineConditionAbstraction;

namespace TypedDataLayer.DataAccess.CommandWriting.Commands {
	/// <summary>
	/// This class should only be used by autogenerated code.
	/// </summary>
	public class InlineUpdate: InlineDbModificationCommand, InlineDbCommandWithConditions {
		/// <summary>
		/// Use this to get a parameter name from a number that should be unique to the query.
		/// </summary>
		internal static string GetParamNameFromNumber( int number ) {
			return "p" + number;
		}

		private readonly string tableName;
		private readonly List<InlineDbCommandColumnValue> columnModifications = new List<InlineDbCommandColumnValue>();
		private readonly List<InlineDbCommandCondition> conditions = new List<InlineDbCommandCondition>();

		/// <summary>
		/// Creates a modification that will execute an inline UPDATE statement.
		/// </summary>
		public InlineUpdate( string tableName ) {
			this.tableName = tableName;
		}

		/// <summary>
		/// Add a data parameter.
		/// </summary>
		public void AddColumnModification( InlineDbCommandColumnValue columnModification ) {
			columnModifications.Add( columnModification );
		}

		/// <summary>
		/// EWL use only.
		/// </summary>
		public void AddCondition( InlineDbCommandCondition condition ) {
			conditions.Add( condition );
		}

		/// <summary>
		/// Executes this command against the specified database connection and returns the number of rows affected.
		/// </summary>
		public int Execute( DBConnection cn ) {
			if( columnModifications.Count == 0 )
				return 0;
			if( conditions.Count == 0 )
				throw new ApplicationException( "Executing an inline update command with no parameters in the where clause is not allowed." );

			var command = cn.DatabaseInfo.CreateCommand();
			command.CommandText = "UPDATE " + tableName + " SET ";
			var paramNumber = 0;
			foreach( var columnMod in columnModifications ) {
				var parameter = columnMod.GetParameter( name: GetParamNameFromNumber( paramNumber++ ) );
				command.CommandText += columnMod.ColumnName + " = " + parameter.GetNameForCommandText( cn.DatabaseInfo ) + ", ";
				command.Parameters.Add( parameter.GetAdoDotNetParameter( cn.DatabaseInfo ) );
			}
			command.CommandText = command.CommandText.Remove( command.CommandText.Length - 2 );
			command.CommandText += " WHERE ";
			foreach( var condition in conditions ) {
				condition.AddToCommand( command, cn.DatabaseInfo, GetParamNameFromNumber( paramNumber++ ) );
				command.CommandText += " AND ";
			}
			command.CommandText = command.CommandText.Remove( command.CommandText.Length - 5 );
			return cn.ExecuteNonQueryCommand( command );
		}
	}
}