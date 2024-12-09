using Trino.Client;
using Trino.Client.Auth;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Trino.Data.ADO.Utilities
{
    public class ConnectionStringUtils
    {
        public static string ServerTypeProperty => "ServerType";
        public static string CatalogProperty => "Catalog";
        public static string ClientInfoProperty => "ClientInfo";
        public static string ClientRequestTimeoutProperty => "ClientRequestTimeout";
        public static string ClientTagsProperty => "ClientTags";
        public static string CompressionDisabledProperty => "CompressionDisabled";
        public static string ExtraCredentialsProperty => "ExtraCredentials";
        public static string LocaleProperty => "Locale";
        public static string PathProperty => "Path";
        public static string PreparedStatementsProperty => "PreparedStatements";
        public static string PropertiesProperty => "Properties";
        public static string ResourceEstimatesProperty => "ResourceEstimates";
        public static string SchemaProperty => "Schema";
        public static string SourceProperty => "Source";
        public static string TestConnectionProperty => "TestConnection";
        public static string TimezoneProperty => "Timezone";
        public static string TraceTokenProperty => "TraceToken";
        public static string TransactionIdProperty => "TransactionId";
        public static string UserProperty => "User";

        public static string HostProperty => "Host";
        public static string PortProperty => "Port";
        public static string EnableSSLProperty => "EnableSSL";

        public static string AuthenticationTypeProperty => "Auth";

        public static string AllowHostNameCNMismatchProperty => "AllowHostNameCNMismatch";
        public static string AllowSelfSignedServerCertProperty => "AllowSelfSignedServerCert";
        public static string TrustedCertPathProperty => "TrustedCertPath";
        public static string TrustedCertificateProperty => "TrustedCertificate";
        public static string UseSystemTrustStoreProperty => "UseSystemTrustStore";

        // handles session key value pair strings
        private static readonly Regex keyValuePairs = new Regex(@"^[0-9A-z_\.]+:[0-9A-z_\.]+(,[0-9A-z_\.]+:[0-9A-z_\.]+)*,?$");

        private static readonly Dictionary<string, PropertyHandler> propertyHandlers = new Dictionary<string, PropertyHandler>(StringComparer.InvariantCultureIgnoreCase)
        {
            { UserProperty, new PropertyHandler((session) => session.User, (session, value) => session.User = value) },
            { TestConnectionProperty, new PropertyHandler((session) => session.TestConnection.ToString(), (session, value) => session.TestConnection = bool.Parse(value)) },
            { SourceProperty, new PropertyHandler((session) => session.Source, (session, value) => session.Source = value) },
            { CatalogProperty, new PropertyHandler((session) => session.Catalog, (session, value) => session.Catalog = value) },
            { SchemaProperty, new PropertyHandler((session) => session.Schema, (session, value) => session.Schema = value) },
            { PathProperty, new PropertyHandler((session) => session.Path, (session, value) => session.Path = value) },
            { TimezoneProperty, new PropertyHandler((session) => session.TimeZone, (session, value) => session.TimeZone = value) },
            { LocaleProperty, new PropertyHandler((session) => session.Locale, (session, value) => session.Locale = value) },
            { ClientInfoProperty, new PropertyHandler((session) => session.ClientInfo, (session, value) => session.ClientInfo = value) },
            { ClientTagsProperty, new PropertyHandler((session) => string.Join(",", session.ClientTags), (session, value) => session.ClientTags = new HashSet<string>(value.Split(','))) },
            { ClientRequestTimeoutProperty, new PropertyHandler((session) => session.ClientRequestTimeout.HasValue ? session.ClientRequestTimeout.Value.TotalSeconds.ToString() : null, (session, value) => session.ClientRequestTimeout = TimeSpan.FromSeconds(double.Parse(value))) },
            { CompressionDisabledProperty, new PropertyHandler((session) => (session.CompressionDisabled).ToString(), (session, value) => session.CompressionDisabled = bool.Parse(value)) },
            { TransactionIdProperty, new PropertyHandler((session) => session.TransactionId, (session, value) => session.TransactionId = value) },
            { TraceTokenProperty, new PropertyHandler((session) => session.TraceToken, (session, value) => session.TraceToken = value) },
            { ExtraCredentialsProperty, new PropertyHandler((session) => SerializeProperties(session.ExtraCredentials), (session, value) => session.ExtraCredentials = ParseProperties("extracredentials", value)) },
            { PropertiesProperty, new PropertyHandler((session) => SerializeProperties(session.Properties), (session, value) => session.Properties = ParseProperties("properties", value)) },
            { ResourceEstimatesProperty, new PropertyHandler((session) => SerializeProperties(session.ResourceEstimates), (session, value) => session.ResourceEstimates = ParseProperties("resourceestimates", value)) },
            { PreparedStatementsProperty, new PropertyHandler((session) => SerializeProperties(session.PreparedStatements), (session, value) => session.PreparedStatements = ParseProperties("preparedstatements", value)) },
            { AllowHostNameCNMismatchProperty, new PropertyHandler((session) => session.AllowHostNameCNMismatch.ToString(), (session, value) => session.AllowHostNameCNMismatch = bool.Parse(value)) },
            { AllowSelfSignedServerCertProperty, new PropertyHandler((session) => session.AllowSelfSignedServerCert.ToString(), (session, value) => session.AllowSelfSignedServerCert = bool.Parse(value)) },
            { TrustedCertPathProperty, new PropertyHandler((session) => session.TrustedCertPath, (session, value) => session.TrustedCertPath = value) },
            { TrustedCertificateProperty, new PropertyHandler((session) => session.TrustedCertificate, (session, value) => session.TrustedCertificate = value) },
            { UseSystemTrustStoreProperty, new PropertyHandler((session) => session.UseSystemTrustStore.ToString(), (session, value) => session.UseSystemTrustStore = bool.Parse(value)) },
            { ServerTypeProperty, new PropertyHandler((session) => session.ServerType.ToString(), (session, value) => session.ServerType = value) }
        };

        /// <summary>
        /// Creates session properties from a connection string. Most properties are handled based on their name, but some require custom handling.
        /// Auth and server URI require custom handling.
        /// </summary>
        /// <param name="additionalAuthProviders">List of authentication providers to use in addition to integrated JWT auth.</param>
        public static ClientSession UpdateSessionFromConnectionString(ClientSession session, string connectionString, IEnumerable<Type> additionalAuthProviders)
        {
            DbConnectionStringBuilder connectionStringBuilder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };
            Dictionary<string, string> connectionStringProperties = connectionStringBuilder.Cast<KeyValuePair<string, object>>().ToDictionary(k => k.Key, v => v.Value.ToString(), StringComparer.InvariantCultureIgnoreCase);

            int port = ClientSessionProperties.DefaultPort;
            ISet<string> propertiesWithCustomHandling =
                new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) { HostProperty, PortProperty, EnableSSLProperty, AuthenticationTypeProperty };

            connectionStringProperties.TryGetValue(AuthenticationTypeProperty, out string auth);

            if (!connectionStringProperties.TryGetValue(HostProperty, out string host))
            {
                throw new ArgumentNullException($"{HostProperty} must be provided in the connection string.");
            }

            bool enableSsl = true;
            if (connectionStringProperties.TryGetValue(EnableSSLProperty, out string enableSSLValue))
            {
                if (!bool.TryParse(enableSSLValue, out bool result))
                {
                    throw new ArgumentException($"{EnableSSLProperty} connection string property must be an boolean.");
                }
                enableSsl = result;
            }

            if (connectionStringProperties.TryGetValue(PortProperty, out string portValue))
            {
                if (!int.TryParse(portValue, out int result))
                {
                    throw new ArgumentException($"{PortProperty} connection string property must be an integer.");
                }
                port = result;
            }

            // Attempt to locate authentication and assign to session
            ISet<string> propertiesConsumedByAuth = string.IsNullOrEmpty(auth) ? new HashSet<string>()
                : CreateAuth(session, connectionStringProperties, auth, additionalAuthProviders);

            // Hostname is required
            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentNullException($"{HostProperty} cannot be empty in the Trino connection string.");
            }

            session.Properties.Server = ClientSessionProperties.GetServerUri(host, enableSsl, port);

            // Apply the remaining properies
            foreach (KeyValuePair<string, object> param in connectionStringBuilder)
            {
                bool customHandled = propertiesWithCustomHandling.Contains(param.Key);
                bool consumedByAuth = propertiesConsumedByAuth.Contains(param.Key);
                bool sessionSet = TrySetSessionProperty(session.Properties, param.Key, param.Value);

                if (!customHandled && !consumedByAuth && !sessionSet)
                {
                    throw new ArgumentException($"Unrecognized connection string parameter: {param.Key}");
                }
            }

            return session;
        }

        /// <summary>
        /// Converts a session to a connection string.
        /// </summary>
        public static string GetConnectionString(ClientSession session)
        {
            DbConnectionStringBuilder connectionString = new DbConnectionStringBuilder();
            foreach (KeyValuePair<string, PropertyHandler> property in propertyHandlers)
            {
                string propertyValue = property.Value.Serializer.Invoke(session.Properties);
                if (!string.IsNullOrEmpty(propertyValue))
                {
                    connectionString.Add(property.Key, property.Value.Serializer.Invoke(session.Properties));
                }
            }

            // add authentication type
            if (session.Auth != null)
            {
                Type authType = session.Auth.GetType();
                connectionString.Add(AuthenticationTypeProperty, session.Auth.GetType().Name);

                // get authentication properties
                foreach (PropertyInfo property in authType.GetProperties())
                {
                    MethodInfo method = property.GetGetMethod(true);
                    // if property is not private
                    if (method != null && !method.IsPrivate)
                    {
                        object propertyValue = property.GetValue(session.Auth);
                        if (propertyValue != null)
                        {
                            connectionString.Add(property.Name, property.GetValue(session.Auth).ToString());
                        }
                    }
                }
            }

            return connectionString.ToString();
        }

        /// <summary>
        /// Assign a session property based on a key/value pair.
        /// </summary>
        private static bool TrySetSessionProperty(ClientSessionProperties sessionProperties, string key, object value)
        {
            if (propertyHandlers.ContainsKey(key.ToLower()))
            {
                propertyHandlers[key.ToLower()].Deserializer.Invoke(sessionProperties, value.ToString());
                return true;
            }
            return false;
        }

        private static string SerializeProperties(Dictionary<string, string> properties)
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, string> property in properties)
            {
                if (string.IsNullOrEmpty(property.Value) || property.Value.Contains(";") || property.Value.Contains(","))
                {
                    throw new ArgumentException($"Session property value cannot be null, empty, contain a semicolon or a comma: {property.Value}");
                }
                if (string.IsNullOrEmpty(property.Key) || property.Key.Contains(";") || property.Key.Contains(","))
                {
                    throw new ArgumentException($"Session property value cannot be null, empty, contain a semicolon or a comma: {property.Value}");
                }
                sb.Append(property.Key);
                sb.Append(':');
                sb.Append(property.Value);
                sb.Append(',');
            }
            return sb.ToString();
        }

        /// <summary>
        /// Splits key value pairs that are comma delimetered and colon separated.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        private static Dictionary<string, string> ParseProperties(string name, string value)
        {
            if (value == null)
            {
                return new Dictionary<string, string>();
            }

            string cleanValue = value.Trim();
            if (cleanValue.Length == 0)
            {
                return new Dictionary<string, string>();
            }

            if (!keyValuePairs.IsMatch(cleanValue))
            {
                throw new FormatException($"Session property \"{name}\" contains key value pairs and should be comma delimetered and colon should separate keys and values: {value}");
            }

            // Matching Trino ODBC, which is comma-separated, colon key-value pairs
            return cleanValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).AsEnumerable()
                .Select(pair => pair.Trim().Split(':'))
                .ToDictionary(pair => pair[0], pair => pair[1]);
        }

        /// <summary>
        /// Given a list of connection string properties extracted from the connection string, return the appropriate ITrinoAuth object.
        /// </summary>
        /// <param name="connectionStringProperties">Key value pairs from the connection string.</param>
        /// <param name="authMode">The auth type, extracted from the connection string</param>
        private static ISet<string> CreateAuth(
            ClientSession session,
            Dictionary<string, string> connectionStringProperties,
            string authMode,
            IEnumerable<Type> additionalAuthProviders)
        {
            Dictionary<string, Type> authTypes = BuildAuthTypesMap(additionalAuthProviders);
            ISet<string> handledProperties = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            if (!authTypes.ContainsKey(authMode))
            {
                throw new ArgumentException($"Authentication type {authMode} not available. Current auth modes available are: {string.Join(", ", authTypes.Keys)}");
            }

            PropertyInfo[] authProperties = authTypes[authMode].GetProperties();
            // create instance of authClientTypeNames[auth]
            ITrinoAuth connectionAuthorization = (ITrinoAuth)Activator.CreateInstance(authTypes[authMode]);
            foreach (PropertyInfo prop in authProperties)
            {
                if (connectionStringProperties.ContainsKey(prop.Name))
                {
                    prop.SetValue(connectionAuthorization, connectionStringProperties[prop.Name]);
                    handledProperties.Add(prop.Name);
                }
            }

            session.Auth = connectionAuthorization;

            return handledProperties;
        }

        /// <summary>
        /// Builds a map of auth types to their respective types, because the connection string will need to initiate an auth mode.
        /// </summary>
        private static Dictionary<string, Type> BuildAuthTypesMap(IEnumerable<Type> additionalAuthProviders)
        {
            // JWT is built in, but additional auth providers can originate from other DLLs to keep dependencies minimal
            Type jwtAuth = typeof(TrinoJWTAuth);
            Type ldapAuth = typeof(LDAPAuth);
            Dictionary<string, Type> authTypes = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase) { { jwtAuth.Name, jwtAuth }, { ldapAuth.Name, ldapAuth } };

            if (additionalAuthProviders != null)
            {
                foreach (Type type in additionalAuthProviders)
                {
                    authTypes.Add(type.Name, type);
                }
            }

            return authTypes;
        }
    }
}
