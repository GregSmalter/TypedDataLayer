using System.Collections.Generic;
using System.IO;
using CommandRunner.DatabaseAbstraction;
using TypedDataLayer.DataAccess;
using TypedDataLayer.Tools;

namespace CommandRunner.CodeGeneration {
	internal static class TableConstantStatics {
		internal static void Generate( DBConnection cn, TextWriter writer, string baseNamespace, IDatabase database, IEnumerable<string> tableNames ) {
			writer.WriteLine( "namespace " + baseNamespace + ".TableConstants {" );
			foreach( var table in tableNames ) {
				CodeGenerationStatics.AddSummaryDocComment( writer, "This object represents the constants of the " + table + " table." );
				writer.WriteLine( "public class " + Utility.GetCSharpIdentifier( table.TableNameToPascal( cn ) + "Table" ) + " {" );

				CodeGenerationStatics.AddSummaryDocComment( writer, "The name of this table." );
				writer.WriteLine( "public const string Name = \"" + table + "\";" );

				foreach( var column in new TableColumns( cn, table, false ).AllColumnsExceptRowVersion ) {
					CodeGenerationStatics.AddSummaryDocComment( writer, "Contains schema information about this column." );
					writer.WriteLine( "public class " + Utility.GetCSharpIdentifier( column.PascalCasedNameExceptForOracle + "Column" ) + " {" );

					CodeGenerationStatics.AddSummaryDocComment( writer, "The name of this column." );
					writer.WriteLine( "public const string Name = \"" + column.Name + "\";" );

					CodeGenerationStatics.AddSummaryDocComment(
						writer,
						"The size of this column. For varchars, this is the length of the biggest string that can be stored in this column." );
					writer.WriteLine( "public const int Size = " + column.Size + ";" );

					writer.WriteLine( "}" );
				}

				writer.WriteLine( "}" );
			}
			writer.WriteLine( "}" );
		}
	}
}