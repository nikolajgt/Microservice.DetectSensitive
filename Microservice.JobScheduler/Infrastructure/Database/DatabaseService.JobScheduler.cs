using Microservice.Domain.Models.Scheduler;
using Microservice.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;

namespace Microservice.JobScheduler.Infrastructure.Database;

public partial class DatabaseService
{
    public async Task<IEnumerable<Job>> GetJobsAsync(CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);

            var response = await connection.QueryAsync<Job>(@"
                SELECT Id, JobName, JobType, JobStatus, PathToHarvest, CronSchedule, LastRun
                FROM Harvester.Jobs;
            ", cancellationToken) ?? new List<Job>();

            await connection.CloseAsync();
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{CorrelationId} Error saving files, rolling back transaction", _correlationId);
            await connection.CloseAsync();
            return new List<Job>();
        }
    }

    public async Task<JobHistory?> StartJobAsync(JobHistory jobHistory, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);

            var insertQuery = @"INSERT INTO Harvester.JobHistories (Id, JobId, JobStatus, Started, Finished)
                                VALUES (@Id, @JobId, @JobStatus, @Started, @Finished);";

            await connection.ExecuteAsync(
              insertQuery,
              param: new
              {
                  Id = jobHistory.Id,
                  JobId = jobHistory.JobId,
                  Status = jobHistory.Status,
                  Started = jobHistory.Started,
                  Finished = jobHistory.Finished ?? (object)DBNull.Value,
              });

            await connection.CloseAsync();

            return jobHistory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{CorrelationId} Error saving files, rolling back transaction", _correlationId);
            await connection.CloseAsync();
            return null;
        }
    }

    public async Task FinishJobAsync(JobHistory jobHistory, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);

            var updateQuery = @"UPDATE Harvester.JobHistories 
                    SET Finished = @Finished, 
                        Failed = @Failed 
                    WHERE Id = @Id;";

            await connection.ExecuteAsync(
                updateQuery,
                param: new
                {
                    Id = jobHistory.Id,
                    Finished = jobHistory.Finished ?? DateTime.UtcNow,
                    Failed = jobHistory.Failed
                });

            await connection.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{CorrelationId} Error saving files, rolling back transaction", _correlationId);
            await connection.CloseAsync();
        }
    }
}
