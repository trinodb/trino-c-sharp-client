

using TrinoClient.Internal;

namespace TrinoClient.Message
{
    public sealed class DataResponse : IQueryResponse
    {
        public DataResponse(IList<Dictionary<string, object>> data, StatementStats statementStats)
        {
            Data = data;
            StatementStats = statementStats;
        }

        public IList<Dictionary<string, object>> Data { get; }
        public StatementStats StatementStats { get; }
    }
}
