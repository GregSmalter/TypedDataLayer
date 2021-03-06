﻿using System;
using System.Collections.Generic;
using System.Linq;
using CommandRunner.Exceptions;
using TypedDataLayer.DataAccess;
using TypedDataLayer.DatabaseSpecification.Databases;

namespace CommandRunner.CodeGeneration {
	internal class TableColumns {
		internal readonly IEnumerable<Column> AllColumns;

		private readonly List<Column> keyColumns = new List<Column>();

		/// <summary>
		/// Returns either all components of the primary key, or the identity (alone).
		/// </summary>
		internal IEnumerable<Column> KeyColumns => keyColumns;

		private readonly Column identityColumn;
		internal Column IdentityColumn => identityColumn;

		internal readonly Column RowVersionColumn;
		internal readonly IEnumerable<Column> AllColumnsExceptRowVersion;
		internal readonly IEnumerable<Column> AllNonIdentityColumnsExceptRowVersion;

		private readonly Column primaryKeyAndRevisionIdColumn;
		internal Column PrimaryKeyAndRevisionIdColumn => primaryKeyAndRevisionIdColumn;

		private readonly IEnumerable<Column> dataColumns;

		/// <summary>
		/// Gets all columns that are not the identity column, the row version column, or the primary key and revision ID column.
		/// </summary>
		internal IEnumerable<Column> DataColumns => dataColumns;

		internal TableColumns( DBConnection cn, string table, bool forRevisionHistoryLogic ) {
			try {
				// NOTE: Cache this result.
				AllColumns = Column.GetColumnsInQueryResults( cn, "SELECT * FROM " + table, true );

				foreach( var col in AllColumns ) {
					// This hack allows code to be generated against a database that is configured for ASP.NET Application Services.
					var isAspNetApplicationServicesTable = table.StartsWith( "aspnet_" );

					if( !( cn.DatabaseInfo is OracleInfo ) && col.DataTypeName == typeof( string ).ToString() && col.AllowsNull && !isAspNetApplicationServicesTable )
						throw new UserCorrectableException( $"String column {col.Name} allows null, which is not allowed." );
				}

				// Identify key, identity, and non identity columns.
				var nonIdentityColumns = new List<Column>();
				foreach( var col in AllColumns ) {
					if( col.IsKey )
						keyColumns.Add( col );
					if( col.IsIdentity ) {
						if( identityColumn != null )
							throw new ApplicationException( "Only one identity column per table is supported." );
						identityColumn = col;
					}
					else {
						nonIdentityColumns.Add( col );
					}
				}
				if( !keyColumns.Any() )
					throw new ApplicationException( "The table must contain a primary key or other means of uniquely identifying a row." );

				// If the table has a composite key, try to use the identity as the key instead since this will enable InsertRow to return a value.
				if( identityColumn != null && keyColumns.Count > 1 ) {
					keyColumns.Clear();
					keyColumns.Add( identityColumn );
				}

				RowVersionColumn = AllColumns.SingleOrDefault( i => i.IsRowVersion );
				AllColumnsExceptRowVersion = AllColumns.Where( i => !i.IsRowVersion ).ToArray();
				AllNonIdentityColumnsExceptRowVersion = nonIdentityColumns.Where( i => !i.IsRowVersion ).ToArray();

				if( forRevisionHistoryLogic ) {
					if( keyColumns.Count != 1 )
						throw new ApplicationException(
							"A revision history modification class can only be created for tables with exactly one primary key column, which is assumed to also be a foreign key to the revisions table." );
					primaryKeyAndRevisionIdColumn = keyColumns.Single();
					if( primaryKeyAndRevisionIdColumn.IsIdentity )
						throw new ApplicationException( "The revision ID column of a revision history table must not be an identity." );
				}

				dataColumns = AllColumns.Where( col => !col.IsIdentity && !col.IsRowVersion && col != primaryKeyAndRevisionIdColumn ).ToArray();
			}
			catch( UserCorrectableException e ) {
				throw new UserCorrectableException( $"There was a problem getting columns for table {table}.", e );
			}
			catch( Exception e ) {
				throw new ApplicationException( $"An exception occurred while getting columns for table {table}.", e );
			}
		}
	}
}