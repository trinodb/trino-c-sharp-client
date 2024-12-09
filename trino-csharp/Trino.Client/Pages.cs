using Trino.Client.Logging;
using Trino.Client.Model;
using Trino.Client.Model.StatementV1;
using Trino.Client.Utils;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Trino.Client
{
    /// <summary>
    /// Page iterator for Trino query results.
    /// </summary>
    internal class Pages : IEnumerator<QueryResultPage>, IAsyncEnumeratorPlaceholder<QueryResultPage>, IDisposable
    {
        private readonly PageQueue pageQueue;
        private readonly ILoggerWrapper logger;

        private readonly SemaphoreSlim allowOneThreadToReadPages = new SemaphoreSlim(1, 1);

        /// <summary>
        /// The current page of data (if any).
        /// </summary>
        public QueryResultPage Current { get; private set; }

        /// <summary>
        /// The executing state of the query containing status and stats.
        /// </summary>
        public Statement LastStatement => pageQueue.LastStatement;

        /// <summary>
        /// The most recent stats for this query.
        /// </summary>
        public TrinoStats LastStats => pageQueue.LastStatement.stats;

        /// <summary>
        /// Required for IEnumerator (same as Current).
        /// </summary>
        object IEnumerator.Current => this.Current;

        public Pages(ILoggerWrapper logger, PageQueue pageQueue)
        {
            this.pageQueue = pageQueue;
            this.logger = logger;
        }

        public void Reset()
        {
            throw new NotSupportedException("Cannot reset Trino stream to beginning.");
        }

        /// <summary>
        /// Wait for query to finish executing.
        /// </summary>
        public async Task<Statement> ReadToEnd()
        {
            logger?.LogDebug("Trino Query Executor: Waiting for finish queryid:{0}", this.LastStatement?.id);
            while (await MoveNextAsync().ConfigureAwait(false)) ;
            return this.LastStatement;
        }

        /// <summary>
        /// Allows the caller to wait for the first page to be read.
        /// Can be used if client wants schema before reading.
        /// </summary>
        internal async Task<IList<TrinoColumn>> WaitAndGetColumns()
        {
            return await pageQueue.GetColumns().ConfigureAwait(false);
        }

        /// <summary>
        /// Consume the next page synchroniously.
        /// </summary>
        public bool MoveNext()
        {
            return this.MoveNextAsync().SafeResult();
        }

        /// <summary>
        /// Consume the next page.
        /// </summary>
        public async Task<bool> MoveNextAsync()
        {
            try
            {
                // prevent access from multiple threads
                if (!allowOneThreadToReadPages.Wait(0))
                {
                    throw new TrinoException("Only one reader can advance pages at a time.");
                }
                
                logger?.LogDebug("Trino Query Executor: Next page requested: queryId:{0}", this.LastStatement?.id);
                pageQueue.ThrowIfErrors();

                // If the executor is finished and at least one page is read (Page != null) and the queue is empty, then we are done.
                if (IsFinished())
                {
                    logger?.LogDebug("Trino Query Executor: Query finished queryId:{0}", this.LastStatement?.id);
                    return false;
                }

                pageQueue.StartReadAhead();

                ResponseQueueStatement response;
                while ((response = await pageQueue.DequeueOrNull().ConfigureAwait(false)) == null)
                {
                    pageQueue.ThrowIfErrors();

                    if (this.IsFinished())
                    {
                        return false;
                    }
                }

                this.Current = response.Response;
            }
            finally
            {
                allowOneThreadToReadPages.Release();
            }

            return true;
        }

        /// <summary>
        /// Indicates the IEnumerator has reached the end.
        /// Check that the query is finished, the queue is empty, and all pages are read.
        /// </summary>
        /// <returns></returns>
        public bool IsFinished()
        {
            if (!this.pageQueue.IsQuery && this.pageQueue.State.IsFinished)
            {
                // if a non-query, no pages will be generated, so as soon as execution finishes server side, query is done.
                return true;
            }

            return this.pageQueue.State.IsFinished && pageQueue.IsEmpty
                && this.LastStatement.IsLastPage;
        }

        /// <summary>
        /// Cancels and terminates the query.
        /// </summary>
        public void Dispose()
        {
            pageQueue.Cancel().Wait();
        }

        internal async Task<bool> HasData()
        {
            return await this.pageQueue.HasData().ConfigureAwait(false);
        }

        /// <summary>
        /// Get record enumerator
        /// </summary>
        internal Records GetRecords()
        {
            return new Records(this.logger, this);
        }

        internal bool IsEmptyPage
        {
            get {
                return Current == null;
            }
        }
    }
}
