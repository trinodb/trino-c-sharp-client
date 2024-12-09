namespace Trino.Client.Model.StatementV1
{
    /// <summary>
    /// Main model class representing a Trino statement API response
    /// </summary>
    public class Statement
    {
        /// <summary>
        /// Gets or sets the query identifier
        /// </summary>
        public string id
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the stats of the Trino query execution.
        /// </summary>
        public TrinoStats stats
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the next URI in the paged Trino response (null indicates no more pages).
        /// </summary>
        public string nextUri
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the URI that points to the Trino query information UX
        /// </summary>
        public string infoUri
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the error in the Trino response (if any).
        /// </summary>
        public TrinoError error
        {
            get;
            set;
        }

        public bool IsLastPage { get { return string.IsNullOrEmpty(nextUri); } }
    }
}
