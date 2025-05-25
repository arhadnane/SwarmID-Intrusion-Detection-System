using LiteDB;
using SwarmID.Core.Models;
using SwarmID.Core.Interfaces;

namespace SwarmID.Core.Repositories;

/// <summary>
/// LiteDB implementation of anomaly repository
/// </summary>
public class LiteDbAnomalyRepository : IAnomalyRepository, IDisposable
{
    private readonly LiteDatabase _database;
    private readonly ILiteCollection<Anomaly> _anomalies;

    public LiteDbAnomalyRepository(string connectionString)
    {
        _database = new LiteDatabase(connectionString);
        _anomalies = _database.GetCollection<Anomaly>("anomalies");
        
        // Create indexes for better performance
        _anomalies.EnsureIndex(x => x.DetectedAt);
        _anomalies.EnsureIndex(x => x.Status);
        _anomalies.EnsureIndex(x => x.Type);
    }    public async Task<IEnumerable<Anomaly>> GetAnomaliesAsync(DateTime? from = null, DateTime? to = null)
    {
        return await Task.Run(() =>
        {
            var query = _anomalies.Query();

            if (from.HasValue)
                query = query.Where(x => x.DetectedAt >= from.Value);

            if (to.HasValue)
            {
                // Add 1 day to the 'to' date and then subtract 1 millisecond to include the entire day
                var endOfDay = to.Value.Date.AddDays(1).AddMilliseconds(-1);
                query = query.Where(x => x.DetectedAt <= endOfDay);
            }

            return query.OrderByDescending(x => x.DetectedAt).ToList();
        });
    }public async Task<Anomaly?> GetAnomalyByIdAsync(string id)
    {
        return await Task.Run(() => 
        {
            if (Guid.TryParse(id, out var guidId))
            {
                return _anomalies.FindOne(x => x.Id == guidId);
            }
            return null;
        });
    }    public async Task SaveAnomalyAsync(Anomaly anomaly)
    {
        await Task.Run(() => 
        {
            // Set ID if not already set, or ensure it's a new GUID for inserts
            if (anomaly.Id == Guid.Empty)
                anomaly.Id = Guid.NewGuid();
            
            // Use Upsert to handle potential duplicate keys
            _anomalies.Upsert(anomaly);
        });
    }public async Task UpdateAnomalyAsync(Anomaly anomaly)
    {
        await Task.Run(() => 
        {
            if (anomaly.Id != Guid.Empty)
                _anomalies.Update(anomaly);
        });
    }    public async Task DeleteAnomalyAsync(string id)
    {
        await Task.Run(() => 
        {
            if (Guid.TryParse(id, out var guidId))
            {
                _anomalies.DeleteMany(x => x.Id == guidId);
            }
        });
    }

    public void Dispose()
    {
        _database?.Dispose();
    }
}
