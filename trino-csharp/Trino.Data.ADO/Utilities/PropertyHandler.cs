using Trino.Client;

using System;

namespace Trino.Data.ADO.Utilities
{
    internal class PropertyHandler
    {
        public Func<ClientSessionProperties, string> Serializer { get; set; }
        public Action<ClientSessionProperties, string> Deserializer { get; set; }

        public PropertyHandler(Func<ClientSessionProperties, string> serializer, Action<ClientSessionProperties, string> deserializer)
        {
            Serializer = serializer;
            Deserializer = deserializer;
        }
    }
}
