﻿using System;
using System.Linq;
using TypedDataLayer.Tools;

namespace TypedDataLayer.DataAccess.CommandWriting {
	/// <summary>
	/// A column name and a value for use by an inline database command.
	/// </summary>
	public class InlineDbCommandColumnValue: IEquatable<InlineDbCommandColumnValue>, IComparable, IComparable<InlineDbCommandColumnValue> {
		private readonly string columnName;
		private readonly DbParameterValue value;

		/// <summary>
		/// Creates an inline database command column value.
		/// </summary>
		public InlineDbCommandColumnValue( string columnName, DbParameterValue value ) {
			this.columnName = columnName;
			this.value = value;
		}

		internal string ColumnName => columnName;

		internal DbCommandParameter GetParameter( string name = "" ) => new DbCommandParameter( name.Any() ? name : columnName, value );

		public override bool Equals( object obj ) => Equals( obj as InlineDbCommandColumnValue );

		public bool Equals( InlineDbCommandColumnValue other ) => other != null && columnName == other.columnName && Utility.AreEqual( value, other.value );

		public override int GetHashCode() => new { columnName, value }.GetHashCode();

		int IComparable.CompareTo( object obj ) {
			var otherCondition = obj as InlineDbCommandColumnValue;
			if( otherCondition == null && obj != null )
				throw new ArgumentException();
			return CompareTo( otherCondition );
		}

		public int CompareTo( InlineDbCommandColumnValue other ) {
			if( other == null )
				return 1;
			var nameResult = Utility.Compare( columnName, other.columnName, comparer: StringComparer.InvariantCulture );
			return nameResult != 0 ? nameResult : Utility.Compare( value, other.value );
		}
	}
}