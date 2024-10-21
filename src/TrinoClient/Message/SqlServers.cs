using System.Collections.Immutable;

namespace TrinoClient.Message
{
    public sealed class SqlServers
    {
        public SqlServers(ImmutableList<string> servers)
        {
            Servers = servers;
        }

        public ImmutableList<string> Servers { get; }
    }
}
