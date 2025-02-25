using System;

namespace Trino.Client.Model.StatementV1
{
    /// <summary>
    /// Statistics about query execution
    /// </summary>
    public class TrinoStats
    {
        /// <summary>
        /// State of the query
        /// </summary>
        public string state
        {
            get;
            set;
        }

        /// <summary>
        /// True, if the query is queued
        /// </summary>
        public bool queued
        {
            get;
            set;
        }

        /// <summary>
        /// True, if the query was scheduled
        /// </summary>
        public bool scheduled
        {
            get;
            set;
        }

        public long nodes { get; set; }
        public long totalSplits { get; set; }
        public long queuedSplits { get; set; }
        public long runningSplits { get; set; }
        public long completedSplits { get; set; }
        public long cpuTimeMillis { get; set; }
        public long wallTimeMillis { get; set; }
        public long queuedTimeMillis { get; set; }
        public long elapsedTimeMillis { get; set; }
        public long processedRows { get; set; }
        public long processedBytes { get; set; }
        public long peakMemoryBytes { get; set; }
        public long spilledBytes { get; set; }
        public double progressPercentage { get; set; }

        /// <summary>
        /// Get the query execution progress percentage as a ratio
        /// </summary>
        /// <returns>Progress executing the query</returns>
        public double GetProgressRatio()
        {
            if (totalSplits == 0)
            {
                return 0;
            }
            else
            {
                return Math.Round(completedSplits / (double)totalSplits, 2);
            }
        }
    }
}
