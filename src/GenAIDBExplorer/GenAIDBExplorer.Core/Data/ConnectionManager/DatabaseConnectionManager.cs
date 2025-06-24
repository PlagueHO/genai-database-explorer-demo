using GenAIDBExplorer.Core.Data.DatabaseProviders;
using GenAIDBExplorer.Core.SemanticModelProviders;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;
using System.Resources;

namespace GenAIDBExplorer.Core.Data.ConnectionManager
{
    /// <summary>
    /// Manages database connections using connection pooling for improved performance and scalability.
    /// </summary>
    /// <remarks>
    /// This class is responsible for managing database connections using SQL Server's built-in connection pooling.
    /// Instead of maintaining a single long-lived connection, it provides connections from the pool as needed,
    /// improving efficiency and supporting concurrent operations while respecting pool size limits.
    /// </remarks>
    public sealed class DatabaseConnectionManager(
        IDatabaseConnectionProvider connectionProvider,
        ILogger<DatabaseConnectionManager> logger
    ) : IDatabaseConnectionManager
    {
        private readonly IDatabaseConnectionProvider _connectionProvider = connectionProvider;
        private readonly ILogger<DatabaseConnectionManager> _logger = logger;
        private static readonly ResourceManager _resourceManagerErrorMessages = new("GenAIDBExplorer.Core.Resources.ErrorMessages", typeof(SchemaRepository).Assembly);
        private bool _disposed = false;
        
        // Connection pool metrics for monitoring
        private int _totalConnectionsCreated = 0;
        private int _activeConnections = 0;
        private readonly object _metricsLock = new();

        /// <summary>
        /// Gets an open SQL connection from the connection pool.
        /// </summary>
        /// <returns>An open <see cref="SqlConnection"/> instance from the connection pool.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the connection could not be opened.</exception>
        /// <remarks>
        /// This method retrieves a connection from the SQL Server connection pool using the configured
        /// connection provider. Each call returns a connection from the pool, which should be disposed
        /// when no longer needed to return it to the pool. Connection pooling parameters are managed
        /// through the database settings configuration.
        /// </remarks>
        public async Task<SqlConnection> GetOpenConnectionAsync()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DatabaseConnectionManager));
            }

            try
            {
                var connection = await _connectionProvider.ConnectAsync().ConfigureAwait(false);

                if (connection.State != ConnectionState.Open)
                {
                    connection.Dispose();
                    throw new InvalidOperationException(_resourceManagerErrorMessages.GetString("ErrorConnectingToDatabase"));
                }

                // Update connection metrics
                lock (_metricsLock)
                {
                    _totalConnectionsCreated++;
                    _activeConnections++;
                    _logger.LogDebug("Connection retrieved from pool. Total created: {TotalCreated}, Active: {Active}", 
                        _totalConnectionsCreated, _activeConnections);
                }

                // Wrap the connection to track when it's disposed
                return new TrackedSqlConnection(connection, OnConnectionDisposed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get connection from pool");
                throw;
            }
        }

        /// <summary>
        /// Called when a tracked connection is disposed to update metrics.
        /// </summary>
        private void OnConnectionDisposed()
        {
            lock (_metricsLock)
            {
                _activeConnections = Math.Max(0, _activeConnections - 1);
                _logger.LogDebug("Connection returned to pool. Active connections: {Active}", _activeConnections);
            }
        }

        /// <summary>
        /// Disposes the managed resources.
        /// </summary>
        /// <remarks>
        /// This method disposes the managed resources and logs final connection metrics.
        /// It ensures that the connection manager is properly disposed of when no longer needed.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">A boolean value indicating whether to dispose managed resources.</param>
        /// <remarks>
        /// This method disposes the managed resources and logs final connection pool metrics.
        /// Connection pool cleanup is handled by the SQL Server connection pool infrastructure.
        /// </remarks>
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Log final metrics
                    lock (_metricsLock)
                    {
                        _logger.LogInformation("DatabaseConnectionManager disposed. Final metrics - Total connections created: {TotalCreated}, Active connections: {Active}", 
                            _totalConnectionsCreated, _activeConnections);
                    }
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizer for the <see cref="DatabaseConnectionManager"/> class.
        /// </summary>
        /// <remarks>
        /// This finalizer ensures that the managed resources are properly disposed of when the
        /// <see cref="DatabaseConnectionManager"/> is garbage collected.
        /// </remarks>
        ~DatabaseConnectionManager()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// A wrapper around SqlConnection that tracks when connections are disposed for metrics.
    /// </summary>
    internal sealed class TrackedSqlConnection : SqlConnection
    {
        private readonly SqlConnection _innerConnection;
        private readonly Action _onDisposed;
        private bool _disposed = false;

        public TrackedSqlConnection(SqlConnection innerConnection, Action onDisposed)
        {
            _innerConnection = innerConnection;
            _onDisposed = onDisposed;
        }

        public override string ConnectionString 
        { 
            get => _innerConnection.ConnectionString; 
            set => _innerConnection.ConnectionString = value; 
        }

        public override int ConnectionTimeout => _innerConnection.ConnectionTimeout;

        public override string Database => _innerConnection.Database;

        public override ConnectionState State => _innerConnection.State;

        public override string DataSource => _innerConnection.DataSource;

        public override string ServerVersion => _innerConnection.ServerVersion;

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return _innerConnection.BeginTransaction(isolationLevel);
        }

        public override void ChangeDatabase(string databaseName)
        {
            _innerConnection.ChangeDatabase(databaseName);
        }

        public override void Close()
        {
            _innerConnection.Close();
        }

        protected override DbCommand CreateDbCommand()
        {
            return _innerConnection.CreateCommand();
        }

        public override void Open()
        {
            _innerConnection.Open();
        }

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            return _innerConnection.OpenAsync(cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _innerConnection?.Dispose();
                _onDisposed?.Invoke();
                _disposed = true;
            }
            base.Dispose(disposing);
        }
    }
}