using Trino.Client.Logging;
using Trino.Client.Model.StatementV1;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Trino.Client
{
    /// <summary>
    /// Queue to hold the pages of data returned from Trino.
    /// </summary>
    internal class PageQueue
    {
        private readonly StatementClientV1 client;
        // BlockingCollection offers no advantage over ConcurrentQueue for this use case.
        private readonly ConcurrentQueue<ResponseQueueStatement> responseQueue = new ConcurrentQueue<ResponseQueueStatement>();
        private readonly ConcurrentBag<Exception> errors = new ConcurrentBag<Exception>();

        // The actual buffer size
        private readonly long bufferSize;
        private readonly CancellationToken cancellationToken;
        private readonly ILoggerWrapper logger;
        private readonly IList<Action<TrinoStats, TrinoError>> queryStatusNotifications;
        private readonly SemaphoreSlim signalUpdatedQueue = new SemaphoreSlim(0, int.MaxValue);
        private readonly SemaphoreSlim signalFoundResult = new SemaphoreSlim(0, 1);
        private readonly SemaphoreSlim signalColumnsRead = new SemaphoreSlim(0, 1);

        private readonly object readAheadLock = new object();

        // backoff for checking the queue for new pages, values tuned 2024
        private const int maxWaitForQueueTimeoutMsec = 10000;
        private const int queueCheckBackoff = 100;
        private int waitForQueueTimeoutMsec = 50;
        private Task readAhead;

        internal PageQueue(ILoggerWrapper logger, IList<Action<TrinoStats, TrinoError>> queryStatusNotifications, StatementClientV1 client, long bufferSize, bool isQuery)
        {
            if (bufferSize == 0)
            {
                throw new ArgumentException("Buffer size of zero is not allowed as no rows can be read.");
            }

            this.logger = logger;
            this.queryStatusNotifications = queryStatusNotifications;
            this.client = client;
            this.bufferSize = bufferSize;
            IsQuery = isQuery;
        }

        /// <summary>
        /// The connection to Trino will consume query results.
        /// </summary>
        public bool IsQuery { get; private set; }

        /// <summary>
        /// The schema columns.
        /// </summary>
        internal IList<TrinoColumn> Columns { get; private set; }
        internal bool IsEmpty { get { return responseQueue.Count == 0; } }
        /// <summary>
        /// The last response from the statement endpoint.
        /// </summary>
        internal Statement LastStatement { get; private set; }

        /// <summary>
        /// True if the last page has been reached.
        /// </summary>
        internal bool IsLastPage => LastStatement != null && LastStatement.IsLastPage;

        /// <summary>
        /// The client state.
        /// </summary>
        internal QueryState State { get => client.State; }

        /// <summary>
        /// True if results have been found in Trino responses.
        /// </summary>
        internal bool HasResults { get; private set; }

        /// <summary>
        /// Starts a thread to asynchronously read ahead to fill the queue with the result set.
        /// </summary>
        internal void StartReadAhead()
        {
            logger?.LogDebug("Attempt to start read ahead: queryId: {0}", LastStatement?.id);
            // Try to read.
            // Starts a task to read. If a read task has already started, ignore.
            if (ShouldReadAheadToNextPage())
            {
                lock (readAheadLock) {
                    if (readAhead == null || readAhead.IsCompleted)
                    {
                        // Thread to read ahead.
                        this.readAhead = Task.Run(async () =>
                        {
                            logger?.LogDebug("Starting read ahead: queryId: {0}", LastStatement?.id);
                            await ReadAhead().ConfigureAwait(false);
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Reads ahead until query is stopped or buffer is full.
        /// </summary>
        private async Task ReadAhead()
        {
            try
            {
                // Will keep reading from Trino until the query is complete or the buffer is over full.
                while (ShouldReadAheadToNextPage() && !ShouldStopReading())
                {
                    ResponseQueueStatement statementResponse = await client.Advance().ConfigureAwait(false);

                    // if schema is discovered, make it available
                    if (this.Columns == null && statementResponse.Response.columns != null)
                    {
                        this.Columns = statementResponse.Response.columns;
                        signalColumnsRead.Release();
                    }

                    // If results are needed, queue the result pages, otherwise they can be ignored.
                    if (this.IsQuery && statementResponse.Response.HasData)
                    {
                        this.responseQueue.Enqueue(statementResponse);
                        HasResults = true;
                        this.LastStatement = statementResponse.Response;
                        signalUpdatedQueue.Release();
                    }
                    else
                    {
                        this.LastStatement = statementResponse.Response;
                    }
                }

                if (client.State.IsFinished)
                {
                    logger?.LogDebug("Trino Query Executor: Set finished state, queryId:{0}", LastStatement?.id);
                    PublishStatus(this.LastStatement?.stats, this.LastStatement?.error);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError("Trino Query Executor: {0}", ex.ToString());
                errors.Add(ex);
                if (ex is TrinoException exception)
                {
                    PublishStatus(this.LastStatement?.stats, exception.Error);
                }
            }
        }

        /// <summary>
        /// Determines if there is a reason to stop reading.
        /// </summary>
        private bool ShouldStopReading()
        {
            if (this.cancellationToken != null && this.cancellationToken.IsCancellationRequested)
            {
                logger?.LogDebug("Trino Query Executor: query cancelled.");
                errors.Add(new OperationCanceledException("Query cancelled"));
                return false;
            }

            if (client.IsTimeout)
            {
                logger?.LogDebug("Trino Query Executor: terminating due to timeout.");
                errors.Add(new TimeoutException("Query timed out"));
                return true;
            }

            if (errors.Count > 0)
            {
                logger?.LogDebug("Trino Query Executor: terminating due to exceptions: {0} ", string.Join(",", errors.Select(e => e.ToString())));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Continue to read pages until the queue is full or the executor is finished.
        /// Or if the query does not return results.
        /// </summary>
        private bool ShouldReadAheadToNextPage()
        {
            if (!IsLastPage && !client.State.IsClientAborted && !client.State.IsClientError)
            {
                if (IsQuery)
                {
                    // if this is a query, but not finished, only read ahead the size of the buffer.
                    // Note, buffer is a soft approximate limit based on the string size of the pages.
                    long queueSize = 0;
                    foreach (ResponseQueueStatement page in responseQueue)
                    {
                        queueSize += page.SizeBytes;
                    }
                    return queueSize < bufferSize;
                }
                else
                {
                    // if non-query, results are going to be thrown away, always read ahead to the end of the query.
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Fetches list of columns for the result set.
        /// </summary>
        internal async Task<IList<TrinoColumn>> GetColumns()
        {
            if (this.Columns == null)
            {
                this.StartReadAhead();
                while (this.Columns == null && !IsLastPage && !ShouldStopReading())
                {
                    await signalColumnsRead.WaitAsync(queueCheckBackoff).ConfigureAwait(false);
                    ThrowIfErrors();
                }
            }
            return this.Columns;
        }

        /// <summary>
        /// Throw an exception if there are any errors during the read.
        /// </summary>
        internal void ThrowIfErrors()
        {
            if (errors.Any())
            {
                throw new TrinoAggregateException(errors);
            }
        }

        /// <summary>
        /// Attempt to dequeue the next available page. Poses an exponential backoff if result is not found.
        /// </summary>
        /// <returns>The next page, or null, if not available</returns>
        internal async Task<ResponseQueueStatement> DequeueOrNull()
        {
            if (!this.responseQueue.TryDequeue(out ResponseQueueStatement response))
            {
                // wait for signal of next dequeue
                if (!(await signalUpdatedQueue.WaitAsync(waitForQueueTimeoutMsec).ConfigureAwait(false)))
                {
                    // ensure readahead is running if there is nothing to dequeue
                    // backoff wait time because aggressive checks only benefit short running queries, and the signal covers most cases
                    waitForQueueTimeoutMsec = Math.Min(waitForQueueTimeoutMsec + queueCheckBackoff, maxWaitForQueueTimeoutMsec);
                }
            }
            return response;
        }

        internal async Task<bool> Cancel()
        {
            return await this.client.Cancel().ConfigureAwait(false);
        }

        /// <summary>
        /// Checks to see if any results have been returned. If not, will wait for results.
        /// </summary>
        internal async Task<bool> HasData()
        {
            if (!this.IsQuery)
            {
                return false;
            }
            else if (this.HasResults)
            {
                return true;
            }
            else
            {
                this.StartReadAhead();
                while (!this.HasResults && !IsLastPage && !ShouldStopReading())
                {
                    await signalFoundResult.WaitAsync(queueCheckBackoff).ConfigureAwait(false);
                    ThrowIfErrors();
                }
            }
            return this.HasResults;
        }

        /// <summary>
        /// Publishes a notification if the query fails and when the query finishes.
        /// </summary>
        private void PublishStatus(TrinoStats stats, TrinoError error)
        {
            if (queryStatusNotifications != null)
            {
                foreach (Action<TrinoStats, TrinoError> notifier in queryStatusNotifications)
                {
                    notifier.Invoke(stats, error);
                }
            }
        }

    }
}
