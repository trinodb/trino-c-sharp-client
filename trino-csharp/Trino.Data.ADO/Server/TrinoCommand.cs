using Trino.Client;
using Trino.Client.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Trino.Data.ADO.Client;
using Trino.Data.ADO.Utilities;

namespace Trino.Data.ADO.Server
{
    /// <summary>
    /// Represents a SQL statement.
    /// Implements the ADO.NET DbCommand for Trino-specific functionality.
    /// </summary>
    public class TrinoCommand : DbCommand
    {
        private readonly TrinoParameterCollection parameters;
        private TrinoConnection connection;

        /// <summary>
        /// Gets the cancellation token source that triggers query cancellation, including server-side cancellation.
        /// </summary>
        public CancellationTokenSource CancellationToken { get; }

        /// <summary>
        /// Gets or sets the logger instance used for command execution logging.
        /// </summary>
        public ILoggerWrapper Logger { get; set; }

        #region Constructors

        /// <summary>
	    /// Creates a new command with the specified connection.
	    /// </summary>
	    /// <param name="connection">The Trino connection to use.</param>
	    public TrinoCommand(TrinoConnection connection)
            : this(connection, string.Empty, TimeSpan.MaxValue, new CancellationTokenSource(), null)
        {
        }

        /// <summary>
        /// Creates a new command with the specified connection and logger.
        /// </summary>
        /// <param name="connection">The Trino connection to use.</param>
        /// <param name="logger">The logger instance for command execution logging.</param>
        public TrinoCommand(TrinoConnection connection, ILoggerWrapper logger)
            : this(connection, string.Empty, TimeSpan.MaxValue, new CancellationTokenSource(), logger)
        {
        }

        /// <summary>
        /// Creates a new command with the specified connection and SQL statement.
        /// </summary>
        /// <param name="connection">The Trino connection to use.</param>
        /// <param name="statement">The SQL statement to execute.</param>
        public TrinoCommand(TrinoConnection connection, string statement)
            : this(connection, statement, TimeSpan.MaxValue, new CancellationTokenSource(), null)
        {
        }

        /// <summary>
        /// Creates a new command with full configuration options for connection, statement, timeout, and cancellation.
        /// </summary>
        /// <param name="connection">The Trino connection to use.</param>
        /// <param name="statement">The SQL statement to execute.</param>
        /// <param name="timeout">The time to wait for command execution.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <param name="logger">The logger instance for command execution logging.</param>
        public TrinoCommand(
            TrinoConnection connection,
            string statement,
            TimeSpan timeout,
            CancellationTokenSource cancellationToken,
            ILoggerWrapper logger)
        {
            connection.ConnectionSession.Properties.Timeout = timeout;
            Connection = connection;
            CommandText = statement;
            CancellationToken = cancellationToken ?? new CancellationTokenSource();
            Logger = logger;
            parameters = new TrinoParameterCollection();
        }

        #endregion

        #region DbCommand Property Implementations

        /// <summary>
        /// Gets or sets the wait time in seconds before terminating the attempt to execute a command and generating an error.
        /// </summary>
        public override int CommandTimeout
        {
            get => connection.ConnectionSession.Properties.Timeout.HasValue
                ? (int)connection.ConnectionSession.Properties.Timeout.Value.TotalSeconds
                : int.MaxValue;
            set => connection.ConnectionSession.Properties.Timeout = TimeSpan.FromSeconds(value);
        }

        /// <summary>
        /// Gets or sets the Trino connection used by this command.
        /// </summary>
        protected override DbConnection DbConnection
        {
            get => connection;
            set => connection = value as TrinoConnection
                ?? throw new ArgumentException("Invalid connection type. Expecting TrinoConnection.");
        }

        /// <summary>
        /// Gets the collection of parameters associated with the command.
        /// </summary>
        protected override DbParameterCollection DbParameterCollection => parameters;

        /// <summary>
        /// Gets or sets the SQL statement or stored procedure to execute.
        /// </summary>
        public override string CommandText { get; set; }

        /// <summary>
        /// Gets or sets how the CommandText property is interpreted.
        /// </summary>
        public override CommandType CommandType { get; set; }

        /// <summary>
        /// Transactions are not supported in Trino.
        /// </summary>
        protected override DbTransaction DbTransaction
        {
            get => throw new NotSupportedException("Transactions are not supported in Trino.");
            set => throw new NotSupportedException("Transactions are not supported in Trino.");
        }

        public override bool DesignTimeVisible { get; set; }

        public override UpdateRowSource UpdatedRowSource
        {
            get => throw new NotSupportedException("UpdateRowSource is not supported in Trino.");
            set => throw new NotSupportedException("UpdateRowSource is not supported in Trino.");
        }

        #endregion

        #region Command Execution Methods

        /// <summary>
        /// Executes a SQL statement against the connection and returns the number of rows affected.
        /// </summary>
        /// <returns>The number of rows affected.</returns>
        public override int ExecuteNonQuery()
        {
            var result = RunNonQuery().SafeResult();
            var stats = result.Records.ReadToEnd().SafeResult().stats;
            return (int)stats.processedRows;
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set.
        /// </summary>
        /// <returns>The first column of the first row in the result set, or null if the result set is empty.</returns>
        public override object ExecuteScalar()
        {
            var records = RunQuery().SafeResult().Records;

            if (!records.MoveNext())
            {
                return null;
            }

            CancellationToken.Cancel();
            return records.Columns.Count > 0 ? records.GetValue<object>(0) : null;
        }

        /// <summary>
        /// Executes the command text against the connection and returns a data reader.
        /// </summary>
        /// <param name="bufferSizeBytes">The size of the buffer for reading data.</param>
        /// <returns>A data reader containing the results.</returns>
        public IDataReader ExecuteReader(long bufferSizeBytes)
        {
            var executor = RunQuery(bufferSizeBytes).SafeResult();
            return new TrinoDataReader(executor);
        }

        /// <summary>
        /// Executes a query asynchronously and returns a RecordExecutor.
        /// </summary>
        /// <param name="bufferSizeBytes">Optional buffer size in bytes. Defaults to system-defined value.</param>
        /// <returns>A task representing the asynchronous operation that returns a RecordExecutor.</returns>
        public async Task<RecordExecutor> RunQuery(long bufferSizeBytes = Constants.DefaultBufferSizeBytes)
        {
            return await RecordExecutor.Execute(
                logger: Logger,
                queryStatusNotifications: connection.InfoMessage,
                session: connection.ConnectionSession,
                statement: CommandText,
                queryParameters: ConvertParameters(Parameters),
                bufferSize: bufferSizeBytes,
                isQuery: true,
                cancellationToken: CancellationToken.Token).ConfigureAwait(false);
        }

        /// <summary>
	    /// Async version of ExecuteReader
	    /// </summary>
	    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            RecordExecutor queryExecutor;
            switch (behavior)
            {
                case CommandBehavior.Default:
                // Single result means only run one query. Trino only supports one query.
                case CommandBehavior.SingleResult:
                    queryExecutor = await RunQuery().ConfigureAwait(false);
                    break;
                case CommandBehavior.SingleRow:
                    // Single row requires the reader to be created and the first row to be read.
                    queryExecutor = await RunQuery().ConfigureAwait(false);
                    return new TrinoDataReader(queryExecutor);
                case CommandBehavior.SchemaOnly:
                    queryExecutor = await RunNonQuery().ConfigureAwait(false);
                    break;
                case CommandBehavior.CloseConnection:
                    // Trino has no concept of a connection because every call is a new connection.
                    queryExecutor = await RunQuery().ConfigureAwait(false);
                    break;
                default:
                    throw new NotSupportedException();
            }

            // always wait for the schema when creating an IEnumerable
            return new TrinoDataReader(queryExecutor);
        }

        /// <summary>
        /// Executes the command text against the connection and returns a data reader.
        /// </summary>
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return ExecuteDbDataReaderAsync(behavior, CancellationToken.Token).SafeResult();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Executes a query without returning results.
        /// </summary>
        private async Task<RecordExecutor> RunNonQuery()
        {
            return await RecordExecutor.Execute(
                logger: Logger,
                queryStatusNotifications: connection.InfoMessage,
                session: connection.ConnectionSession,
                statement: CommandText,
                queryParameters: ConvertParameters(Parameters),
                bufferSize: Constants.DefaultBufferSizeBytes,
                isQuery: false,
                cancellationToken: CancellationToken.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Converts ADO.NET parameters to Trino query parameters.
        /// </summary>
        private static IEnumerable<QueryParameter> ConvertParameters(IDataParameterCollection parameters)
        {
            foreach (IDataParameter parameter in parameters)
            {
                yield return new QueryParameter(parameter.Value);
            }
        }

        /// <summary>
        /// Cancels the execution of the command.
        /// </summary>
        public override void Cancel()
        {
            CancellationToken.Cancel();
        }

        /// <summary>
        /// Creates and returns a new parameter object.
        /// </summary>
        /// <returns>A new DbParameter object.</returns>
        protected override DbParameter CreateDbParameter()
        {
            var parameter = new TrinoParameter();
            parameters.Add(parameter);
            return parameter;
        }

        /// <summary>
        /// Prepare is not supported in Trino.
        /// </summary>
        public override void Prepare()
        {
            throw new NotSupportedException("Prepare is not supported in Trino queries. Please do not call prepare.");
        }

        #endregion
    }
}