using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Trino.Client.Model;
using System.Collections;
using Trino.Client.Model.StatementV1;
using Trino.Client.Logging;

namespace Trino.Client
{
    /// <summary>
    /// Asynchronous streaming query executor SDK for Trino.
    /// </summary>
    internal class PageExecutor : IEnumerable<QueryResultPage>
    {
        /// <summary>
        /// Get the source of pages.
        /// </summary>
        public Pages Pages { get; private set; }

        public PageExecutor(Pages enumerator)
        {
            this.Pages = enumerator;
        }

        public async static Task<PageExecutor> Execute(
            ClientSession session,
            string statement) 
        {
            return await Execute(null, null, session, statement, null, int.MaxValue, true, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Factory method for enumerable.
        /// .NET standard 2.0 does not have IAsyncEnumerable, so we use this method to create an enumerable.
        /// </summary>
        public async static Task<PageExecutor> Execute(
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

            StatementClientV1 client = new StatementClientV1(session, cancellationToken, logger);
            logger?.LogInformation("Trino: Created client, starting query: {0}", statement);
            await client.GetInitialResponse(statement, queryParameters, cancellationToken).ConfigureAwait(false);

            PageQueue pageQueue = new PageQueue(logger, queryStatusNotifications, client, bufferSize, isQuery);
            pageQueue.StartReadAhead();

            Pages enumerator = new Pages(logger, pageQueue);
            return new PageExecutor(enumerator);
        }

        public IEnumerator<QueryResultPage> GetEnumerator()
        {
            return Pages;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
