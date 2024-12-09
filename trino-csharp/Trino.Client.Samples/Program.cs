using Microsoft.Identity.Client;

using System;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Trino.Client.Auth;
using Trino.Client.Logging;
using Trino.Client.Utils;
using Trino.Data.ADO.Client;
using Trino.Data.ADO.Server;

namespace Trino.Client.Samples
{
    /// <summary>
    /// Demonstrates various ways to interact with Trino using both ADO.NET and direct client approaches.
    /// </summary>
    public class Program
    {
        private static int _queryCount;
        private static int _activeThreadCount;
        private static readonly ConcurrentBag<double> _queryDurations = new();

        /// <summary>
        /// Entry point that demonstrates various Trino interaction patterns.
        /// </summary>
        public static async Task Main()
        {
            ILoggerWrapper logger = null;
            ITrinoAuth auth = null;
            var serverUri = new Uri("http://localhost:8060/");

            SimpleReadADO(logger, auth, serverUri);
            await SimpleRead(logger, auth, serverUri);
            await SimpleReadWithDataTable(logger, auth, serverUri);
            RunParallelQueries(logger, auth, serverUri);
            DoLargeRead(logger, auth, serverUri);
            SessionExample(logger, auth, serverUri);
            GetInformationSchema(logger, auth, serverUri);
            GetSpecificInformationSchema(logger, auth, serverUri);
        }

        /// <summary>
        /// Demonstrates a simple table read using ADO.NET with Trino.
        /// </summary>
        /// <param name="logger">Logger instance for tracking operations.</param>
        /// <param name="auth">Authentication provider for Trino.</param>
        /// <param name="serverUri">URI of the Trino server.</param>
        private static void SimpleReadADO(ILoggerWrapper logger, ITrinoAuth auth, Uri serverUri)
        {
            var properties = new TrinoConnectionProperties
            {
                Catalog = "tpch",
                Server = serverUri,
                Auth = auth
            };

            // Example of OAuth configuration - typically should be moved to configuration
            _ = new TrinoOauthClientSecretAuth(
                tokenEndpoint: "login.microsoftonline.com",
                clientId: "guid",
                clientSecret: "secret",
                scope: "https://myscope/");

            using var connection = new TrinoConnection(properties);
            using var command = new TrinoCommand(connection, "SELECT * FROM tpch.tiny.customer LIMIT 5");
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? "null" : reader.GetValue(i).ToString();
                    Console.WriteLine($"{reader.GetName(i)} -> {reader.GetDataTypeName(i)} : {value}");
                }
            }
        }

        /// <summary>
        /// Demonstrates querying Trino without using ADO.NET.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task<DataTable> SimpleRead(ILoggerWrapper logger, ITrinoAuth auth, Uri serverUri)
        {
            var session = new ClientSession(
                sessionProperties: new ClientSessionProperties { Server = serverUri },
                auth: auth);

            var records = await RecordExecutor.Execute(
                session: session,
                statement: "SELECT * FROM tpch.tiny.customer")
                .ConfigureAwait(false);

            foreach (var row in records)
            {
                Console.WriteLine(string.Join(",", row));
            }

            return null;
        }

        /// <summary>
        /// Demonstrates reading data into a DataTable without using ADO.NET.
        /// </summary>
        /// <returns>DataTable containing the query results.</returns>
        public static async Task<DataTable> SimpleReadWithDataTable(ILoggerWrapper logger, ITrinoAuth auth, Uri serverUri)
        {
            var session = new ClientSession(serverUri, auth);
            var executor = await RecordExecutor.Execute(
                session: session,
                statement: "SELECT * FROM tpch.tiny.customer")
                .ConfigureAwait(false);

            var dataTable = await executor.BuildDataTableAsync().ConfigureAwait(false);
            Console.WriteLine($"Read {dataTable.Rows.Count} rows");
            return dataTable;
        }

        /// <summary>
        /// Demonstrates running parallel queries against a Trino cluster.
        /// </summary>
        private static void RunParallelQueries(ILoggerWrapper logger, ITrinoAuth auth, Uri serverUri)
        {
            const int ThreadCount = 10; // Typically handles up to 200 connections on normal clusters
            const int RunDurationSeconds = 15;

            var startTime = DateTime.UtcNow;
            Console.WriteLine("Authorizing...");
            Console.WriteLine("Ready to start");

            ThreadPool.SetMinThreads(ThreadCount + 20, ThreadCount + 20);
            var endTime = DateTime.UtcNow.AddSeconds(RunDurationSeconds);
            var tasks = new Task[ThreadCount];

            for (int i = 0; i < ThreadCount; i++)
            {
                Console.WriteLine($"Starting thread {i}");
                tasks[i] = Task.Run(async () =>
                {
                    var properties = new TrinoConnectionProperties
                    {
                        Catalog = "tpch",
                        Server = serverUri,
                        Auth = auth,
                        TestConnection = false
                    };

                    while (DateTime.UtcNow < endTime)
                    {
                        await TestClientThroughput(properties, logger).ConfigureAwait(false);
                    }
                });
            }

            Task.WaitAll(tasks);
            PrintResultsOfThreadedExecution(startTime, ThreadCount);
        }

        /// <summary>
        /// Prints statistical information about the executed queries.
        /// </summary>
        private static void PrintResultsOfThreadedExecution(DateTime startTime, int threadCount)
        {
            var percentiles = new[] { 5, 50, 75, 90, 95, 99, 100 };
            var sortedDurations = _queryDurations.OrderBy(d => d).ToList();

            Console.WriteLine("Statistics:");
            Console.WriteLine($"Total Queries: {_queryCount}");
            Console.WriteLine($"Start Time: {startTime}");
            Console.WriteLine($"End Time: {DateTime.UtcNow}");
            Console.WriteLine($"Total Threads: {threadCount}");

            foreach (var percentile in percentiles)
            {
                var index = Math.Min((int)(percentile / 100.0 * sortedDurations.Count), sortedDurations.Count - 1);
                Console.WriteLine($"{percentile}th percentile: {sortedDurations[index]:F2}s");
            }
        }

        /// <summary>
        /// Executes a single query and measures its duration.
        /// </summary>
        private static async Task<bool> TestClientThroughput(TrinoConnectionProperties properties, ILoggerWrapper logger)
        {
            const string Query = @"
                SELECT sum(acctbal), avg(acctbal), nationkey, mktsegment 
                FROM tpch.tiny.customer 
                WHERE mktsegment IN ('AUTOMOBILE', 'BUILDING') 
                GROUP BY nationkey, mktsegment";

            Interlocked.Increment(ref _activeThreadCount);

            using var connection = new TrinoConnection(properties);
            connection.Open();

            var stopwatch = Stopwatch.StartNew();
            using var command = new TrinoCommand(connection, Query, TimeSpan.MaxValue, null, logger);
            using var reader = (TrinoDataReader)await command.ExecuteReaderAsync().ConfigureAwait(false);

            var rowCount = 0;
            while (reader.Read())
            {
                rowCount++;
            }

            _queryDurations.Add(stopwatch.Elapsed.TotalSeconds);
            Interlocked.Decrement(ref _activeThreadCount);
            Interlocked.Increment(ref _queryCount);

            return true;
        }

        /// <summary>
        /// Demonstrates reading a large dataset. Use sf100 for production clusters.
        /// </summary>
        private static void DoLargeRead(ILoggerWrapper logger, ITrinoAuth auth, Uri serverUri)
        {
            var properties = new TrinoConnectionProperties
            {
                Catalog = "tpch",
                Server = serverUri,
                Auth = auth
            };

            Console.WriteLine("Starting large read...");
            var stopwatch = Stopwatch.StartNew();

            using var connection = new TrinoConnection(properties);
            using var command = new TrinoCommand(connection, "SELECT * FROM tpch.sf1.customer")
            {
                Logger = logger
            };

            using var reader = command.ExecuteReader(CommandBehavior.CloseConnection);
            Console.WriteLine("Reading data...");

            var rowCount = 0;
            while (reader.Read())
            {
                rowCount++;
                if (rowCount % 100000 == 0)
                {
                    Console.WriteLine($"Read {rowCount:N0} rows");
                }
            }

            Console.WriteLine($"Completed in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        }

        /// <summary>
        /// Demonstrates how to set and use session properties.
        /// </summary>
        public static void SessionExample(ILoggerWrapper logger, ITrinoAuth auth, Uri serverUri)
        {
            var properties = new TrinoConnectionProperties
            {
                Catalog = "tpch",
                Server = serverUri,
                Auth = auth,
                Source = "Pythagoras"
            };

            using var connection = new TrinoConnection(properties);

            // Set session properties
            using (var command = new TrinoCommand(connection, "SET SESSION task_concurrency=32"))
            {
                command.Logger = logger;
                command.ExecuteNonQuery();
            }

            UserAssertion.Equals(connection.ConnectionSession.Properties.Source, properties.Source);
            connection.ConnectionSession.Properties.Source = "Archimedes";

            // Set schema and other properties
            using (var command = new TrinoCommand(connection, "USE tpch.sf10"))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new TrinoCommand(connection, "SET SESSION hive.insert_existing_partitions_behavior = 'OVERWRITE'"))
            {
                command.ExecuteNonQuery();
            }

            // Display session information
            Console.WriteLine($"Catalog: {connection.ConnectionSession.Properties.Catalog}");
            Console.WriteLine($"Schema: {connection.ConnectionSession.Properties.Schema}");
            Console.WriteLine($"Properties: {string.Join(", ", connection.ConnectionSession.Properties.Properties.Keys)}");
            Console.WriteLine($"Source: {connection.ConnectionSession.Properties.Source}");
        }

        /// <summary>
        /// Retrieves and displays general information schema details using ADO.NET.
        /// </summary>
        private static void GetInformationSchema(ILoggerWrapper logger, ITrinoAuth auth, Uri serverUri)
        {
            var properties = new TrinoConnectionProperties
            {
                Catalog = "tpcds",
                Schema = "tiny",
                Server = serverUri,
                Auth = auth
            };

            using var connection = new TrinoConnection(properties);
            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("Table Schemas:");
            PrintTable(connection.GetSchema());

            Console.WriteLine("\nColumns:");
            PrintTable(connection.GetSchema("columns"));

            Console.WriteLine("\nTables:");
            PrintTable(connection.GetSchema("tables"));

            Console.WriteLine("\nViews:");
            PrintTable(connection.GetSchema("views"));

            Console.WriteLine($"Metadata query duration: {stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// Retrieves and displays specific information schema details using ADO.NET.
        /// </summary>
        private static void GetSpecificInformationSchema(ILoggerWrapper logger, ITrinoAuth auth, Uri serverUri)
        {
            var properties = new TrinoConnectionProperties
            {
                Catalog = "tpcds",
                Schema = "tiny",
                Server = serverUri,
                Auth = auth
            };

            using var connection = new TrinoConnection(properties);
            var stopwatch = Stopwatch.StartNew();

            var schemas = connection.GetSchema();
            Console.WriteLine("Table Schemas:");
            PrintTable(schemas);

            foreach (DataRow row in schemas.Rows)
            {
                foreach (DataColumn column in schemas.Columns)
                {
                    Console.WriteLine(new string('-', 60));
                    var schemaName = row[column].ToString().ToUpperInvariant();
                    Console.WriteLine(schemaName);
                    PrintTable(connection.GetSchema(row[column].ToString()));
                }
            }

            Console.WriteLine($"Metadata query duration: {stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// Prints the contents of a DataTable in a formatted manner.
        /// </summary>
        /// <param name="table">The DataTable to print.</param>
        private static void PrintTable(DataTable table)
        {
            foreach (DataColumn column in table.Columns)
            {
                Console.Write($"{column.ColumnName}\t");
            }
            Console.WriteLine();

            foreach (DataRow row in table.Rows)
            {
                foreach (DataColumn column in table.Columns)
                {
                    Console.Write($"{row[column]}\t");
                }
                Console.WriteLine();
            }
        }
    }
}