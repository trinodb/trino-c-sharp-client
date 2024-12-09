using Trino.Client;
using Trino.Client.Auth;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Trino.Data.ADO.Server
{
    public enum ServerType
    {
        Presto,
        Trino
    }
    /// <summary>
    /// Connection properties for the Trino Client
    /// </summary>
    public class TrinoConnectionProperties
    {
        public TrinoConnectionProperties()
        {
        }

        /// <summary>
        /// Any additional headers to be passed to the Trino cluster.
        /// </summary>
        public Dictionary<string, string> AdditionalHeaders { get; set; }

        /// <summary>
        /// Authentication mode, null for no authentication.
        /// </summary>
        public ITrinoAuth Auth { get; set; }

        /// <summary>
        /// Allow host name CN mismatch.
        /// </summary>
        public bool AllowHostNameCNMismatch { get; set; }

        /// <summary>
        /// Allow self signed certificate.
        /// </summary>
        public bool AllowSelfSignedServerCert { get; set; }

        /// <summary>
        /// Allows the server type to be configured which allows customization of the protocol header prefix.
        /// </summary>
        private string serverType = "Trino";
        public string ServerType
        {
            get => serverType;
            set
            {
                if (Enum.TryParse(value, true, out ServerType result))
                {
                    serverType = result.ToString();
                }
                else
                {
                    throw new ArgumentException($"Invalid server type: {value}. Must be 'Presto' or 'Trino'.");
                }
            }
        }

        /// <summary>
        /// Allows header prefix value to be set, for example: if you wanted X-Trino, you would set this to be "Trino"
        /// </summary>
        public string HeaderPrefix => ServerType;


        /// <summary>
        /// The default catalog to use for unqualified table names in SQL statements.
        /// </summary>
        public string Catalog { get; set; }

        /// <summary>
        /// Extra information about the client.
        /// </summary>
        public string ClientInfo { get; set; }

        /// <summary>
        /// Tags to be associated with the client connection.
        /// </summary>
        public HashSet<string> ClientTags { get; set; }

        /// <summary>
        /// Whether compression should be enabled.
        /// </summary>
        public bool CompressionDisabled { get; set; }

        /// <summary>
        /// The endpoint host name of the Trino server.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The host path of the Trino endpoint.
        /// </summary>
        public string HostPath { get; set; }

        /// <summary>
        /// Host port for Trino.
        /// </summary>
        public int Port { get; set; } = ClientSessionProperties.DefaultPort;

        /// <summary>
        /// Set the default SQL path for the session. Useful for setting a catalog and schema location for catalog routines.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Set true to specify using TLS/HTTPS for connections.
        /// </summary>
        public bool EnableSsl { get; set; } = true;

        /// <summary>
        /// Authorization roles to use for catalogs.
        /// </summary>
        public IDictionary<string, ClientSelectedRole> Roles { get; set; }

        /// <summary>
        /// The default schema to use for unqualified table names in SQL statements.
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// Properties to be associated with the client session.
        /// </summary>
        public IDictionary<string, string> SessionProperties { get; set; }

        /// <summary>
        /// Source name for the Trino query.
        /// </summary>
        public string Source { get; set; } = Constants.TrinoClientName;

        /// <summary>
        /// The trace token to be associated with the client connection.
        /// </summary>
        public string TraceToken { get; set; }

        /// <summary>
        /// Get Trino cluster Uri.
        /// </summary>
        public Uri Server {
            get
            {
                return ClientSessionProperties.GetServerUri(this.Host, this.EnableSsl, this.Port, this.HostPath);
            }
            set
            {
                // decompose into parts
                this.Host = value.Host;
                this.Port = value.Port;
                this.HostPath = value.AbsolutePath;
                this.EnableSsl = value.Scheme == Uri.UriSchemeHttps;
            }
        }

        /// <summary>
        /// Trusted certificate path.
        /// </summary>
        public string TrustedCertPath { get; set; }

        /// <summary>
        /// Trusted certificate embeded string.
        /// </summary>
        public string TrustedCertificate { get; set; }

        /// <summary>
        /// Use system trust store.
        /// </summary>
        public bool UseSystemTrustStore { get; set; }

        /// <summary>
        /// The timezone for query processing. Defaults to the timezone of the Trino cluster, and not the timezone of the client.
        /// </summary>
        public string TimeZone { get; set; }

        /// <summary>
        /// Sets the username for Username and password authentication.
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Allows test connection to be enabled. If false, any connection test will ignored, even if explicitly called.
        /// </summary>
        public bool TestConnection { get; set; }

        /// <summary>
        /// Tests the connection on every connection open.
        /// </summary>
        public bool TestConnectionOnConnectionOpen { get; set; }

        /// <summary>
        /// Sets the duration for query processing, after which, the client request is terminated.
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        public ClientSession GetSession()
        {
            ClientSessionProperties properties = new ClientSessionProperties()
            {
                Catalog = this.Catalog,
                Schema = this.Schema,
                Path = this.Path,
                TimeZone = this.TimeZone,
                ClientInfo = this.ClientInfo,
                ClientTags = this.ClientTags == null ? new HashSet<string>() : new HashSet<string>(this.ClientTags),
                CompressionDisabled = this.CompressionDisabled,
                // deep copy properties dictionary
                Properties = this.SessionProperties == null ? new Dictionary<string, string>() : this.SessionProperties.ToDictionary(entry => entry.Key, entry => entry.Value),
                Roles = this.Roles == null ? new Dictionary<string, ClientSelectedRole>() : this.Roles.ToDictionary(entry => entry.Key, entry => entry.Value),
                Source = this.Source,
                Server = this.Server,
                ClientRequestTimeout = this.Timeout,
                TestConnection = this.TestConnection,
                TraceToken = this.TraceToken,
                User = this.User,
                AdditionalHeaders = this.AdditionalHeaders == null ? new Dictionary<string, string>() : this.AdditionalHeaders.ToDictionary(entry => entry.Key, entry => entry.Value),
                AllowHostNameCNMismatch = this.AllowHostNameCNMismatch,
                AllowSelfSignedServerCert = this.AllowSelfSignedServerCert,
                TrustedCertPath = this.TrustedCertPath,
                UseSystemTrustStore = this.UseSystemTrustStore,
                ServerType = this.ServerType
            };
            return new ClientSession(properties, this.Auth);
        }
    }
}
