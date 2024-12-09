using Trino.Client;
using Trino.Client.Model.StatementV1;
using Trino.Client.Types;
using Trino.Client.Utils;

using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Trino.Data.ADO.Utilities;

namespace Trino.Data.ADO.Client
{
    /// <summary>
    /// IDataReader implementation for Trino.
    /// Trino client ignores empty pages and returns the next page with data. An empty page signifies it is the last page.
    /// </summary>
    public class TrinoDataReader : DbDataReader
    {
        public override int Depth => 1;
        public override bool IsClosed { get { return isClosed; } }
        private bool isClosed = false;
        private readonly Records records;

        public override int RecordsAffected
        {
            get
            {
                if (records.Stats == null)
                {
                    return 0;
                }
                return records.Stats.processedRows > int.MaxValue ? int.MaxValue : (int)records.Stats.processedRows;
            }
        }

        //
        // Summary:
        //     Gets the number of columns in the current row.
        //
        // Returns:
        //     When not positioned in a valid recordset, 0; otherwise, the number of columns
        //     in the current record. The default is -1.
        public override int FieldCount
        {
            get
            {
                if (records == null)
                {
                    return 0;
                }
                else
                {
                    var columns = records.Columns;

                    if (columns == null)
                    {
                        return -1;
                    }
                    else
                    {
                        return records.Columns.Count;
                    }
                }
            }
        }

        public TrinoDataReader(RecordExecutor executor)
        {
            records = executor.Records;
        }

        /// <summary>
        /// Checks to see if any rows are present in the current page, will return false if no rows are available yet.
        /// </summary>
        public override bool HasRows => records.HasData().SafeResult();

        public override object this[int i]
        {
            get
            {
                return records.GetValue<object>(i);
            }
        }

        public override object this[string name]
        {
            get
            {
                return records.GetValue<object>(GetOrdinal(name));
            }
        }

        /// <summary>
        /// Does not affect the TrinoDataReader except to set the state to closed.
        /// </summary>
        public override void Close()
        {
            isClosed = true;
        }

        public override bool GetBoolean(int i)
        {
            return records.GetValue<bool>(i, "boolean", false);
        }

        public override byte GetByte(int i)
        {
            return records.GetValue<byte>(i, "tinyint", false);
        }

        public override long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            byte[] value = records.GetValue<byte[]>(i, "varbinary", false);
            if (value.Length > buffer.Length - bufferoffset)
            {
                throw new ArgumentException("Buffer is too small to hold the requested value");
            }

            // fill the buffer with the value
            for (int j = 0; j < length; j++)
            {
                buffer[j + bufferoffset] = value[(int)fieldOffset + j];
            }
            return value.Length;
        }

        public override char GetChar(int i)
        {
            return records.GetValue<char>(i, "char", false);
        }

        /// <summary>
        /// Reads the value of the specified column into an exstiing buffer.
        /// </summary>
        public override long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            string value = records.GetValue<string>(i);
            if (value == null)
            {
                return 0;
            }
            if (value.Length > buffer.Length - bufferoffset)
            {
                throw new ArgumentException("Buffer is too small to hold the requested value");
            }

            // fill the buffer with the value
            for (int j = 0; j < length; j++)
            {
                buffer[j + bufferoffset] = value[(int)fieldoffset + j];
            }
            return value.Length;
        }

        public override string GetDataTypeName(int i)
        {
            return records.GetColumn(i).type;
        }

        public override DateTime GetDateTime(int i)
        {
            if (records.GetColumn(i).type.EndsWith(TrinoTypeConverters.TRINO_WITH_TIME_ZONE_SUFFIX)) {
                return records.GetValue<DateTimeOffset>(i, TrinoTypeConverters.TRINO_TIMESTAMP_WITH_TIME_ZONE, false).DateTime;
            }
            else {
                return records.GetValue<DateTime>(i, TrinoTypeConverters.TRINO_TIMESTAMP, false);
            }
        }

        public DateTimeOffset GetDateTimeOffset(int i)
        {
            return records.GetValue<DateTimeOffset>(i, TrinoTypeConverters.TRINO_TIMESTAMP_WITH_TIME_ZONE, false);
        }

        public override decimal GetDecimal(int i)
        {
            if (records.GetColumn(i).type.StartsWith("decimal"))
            {
                // because Trino provides a big decimal, we must be absolutely sure it is a big decimal
                return records.GetValue<TrinoBigDecimal>(i, new string[] { "decimal", "bigdecimal" }, false).ToDecimal();
            }
            return records.GetValue<decimal>(i, new string[] { "integer", TrinoTypeConverters.TRINO_BIGINT, "smallint" }, false);
        }

        public override double GetDouble(int i)
        {
            return records.GetValue<double>(i, false);
        }

        public override IEnumerator GetEnumerator()
        {
            return records;
        }

        public override Type GetFieldType(int i)
        {
            return TrinoTypeConverters.GetClrTypeFromTrinoType(records.GetColumn(i));
        }

        public override float GetFloat(int i)
        {
            return records.GetValue<float>(i, "real", false);
        }

        public override Guid GetGuid(int i)
        {
            return records.GetValue<Guid>(i, "uuid", false);
        }

        public override short GetInt16(int i)
        {
            return records.GetValue<short>(i, "smallint", false);
        }

        public override int GetInt32(int i)
        {
            return records.GetValue<int>(i, new string[] { "integer", "smallint" }, false);
        }

        public override long GetInt64(int i)
        {
            return records.GetValue<long>(i, new string[] { TrinoTypeConverters.TRINO_BIGINT, "integer", "smallint" }, false);
        }

        public override string GetName(int i)
        {
            return records.GetColumn(i).name;
        }

        public override int GetOrdinal(string name)
        {
            for (int i = 0; i < records.Columns.Count; i++)
            {
                if (records.Columns[i].name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            throw new IndexOutOfRangeException($"Column name \"{name}\" not found.");
        }

        /// <summary>
        /// To support this: https://learn.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqldatareader.getschematable?view=netframework-4.8.1&viewFallbackFrom=dotnet-plat-ext-8.0
        /// </summary>
        public override DataTable GetSchemaTable()
        {
            DataTable schemaTable = new DataTable();
            schemaTable.Columns.Add(SchemaTableColumn.ColumnName, typeof(string));
            schemaTable.Columns.Add(SchemaTableColumn.ColumnOrdinal, typeof(int));
            schemaTable.Columns.Add(SchemaTableColumn.DataType, typeof(Type));
            schemaTable.Columns.Add(SchemaTableColumn.ProviderType, typeof(string));
            schemaTable.Columns.Add(SchemaTableColumn.NumericPrecision, typeof(int));
            schemaTable.Columns.Add(SchemaTableColumn.NumericScale, typeof(int));

            for (int i = 0; i < records.Columns.Count;i++)
            {
                TrinoColumn col = records.Columns[i];
                int precision = -1;
                int scale = -1;
                TrinoTypeConverters.GetNestedTypes(col.type, out string baseType, out string typeParameters);
                if (!string.IsNullOrEmpty(typeParameters) && baseType.Equals(TrinoTypeConverters.TRINO_DECIMAL, StringComparison.OrdinalIgnoreCase))
                {
                    string[] types = typeParameters.Split(',');
                    if (types.Length == 2)
                    {
                        int.TryParse(types[0].Trim(), out precision);
                        int.TryParse(types[1].Trim(), out scale);
                    }
                }
                schemaTable.Rows.Add(new object[] { col.name, i, col.GetColumnType(), baseType, precision, scale });

            }
            return schemaTable;
        }

        public DataTable GetSchemaTableTemplate()
        {
            return records.Columns.BuildDataTable();
        }

        public override string GetString(int i)
        {
            return records.Current[i].ToString();
        }

        public override object GetValue(int i)
        {
            return records.GetValue<object>(i);
        }

        public override int GetValues(object[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("Array to populate is null.");
            }

            for (int i = 0; values != null && i < values.Length && i < records.Columns.Count; i++)
            {
                values[i] = records.GetValue<object>(i);
            }
            return values.Length;
        }

        public override bool IsDBNull(int i)
        {
            records.GetColumn(i);
            return records.Current[i] == null;
        }

        public override bool NextResult()
        {
            throw new NotSupportedException("Trino supports only a single result per query.");
        }

        /// <summary>
        /// The default position of a data reader is before the first record. Therefore, you must call Read to begin accessing data.
        /// </summary>
        public override bool Read()
        {
            return records.MoveNext();
        }

        /// <summary>
        /// The default position of a data reader is before the first record. Therefore, you must call Read to begin accessing data.
        /// </summary>
        public override async Task<bool> ReadAsync(CancellationToken token)
        {
            // ignore cancellation token because the command already has a cancellation token
            return await records.MoveNextAsync().ConfigureAwait(false);
        }
    }
}
