using System;

using Trino.Client.Types;

namespace Trino.Client.Model.StatementV1
{
    /// <summary>
    /// Represents a Presto column definition
    /// </summary>
    public class TrinoColumn
    {
        /// <summary>
        /// The column name
        /// </summary>
        public string name
        {
            get;
            set;
        }

        /// <summary>
        /// The Trino column data type. Call GetColumnType() to get the .NET type
        /// </summary>
        public string type
        {
            get;
            set;
        }

        public Type GetColumnType()
        {
            switch (type)
            {
                case "boolean":
                    return typeof(bool);
                case "tinyint":
                    return typeof(sbyte);
                case "smallint":
                    return typeof(short);
                case "bigint":
                    return typeof(long);
                case "integer":
                    return typeof(int);
                case "double":
                    return typeof(double);
                case "real":
                    return typeof(float);
                case "date":
                case "timestamp":
                    return typeof(DateTime);
                case "timestamp with time zone":
                    return typeof(DateTimeOffset);
                case "uuid":
                    return typeof(Guid);
                case "varbinary":
                    return typeof(byte[]);
                case string t when t.StartsWith("decimal"):
                    return typeof(TrinoBigDecimal);
                case "time":
                case "interval day to second":
                    return typeof(TimeSpan);
                case "interval year to month":
                    return typeof(TrinoIntervalYearToMonth);
                case "varchar":
                case "char":
                case "json":
                case "array":
                case "map":
                case "row":
                case "ipaddress":
                    return typeof(string);
                default:
                    return typeof(string);
            }
        }
    }
}
