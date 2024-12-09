using Trino.Client.Model;

namespace Trino.Client
{
    /// <summary>
    /// A queue item in the response queue.
    /// </summary>
    internal class ResponseQueueStatement
    {
        internal QueryResultPage Response { get; private set; }

        internal int SizeBytes { get; private set; }

        internal ResponseQueueStatement(QueryResultPage statementResponse, int sizeBytes)
        {
            this.Response = statementResponse;
            this.SizeBytes = sizeBytes;
        }
    }
}
