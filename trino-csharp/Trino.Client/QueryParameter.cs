using System;
using System.Collections.Generic;
using System.Linq;

namespace Trino.Client
{
    /// <summary>
    /// A query parameter for a Trino query.
    /// </summary>
    public class QueryParameter
    {
        public QueryParameter(object value)
        {
            Value = value;
        }

        /// <summary>
        /// The value of the parameter.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Get the string representation of the parameter value that can be used in a SQL expression.
        /// </summary>
        internal string SqlExpressionValue
        {
            get
            {
                if (Value == null)
                {
                    return "NULL";
                }
                else if (Value is string)
                {
                    return $"'{Value.ToString().Replace("'", "''")}'";
                }
                else if (Value is DateTime dateTime)
                {
                    return $"timestamp '{dateTime:yyyy-MM-dd HH:mm:ss.fff}'";
                }
                else if (Value is DateTimeOffset offset)
                {
                    return $"\"timestamp with time zone\" '{offset:yyyy-MM-dd HH:mm:ss.fff zzz}'";
                }
                else if (Value is TimeSpan span)
                {
                    return $"'{span:c}'";
                }
                else if (Value is Guid)
                {
                    return $"'{Value}'";
                }
                else if (Value is bool b)
                {
                    return b ? "TRUE" : "FALSE";
                }
                else if (Value is byte[] binary)
                {
                    return $"X'{BitConverter.ToString(binary).Replace("-", "")}'";
                }
                else if (Value is IEnumerable<object> enumerable)
                {
                    var items = enumerable.Cast<object>()
                        .Select(item => new QueryParameter(item).SqlExpressionValue);
                    return $"({string.Join(", ", items)})";
                }
                else
                {
                    return Value.ToString();
                }
            }
        }
    }
}
