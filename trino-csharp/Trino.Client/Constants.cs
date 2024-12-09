using System;

namespace Trino.Client
{
    /// <summary>
    /// Contains the constants used by the Trino client.
    /// </summary>
    public static class Constants
    {
        public const string TrinoClientName = "Trino Microsoft .NET Client";

        // Increasing the max target result size to 5MB over the default increases read performance by 30%.
        internal const long MaxTargetResultSizeMB = 5;

        // The default buffer size for the query executor.
        // Set relative to optimal value of MaxTargetResultSizeMB
        public const long DefaultBufferSizeBytes = (MaxTargetResultSizeMB * 10) * 1024 * 1024;

        /// <summary>
        /// HTTP connection timeout
        /// </summary>
        public static TimeSpan HttpConnectionTimeout { get => TimeSpan.FromSeconds(100); }
    }
}
