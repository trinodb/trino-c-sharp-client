using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Collections;
using Trino.Client.Model.StatementV1;
using Trino.Client.Logging;

namespace Trino.Client
{
    /// <summary>
    /// Provides asynchronous streaming query execution for Trino.
    /// </summary>
    public class RecordExecutor : IEnumerable<List<object>>
    {
        /// <summary>
        /// Gets the source of data pages containing query results.
        /// </summary>
        public Records Records { get; }

        /// <summary>
        /// Creates a new RecordExecutor with the specified record enumerator.
        /// </summary>
        /// <param name="records">The record enumerator containing query results.</param>
        private RecordExecutor(Records records)
        {
            Records = records;
        }

        /// <summary>
        /// Executes a query and returns results as an enumerable using default settings.
        /// </summary>
        /// <param name="session">The client session for executing the query.</param>
        /// <param name="statement">The SQL statement to execute.</param>
        /// <returns>A RecordExecutor for enumerating the query results.</returns>
        public static async Task<RecordExecutor> Execute(
            ClientSession session,
            string statement)
        {
            return await Execute(
                logger: null,
                queryStatusNotifications: null,
                session: session,
                statement: statement,
                queryParameters: null,
                bufferSize: Constants.DefaultBufferSizeBytes,
                isQuery: true,
                cancellationToken: CancellationToken.None)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Creates and initializes a RecordExecutor for query execution.
        /// This factory method is used because .NET Standard 2.0 does not support IAsyncEnumerable.
        /// </summary>
        /// <param name="logger">Optional logger for tracking execution.</param>
        /// <param name="queryStatusNotifications">Optional handlers for receiving query status updates.</param>
        /// <param name="session">The client session for executing the query.</param>
        /// <param name="statement">The SQL statement to execute.</param>
        /// <param name="queryParameters">Optional parameters for the query.</param>
        /// <param name="bufferSize">Size of the buffer for reading data.</param>
        /// <param name="isQuery">Indicates if this is a query operation requiring results.</param>
        /// <param name="cancellationToken">Token for cancelling the operation.</param>
        /// <returns>A RecordExecutor instance for enumerating the query results.</returns>
        public static async Task<RecordExecutor> Execute(
            ILoggerWrapper logger,
            IList<Action<TrinoStats, TrinoError>> queryStatusNotifications,
            ClientSession session,
            string statement,
            IEnumerable<QueryParameter> queryParameters,
            long bufferSize,
            bool isQuery,
            CancellationToken cancellationToken)
        {
            session.Auth?.AuthorizeAndValidate();

            StatementClientV1 statementClient = new StatementClientV1(session, cancellationToken, logger);
            logger?.LogInformation("Trino: Created client, starting query: {0}", statement);
            
            await statementClient.GetInitialResponse(
                statement, 
                queryParameters, 
                cancellationToken)
                .ConfigureAwait(false);

            PageQueue pageQueue = new PageQueue(
                logger, 
                queryStatusNotifications, 
                statementClient, 
                bufferSize, 
                isQuery);

            pageQueue.StartReadAhead();

            Pages pageEnumerator = new Pages(logger, pageQueue);
            Records recordEnumerator = pageEnumerator.GetRecords();
            
            return new RecordExecutor(recordEnumerator);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the query results.
        /// </summary>
        public IEnumerator<List<object>> GetEnumerator()
        {
            return Records;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the query results.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}