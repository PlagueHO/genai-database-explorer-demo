using GenAIDBExplorer.Core.Models.Project;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Net.Sockets;
using System.Resources;
using System.Text;

namespace GenAIDBExplorer.Core.Data.DatabaseProviders;

/// <summary>
/// Responsible for producing a connection string for the requested project and establishing a SQL connection.
/// </summary>
/// <remarks>
/// This class is responsible for creating and opening a SQL connection using the connection string provided in the project settings. It handles the connection lifecycle, including logging connection attempts and errors.
/// </remarks>
public sealed class SqlConnectionProvider(
    IProject project,
    ILogger<SqlConnectionProvider> logger
) : IDatabaseConnectionProvider
{
    private readonly IProject _project = project;
    private readonly ILogger<SqlConnectionProvider> _logger = logger;
    private static readonly ResourceManager _resourceManagerLogMessages = new("GenAIDBExplorer.Core.Resources.LogMessages", typeof(SqlConnectionProvider).Assembly);
    private static readonly ResourceManager _resourceManagerErrorMessages = new("GenAIDBExplorer.Core.Resources.ErrorMessages", typeof(SqlConnectionProvider).Assembly);

    /// <summary>
    /// Factory method for producing a live SQL connection instance.
    /// </summary>
    /// <returns>A <see cref="SqlConnection"/> instance in the "Open" state.</returns>
    /// <remarks>
    /// This method retrieves the connection string from the project settings and attempts to open a SQL connection. It logs the connection attempt and any errors that occur.
    /// Connection pooling is configured based on the database settings to optimize connection reuse and performance.
    /// Includes retry logic for transient connection failures.
    /// </remarks>
    /// <exception cref="InvalidDataException">Thrown if the connection string is missing.</exception>
    /// <exception cref="SqlException">Thrown if there is an error connecting to the SQL database.</exception>
    /// <exception cref="Exception">Thrown if there is a general error connecting to the database.</exception>
    public async Task<SqlConnection> ConnectAsync()
    {
        var baseConnectionString =
            _project.Settings.Database.ConnectionString ??
                throw new InvalidDataException($"Missing database connection string.");

        var connectionString = BuildConnectionStringWithPooling(baseConnectionString);

        var maxRetries = _project.Settings.Database.MaxRetryAttempts;
        var retryDelay = _project.Settings.Database.RetryDelayMilliseconds;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                var connection = new SqlConnection(connectionString);
                
                _logger.LogInformation(_resourceManagerLogMessages.GetString("ConnectingSQLDatabase"));
                await connection.OpenAsync().ConfigureAwait(false);
                
                // Perform connection health check if enabled
                if (_project.Settings.Database.EnableHealthMonitoring)
                {
                    await PerformHealthCheckAsync(connection);
                }
                
                _logger.LogInformation(_resourceManagerLogMessages.GetString("ConnectSQLSuccessful"));
                _logger.LogInformation(_resourceManagerLogMessages.GetString("DatabaseConnectionState"), connection.State);
                
                return connection;
            }
            catch (SqlException ex) when (IsTransientError(ex) && attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Transient SQL error on attempt {Attempt} of {MaxRetries}. Retrying in {Delay}ms...", 
                    attempt + 1, maxRetries + 1, retryDelay);
                await Task.Delay(retryDelay);
                continue;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, _resourceManagerErrorMessages.GetString("ErrorConnectingToDatabaseSQL"));
                throw;
            }
            catch (Exception ex) when (IsTransientException(ex) && attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Transient error on attempt {Attempt} of {MaxRetries}. Retrying in {Delay}ms...", 
                    attempt + 1, maxRetries + 1, retryDelay);
                await Task.Delay(retryDelay);
                continue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _resourceManagerErrorMessages.GetString("ErrorConnectingToDatabase"));
                throw;
            }
        }

        throw new InvalidOperationException("Failed to establish connection after all retry attempts.");
    }

    /// <summary>
    /// Builds a connection string with connection pooling parameters based on database settings.
    /// </summary>
    /// <param name="baseConnectionString">The base connection string from project settings.</param>
    /// <returns>A connection string with pooling parameters applied.</returns>
    private string BuildConnectionStringWithPooling(string baseConnectionString)
    {
        var builder = new SqlConnectionStringBuilder(baseConnectionString);
        var settings = _project.Settings.Database;

        // Apply pooling settings
        builder.Pooling = settings.PoolingEnabled;
        builder.MaxPoolSize = settings.MaxPoolSize;
        builder.MinPoolSize = settings.MinPoolSize;
        builder.ConnectTimeout = settings.ConnectionTimeout;
        builder.CommandTimeout = settings.CommandTimeout;

        // Ensure connection pooling key parameters are set appropriately
        if (settings.PoolingEnabled)
        {
            // Enable connection reset to ensure clean connections from pool
            builder.ConnectionReset = true;
            
            // Set load balance timeout to help with connection pool management
            builder.LoadBalanceTimeout = 30;
        }

        var finalConnectionString = builder.ConnectionString;
        _logger.LogDebug("Built connection string with pooling - MaxPoolSize: {MaxPoolSize}, MinPoolSize: {MinPoolSize}, Timeout: {Timeout}", 
            settings.MaxPoolSize, settings.MinPoolSize, settings.ConnectionTimeout);

        return finalConnectionString;
    }

    /// <summary>
    /// Performs a basic health check on the database connection.
    /// </summary>
    /// <param name="connection">The connection to check.</param>
    private async Task PerformHealthCheckAsync(SqlConnection connection)
    {
        try
        {
            using var command = new SqlCommand("SELECT 1", connection);
            command.CommandTimeout = _project.Settings.Database.CommandTimeout;
            await command.ExecuteScalarAsync().ConfigureAwait(false);
            _logger.LogDebug("Connection health check passed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connection health check failed");
            throw;
        }
    }

    /// <summary>
    /// Determines if a SqlException represents a transient error that should be retried.
    /// </summary>
    /// <param name="ex">The SqlException to check.</param>
    /// <returns>True if the error is transient and should be retried.</returns>
    private static bool IsTransientError(SqlException ex)
    {
        // Common transient error numbers for SQL Server
        int[] transientErrorNumbers = {
            2,     // Timeout expired
            53,    // Network path not found
            121,   // The semaphore timeout period has expired
            233,   // The client was unable to establish a connection
            10053, // An established connection was aborted by the software in your host machine
            10054, // An existing connection was forcibly closed by the remote host
            10060, // A connection attempt failed because the connected party did not properly respond
            10061, // No connection could be made because the target machine actively refused it
            18456, // Login failed (can be transient in some cases)
            40197, // The service has encountered an error processing your request
            40501, // The service is currently busy
            40613, // Database on server is not currently available
        };

        return transientErrorNumbers.Contains(ex.Number);
    }

    /// <summary>
    /// Determines if a general Exception represents a transient error that should be retried.
    /// </summary>
    /// <param name="ex">The Exception to check.</param>
    /// <returns>True if the error is transient and should be retried.</returns>
    private static bool IsTransientException(Exception ex)
    {
        return ex is TimeoutException || 
               ex is SocketException ||
               (ex is InvalidOperationException && ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase));
    }
}