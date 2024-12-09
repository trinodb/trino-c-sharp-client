using Trino.Client.Model.StatementV1;
using System.Collections.Generic;
using System.Text;

namespace Trino.Client.Model
{
    /// <summary>
    /// Main model class representing a Trino statement API response
    /// </summary>
    internal class QueryResultPage : Statement
    {
        /// <summary>
        /// Gets or sets the data rows in the response.
        /// </summary>
        public List<List<object>> data
        {
            get;
            set;
        } = new List<List<object>>();

        /// <summary>
        /// Gets or sets the columns (schema definition) in the response.
        /// </summary>
        public IList<TrinoColumn> columns
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether this message contains data
        /// </summary>
        public bool HasData
        {
            get
            {
                return this.data != null && data.Count > 0;
            }
        }

        /// <summary>
        /// Construct a string containing the data in this response
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.ToTsv().ToString();
        }

        /// <summary>
        /// Construct a TSV from the data in the response
        /// </summary>
        /// <returns>TSV representation of the page.</returns>
        private string ToTsv(char separator = '\t', bool includeColumnNames = true)
        {
            StringBuilder sv = new StringBuilder();
            bool isNewRow = true;
            if (includeColumnNames)
            {
                foreach (TrinoColumn col in this.columns)
                {
                    if (isNewRow)
                    {
                        isNewRow = false;
                    }
                    else
                    {
                        sv.Append(separator);
                    }
                    sv.Append(col.name);
                }
                sv.AppendLine();
            }

            isNewRow = true;
            foreach (List<object> row in this.data)
            {
                foreach (object value in row)
                {
                    if (isNewRow)
                    {
                        isNewRow = false;
                    }
                    else
                    {
                        sv.Append(separator);
                    }
                    sv.Append(value.ToString());
                }
                sv.AppendLine();
                isNewRow = true;
            }

            return sv.ToString();
        }
    }
}
