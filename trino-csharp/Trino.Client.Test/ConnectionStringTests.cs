using Trino.Client.Auth;
using Trino.Data.ADO.Server;

namespace Trino.Client.Test
{
    [TestClass]
    public class ConnectionStringTests
    {
        [TestMethod]
        public void AllProperties()
        {
            const string host = "host.trino.com";
            const string port = "8080";
            const string enableSsl = "false";
            const string user = "test";
            const string source = "source";
            const string catalog = "catalog";
            const string schema = "schema";
            const string path = "path";
            const string timezone = "UTC";
            const string locale = "test";
            const string clientinfo = "test";
            const string clienttags = "one,two,three";
            const string clientrequesttimeout = "234324";
            const string compressionDisabled = "True";
            const string transactionid = "value";
            const string tracetoken = "tracetoken";
            const string extracredentials = "key:value,key2:value2";
            const string properties = "query_cache_enabled:false,min_hash_partition_count_for_write:100";
            const string resourceestimates = "test:value,test2:value2";
            const string preparedstatements = "test:test";

            const string scheme = "http";

            string connectionString =
                $"host={host};port={port};enableSsl={enableSsl};user={user};source={source};catalog={catalog};schema={schema};path={path};timezone={timezone};locale={locale};clientinfo={clientinfo};clienttags={clienttags};clientrequesttimeout={clientrequesttimeout};compressionDisabled={compressionDisabled};transactionid={transactionid};tracetoken={tracetoken};extracredentials={extracredentials};properties={properties};resourceestimates={resourceestimates};preparedstatements={preparedstatements}";

            TrinoConnection connection = new TrinoConnection();
            connection.ConnectionString = connectionString;
            Assert.AreEqual(connection.ConnectionSession.Properties.Server.Host, host);
            Assert.AreEqual(connection.ConnectionSession.Properties.Server.Port.ToString(), port);
            Assert.AreEqual(connection.ConnectionSession.Properties.Server.Scheme, scheme);
            Assert.AreEqual(connection.ConnectionSession.Properties.User, user);
            Assert.AreEqual(connection.ConnectionSession.Properties.Source, source);
            Assert.AreEqual(connection.ConnectionSession.Properties.Catalog, catalog);
            Assert.AreEqual(connection.ConnectionSession.Properties.Schema, schema);
            Assert.AreEqual(connection.ConnectionSession.Properties.Path, path);
            Assert.AreEqual(connection.ConnectionSession.Properties.TimeZone, timezone);
            Assert.AreEqual(connection.ConnectionSession.Properties.Locale, locale);
            Assert.AreEqual(connection.ConnectionSession.Properties.ClientInfo, clientinfo);
            CollectionAssert.AreEqual(connection.ConnectionSession.Properties.ClientTags.ToArray(), clienttags.Split(','));
            Assert.IsNotNull(connection.ConnectionSession.Properties.ClientRequestTimeout);
            Assert.AreEqual((int)connection.ConnectionSession.Properties.ClientRequestTimeout.Value.TotalSeconds, int.Parse(clientrequesttimeout));
            Assert.AreEqual(connection.ConnectionSession.Properties.CompressionDisabled, true);
            Assert.AreEqual(connection.ConnectionSession.Properties.TransactionId, transactionid);
            Assert.AreEqual(connection.ConnectionSession.Properties.TraceToken, tracetoken);
            CollectionAssert.AreEqual(connection.ConnectionSession.Properties.ExtraCredentials, extracredentials.Split(',').Select(x => x.Split(':')).ToDictionary(x => x[0], x => x[1]));
            CollectionAssert.AreEqual(connection.ConnectionSession.Properties.Properties, properties.Split(',').Select(x => x.Split(':')).ToDictionary(x => x[0], x => x[1]));
            CollectionAssert.AreEqual(connection.ConnectionSession.Properties.ResourceEstimates, resourceestimates.Split(',').Select(x => x.Split(':')).ToDictionary(x => x[0], x => x[1]));
            CollectionAssert.AreEqual(connection.ConnectionSession.Properties.PreparedStatements, preparedstatements.Split(',').Select(x => x.Split(':')).ToDictionary(x => x[0], x => x[1]));

            // verify the connection string can be regenerated
            Assert.IsNotNull(connection.ConnectionString);
        }

        [TestMethod]
        public void JWTAuth()
        {
            const string host = "host.trino.com";
            const string auth = "TrinoJWTAuth";
            const string token = "TESTTOKEN";
            const string defaultPort = "443";
            const string scheme = "https";

            string connectionString =
                $"host={host};auth={auth};AccessToken={token}";

            TrinoConnection connection = new TrinoConnection();
            connection.ConnectionString = connectionString;
            Assert.AreEqual(connection.ConnectionSession.Properties.Server.Host, host);
            Assert.AreEqual(connection.ConnectionSession.Properties.Server.Port.ToString(), defaultPort);
            Assert.AreEqual(connection.ConnectionSession.Properties.Server.Scheme, scheme);
            Assert.AreEqual(connection.ConnectionSession.Auth.GetType().Name, auth);
            Assert.AreEqual(((TrinoJWTAuth)connection.ConnectionSession.Auth).AccessToken, token);

            // verify the connection string can be regenerated
            Assert.IsNotNull(connection.ConnectionString);
        }

        [TestMethod]
        public void ClientSecretAuth()
        {
            const string host = "host.trino.com";
            const string auth = "TrinoOauthCLIENTSecretAuth";
            const string clientid = "clientId";
            const string clientsecret = "clientSECret";
            const string scope = "54dfb96b-fb36-4b76-b65b-083ed2b0b058";
            const string defaultPort = "443";
            const string scheme = "https";
            const string tokenEndpoint = "https://login.microsoftonline.com/0a1a60c6-7a15-43ae-ae41-aee8f3fe68fd/oauth2/v2.0/authorize";

            string connectionString =
                $"host={host};auth={auth};ClientId={clientid};ClientSecret={clientsecret};SCOpe={scope};tokenendpoint={tokenEndpoint}";

            TrinoConnection connection = new TrinoConnection();
            connection.AdditionalAuthProvidersForConnectionString = new List<Type>() { typeof(TrinoOauthClientSecretAuth) };
            connection.ConnectionString = connectionString;
            Assert.AreEqual(connection.ConnectionSession.Properties.Server.Host, host);
            Assert.AreEqual(connection.ConnectionSession.Properties.Server.Port.ToString(), defaultPort);
            Assert.AreEqual(connection.ConnectionSession.Properties.Server.Scheme, scheme);
            Assert.AreEqual(connection.ConnectionSession.Auth.GetType().Name, auth, StringComparer.InvariantCultureIgnoreCase);
            Assert.AreEqual(((TrinoOauthClientSecretAuth)connection.ConnectionSession.Auth).ClientId, clientid);
            Assert.AreEqual(((TrinoOauthClientSecretAuth)connection.ConnectionSession.Auth).TokenEndpoint, tokenEndpoint);
            Assert.AreEqual(((TrinoOauthClientSecretAuth)connection.ConnectionSession.Auth).Scope, scope);
            // client secret is not exposed

            // verify the connection string can be regenerated
            Assert.IsNotNull(connection.ConnectionString);
        }

        [TestMethod]
        public void LDAPAuth()
        {
            const string host = "host.trino.com";
            const string auth = "LDAPAuth";
            const string user = "user";
            const string password = "password";
            const string defaultPort = "443";
            const string scheme = "https";

            string connectionString =
                $"host={host};auth={auth};user={user};password={password}";

            TrinoConnection connection = new TrinoConnection();
            connection.ConnectionString = connectionString;
            Assert.AreEqual(connection.ConnectionSession.Properties.Server.Host, host);
            Assert.AreEqual(connection.ConnectionSession.Properties.Server.Port.ToString(), defaultPort);
            Assert.AreEqual(connection.ConnectionSession.Properties.Server.Scheme, scheme);
            Assert.AreEqual(connection.ConnectionSession.Auth.GetType().Name, auth, StringComparer.InvariantCultureIgnoreCase);
            Assert.AreEqual(((LDAPAuth)connection.ConnectionSession.Auth).User, user);
            Assert.AreEqual(((LDAPAuth)connection.ConnectionSession.Auth).Password, password);

            // verify the connection string can be regenerated
            Assert.IsNotNull(connection.ConnectionString);
        }
    }
}
