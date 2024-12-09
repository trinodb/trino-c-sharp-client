using System;
using System.Collections.Generic;

namespace Trino.Client
{
    /// <summary>
    /// Aggregate exception for Trino client
    /// </summary>
    public class TrinoAggregateException : AggregateException
    {
        /// <summary>
        /// Create a TrinoAggregateException
        /// </summary>
        public TrinoAggregateException(IEnumerable<Exception> exceptions) : base(exceptions)
        {
        }
    }
}
