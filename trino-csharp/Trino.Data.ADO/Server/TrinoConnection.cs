using Trino.Client;
using Trino.Client.Model.StatementV1;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Trino.Client.Logging;
using Trino.Client.Utils;
using Trino.Data.ADO.Utilities;

namespace Trino.Data.ADO.Server
{
    /// <summary>
    /// ADO.NET connection implementation for Trino database access.
    /// Manages connection state, session properties, and schema information retrieval.
    /// </summary>
    public class TrinoConnection : DbConnection
    {
        private ConnectionState state = ConnectionState.Closed;
        private static readonly HashSet<string> supportedSchemaCollections = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "catalogs",
            "schemas",
            "schemata",
            "tables",
            "columns",
            "views",
            "functions",
            "sessions"
        };

        /// <summary>
        /// Gets or sets the client session associated with this connection.
        /// </summary>
        public ClientSession ConnectionSession { get; set; } = new ClientSession();

        /// <summary>
        /// Additional authentication providers that can be used when creating connections from connection strings.
        /// Required because connection strings cannot directly provide authentication providers to the driver.
        /// </summary>
        public List<Type> AdditionalAuthProvidersForConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the connection string. Connection string parameters are decoded into connection properties.
        /// </summary>
        public override string ConnectionString
        {
            get => ConnectionStringUtils.GetConnectionString(ConnectionSession);
            set => ConnectionSession = ConnectionStringUtils.UpdateSessionFromConnectionString(
                ConnectionSession,
                value,
                AdditionalAuthProvidersForConnectionString);
        }

        /// <summary>
        /// Gets the connection timeout in seconds.
        /// </summary>
        public override int ConnectionTimeout => (int)Constants.HttpConnectionTimeout.TotalSeconds;

        /// <summary>
        /// Gets the current database name (equivalent to schema name in Trino).
        /// </summary>
        public override string Database => ConnectionSession.Properties.Schema;

        /// <summary>
        /// Gets or sets the logger for connection operations.
        /// </summary>
        public ILoggerWrapper Logger { get; set; }

        /// <summary>
        /// Gets the current state of the connection.
        /// </summary>
        public override ConnectionState State => state;

        /// <summary>
        /// Gets the data source (server URI) for this connection.
        /// </summary>
        public override string DataSource => ConnectionSession.Properties.Server?.ToString();

        /// <summary>
        /// Gets the version of the connected Trino server.
        /// </summary>
        public override string ServerVersion => new InfoClientV1(ConnectionSession).Get().nodeVersion.version;

        /// <summary>
        /// Event handlers for receiving query statistics and errors.
        /// </summary>
        public IList<Action<TrinoStats, TrinoError>> InfoMessage { get; private set; }

        /// <summary>
        /// Creates a new Trino connection with default settings.
        /// </summary>
        public TrinoConnection() : this(null)
        {
        }

        /// <summary>
        /// Creates a new Trino connection with the specified connection properties.
        /// </summary>
        /// <param name="connectionProperties">Connection configuration properties.</param>
        public TrinoConnection(TrinoConnectionProperties connectionProperties)
        {
            if (connectionProperties != null)
            {
                ConnectionSession = connectionProperties.GetSession();
            }

            InfoMessage = new List<Action<TrinoStats, TrinoError>>();
        }

        /// <summary>
        /// Changes the current database (schema in Trino terminology).
        /// </summary>
        /// <param name="databaseName">The name of the database to use.</param>
        public override void ChangeDatabase(string databaseName)
        {
            ConnectionSession.Properties.Schema = databaseName;
        }

        /// <summary>
        /// Closes the connection and updates its state.
        /// </summary>
        public override void Close()
        {
            state = ConnectionState.Closed;
        }

        /// <summary>
        /// Creates a new command associated with this connection.
        /// </summary>
        protected override DbCommand CreateDbCommand()
        {
            return new TrinoCommand(this, Logger);
        }

        /// <summary>
        /// Opens the connection and optionally tests the connection if configured.
        /// </summary>
        public override void Open()
        {
            if (ConnectionSession.Properties.TestConnection)
            {
                // Fastest way to test connection is to hit the v1/info resource
                if (!TestConnection())
                {
                    throw new TrinoException("Trino connection test failed.");
                }
            }

            state = ConnectionState.Open;
        }

        /// <summary>
        /// Transactions are not supported in Trino.
        /// </summary>
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            throw new NotSupportedException("Transactions are not supported in Trino.");
        }

        /// <summary>
        /// Returns information about available schema collections.
        /// </summary>
        public override DataTable GetSchema()
        {
            var schemaTable = new DataTable();
            schemaTable.Columns.Add("CollectionName", typeof(string));

            foreach (string collection in supportedSchemaCollections)
            {
                DataRow row = schemaTable.NewRow();
                row["CollectionName"] = collection;
                schemaTable.Rows.Add(row);
            }

            return schemaTable;
        }

        /// <summary>
        /// Gets schema information for the specified collection.
        /// </summary>
        public override DataTable GetSchema(string collectionName)
        {
            return GetSchema(collectionName, Array.Empty<string>());
        }

        /// <summary>
        /// Gets schema information for the specified collection with restriction values.
        /// Supports retrieving information about catalogs, schemas, tables, columns, views, functions, and sessions.
        /// </summary>
        /// <param name="collectionName">The type of schema information to retrieve.</param>
        /// <param name="restrictionValues">Optional filtering restrictions.</param>
        public override DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            switch (collectionName.ToLower())
            {
                case "catalogs":
                    return new TrinoCommand(this, "SHOW CATALOGS")
                        .RunQuery()
                        .SafeResult()
                        .BuildDataTableAsync()
                        .SafeResult();

                case "databases":
                case "schemas":
                case "schemata":
                    return SchemaUtils.GetInformationSchema(
                        this,
                        Logger,
                        "schemata",
                        SchemaUtils.BuildFilterForRestrictions(ConnectionSession, SchemaUtils.SchemaRestrictionsMapping, restrictionValues));

                case "tables":
                    return SchemaUtils.GetInformationSchema(
                        this,
                        Logger,
                        "tables",
                        SchemaUtils.BuildFilterForRestrictions(ConnectionSession, SchemaUtils.TableRestrictionsMapping, restrictionValues));

                case "columns":
                    return SchemaUtils.GetInformationSchema(
                        this,
                        Logger,
                        "columns",
                        SchemaUtils.BuildFilterForRestrictions(ConnectionSession, SchemaUtils.ColumnRestrictionsMapping, restrictionValues));

                case "views":
                    return SchemaUtils.GetInformationSchema(
                        this,
                        Logger,
                        "views",
                        SchemaUtils.BuildFilterForRestrictions(ConnectionSession, SchemaUtils.ViewRestrictionsMapping, restrictionValues));

                case "functions":
                    return new TrinoCommand(this, "SHOW FUNCTIONS")
                        .RunQuery()
                        .SafeResult()
                        .BuildDataTableAsync()
                        .SafeResult();

                case "sessions":
                    return new TrinoCommand(this, "SHOW SESSION")
                        .RunQuery()
                        .SafeResult()
                        .BuildDataTableAsync()
                        .SafeResult();

                default:
                    throw new NotSupportedException($"Collection {collectionName} is not supported.");
            }
        }

        private bool TestConnection()
        {
            return new InfoClientV1(ConnectionSession).Get().starting == false;
        }
    }
}