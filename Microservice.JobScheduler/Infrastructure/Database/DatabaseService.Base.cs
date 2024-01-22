using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.JobScheduler.Infrastructure.Database;

public partial class DatabaseService : IDisposable, IAsyncDisposable
{
    private readonly int _correlationId = Random.Shared.Next();
    private readonly ILogger<DatabaseService> _logger;
    private readonly string _connectionString;

    private readonly SqlConnection _connection;
    private SqlTransaction? _transaction;

    public DatabaseService(
        ILogger<DatabaseService> logger)
    {
        _connectionString = "Server=NIKOLAJGT;Database=Svende;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";
        _connection = new SqlConnection(_connectionString);
        _logger = logger;
    }

    public void BeginTransaction()
    {
        _connection.Open();
        _transaction ??= _connection.BeginTransaction();
    }

    public void CommitTransaction()
    {
        _transaction?.Commit();
        _transaction = null;
        _connection.Close();
    }

    public void RollbackTransaction()
    {
        _transaction?.Rollback();
        _connection.Close();
    }

    private bool disposed = false;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                _connection.Dispose();
                _transaction?.Dispose();
            }

            // Dispose unmanaged resources

            disposed = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        Dispose(false);
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    private async ValueTask DisposeAsyncCore()
    {
        if (_connection != null)
        {
            _transaction?.Dispose();

            await _connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    ~DatabaseService()
    {
        Dispose(false);
    }
}
