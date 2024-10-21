

using TrinoClient.Message;

namespace TrinoClient
{
    public sealed class SqlData
    {
        public SqlData(IQueryResponse response)
        {
            Response = response;
        }
        public IQueryResponse Response { get; }
    }
}
