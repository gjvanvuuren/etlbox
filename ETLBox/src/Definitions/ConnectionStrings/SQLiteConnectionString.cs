﻿using System.Data.SQLite;

namespace ALE.ETLBox
{
    /// <summary>
    /// A helper class for encapsulating a conection string in an object.
    /// Internally the SQLiteConnectionStringBuilder is used to access the values of the given connection string.
    /// </summary>
    public class SQLiteConnectionString :
        DbConnectionString<SQLiteConnectionString, SQLiteConnectionStringBuilder>
    {
        public SQLiteConnectionString() :
            base()
        { }
        public SQLiteConnectionString(string value) :
            base(value)
        { }

        public override string DbName
        {
            get => Builder.DataSource;
            set => Builder.DataSource = value;
        }

        public override string MasterDbName => 
            throw new ETLBoxNotSupportedException("N/A for SQLite connection strings!");
        protected override string DbNameKeyword => "Data Source";

        public static implicit operator SQLiteConnectionString(string value) => new SQLiteConnectionString(value);
    }
}
