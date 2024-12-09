namespace Trino.Client.Model.StatementV1
{
    /// <summary>
    /// Model class for Trino statement API response
    /// </summary>
    public class TrinoError
    {
        /// <summary>
        /// Gets or sets the error type of the Trino error message.
        /// </summary>
        public string type { get; set; }

        /// <summary>
        /// Gets or sets the text of the Trino error message.
        /// </summary>
        public string message
        {
            get;
            set;
        }

        /// <summary>
        /// Gets of sets the Trino error code.
        /// </summary>
        public long errorCode { get; set; }

        /// <summary>
        /// Gets or sets the Trino error name
        /// </summary>
        public string errorName { get; set; }

        /// <summary>
        /// Gets or sets the Trino error type
        /// </summary>
        public string errorType { get; set; }

        /// <summary>
        /// Gets or sets the Trino query error line positions.
        /// </summary>
        public TrinoErrorLocation errorLocation { get; set; }

        /// <summary>
        /// Gets or sets the Trino error failure details which includes the stack trace.
        /// </summary>
        public TrinoErrorCause failureInfo
        {
            get;
            set;
        }
    }
}
