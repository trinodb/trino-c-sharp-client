using Trino.Client.Model.StatementV1;

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Trino.Client.Utils
{
    public static class DataTableUtils
    {
        /// <summary>
        /// Constructs a data table prepopulated with schema.
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public static DataTable BuildDataTable(this IList<TrinoColumn> columns)
        {
            DataTable dt = new DataTable();
            if (columns != null && dt.Columns.Count == 0)
            {
                foreach (TrinoColumn column in columns)
                {
                    dt.Columns.Add(new DataColumn(column.name, column.GetColumnType()));
                }
            }
            return dt;
        }

        /// <summary>
        /// Constructs a data table prepopulated with schema asynchronously.
        /// </summary>
        /// <exception cref="TrinoException"></exception>
        public async static Task<DataTable> BuildDataTableAsync(this RecordExecutor recordExecutor)
        {
            Records records = recordExecutor.Records;
            await records.PopulateColumnsAsync().ConfigureAwait(false);
            DataTable dt = records.Columns.BuildDataTable();
            int rowCount = 0;
            while (await records.MoveNextAsync().ConfigureAwait(false))
            {
                if (records.Current.Count != dt.Columns.Count)
                {
                    throw new TrinoException($"Column count {records.Current.Count} does not match schema column count {dt.Columns.Count}.");
                }

                DataRow dr = dt.NewRow();
                for (int colIndex = 0; colIndex < records.Current.Count; colIndex++)
                {
                    if (records.Current[colIndex] != null)
                    {
                        dr[colIndex] = Convert.ChangeType(records.Current[colIndex], dt.Columns[colIndex].DataType);
                    }
                }
                dt.Rows.Add(dr);
                rowCount++;
            }
            return dt;
        }
    }
}
