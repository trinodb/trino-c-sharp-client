
using TrinoClient.Internal;

namespace TrinoClient.Message
{
    public class StatsResponse : IQueryResponse
    {
        public StatsResponse(StatementStats stats)
        {
            Stats = stats;
        }

        public StatementStats Stats { get; }
    }
}
