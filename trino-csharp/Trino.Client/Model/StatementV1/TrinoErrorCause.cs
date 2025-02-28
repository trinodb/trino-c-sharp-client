using System.Collections.Generic;

namespace Trino.Client.Model.StatementV1
{
    /// <summary>
    /// Describes the cause of a Trino error.
    /// </summary>
    public class TrinoErrorCause
    {
        /// <summary>
        /// Trino error type
        /// </summary>
        public string type { get; set; }

        /// <summary>
        /// Trino error message
        /// </summary>
        public string message { get; set; }

        /// <summary>
        /// Location of the error in the query
        /// </summary>
        public TrinoErrorLocation errorLocation { get; set; }

        /// <summary>
        /// Stack trace of the error
        /// </summary>
        public List<string> stack { get; set; }

        /// <summary>
        /// Suppressed errors
        /// </summary>
        public List<TrinoErrorCause> suppressed { get; set; }

        /// <summary>
        /// Cause of the error
        /// </summary>
        public TrinoErrorCause cause { get; set; }
    }
}
