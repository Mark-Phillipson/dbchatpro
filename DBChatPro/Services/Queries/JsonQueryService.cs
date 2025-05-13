using DBChatPro.Models;
using System.Text.Json;

namespace DBChatPro
{
    public class JsonQueryService : IQueryService
    {
        private readonly string _filePath;
        private readonly object _fileLock = new object();
        private List<HistoryItem> _queries;

        public JsonQueryService(string filePath = null)
        {
            _filePath = filePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "queries.json");
            _queries = LoadQueries();
        }

        private List<HistoryItem> LoadQueries()
        {
            lock (_fileLock)
            {
                if (!File.Exists(_filePath))
                {
                    return new List<HistoryItem>();
                }

                try
                {
                    string json = File.ReadAllText(_filePath);
                    return JsonSerializer.Deserialize<List<HistoryItem>>(json) ?? new List<HistoryItem>();
                }
                catch (Exception ex)
                {
                    // Log exception properly in production code
                    Console.WriteLine($"Error loading queries: {ex.Message}");
                    return new List<HistoryItem>();
                }
            }
        }

        private void SaveQueriesToFile()
        {
            lock (_fileLock)
            {
                try
                {
                    // Ensure directory exists
                    string directory = Path.GetDirectoryName(_filePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    string json = JsonSerializer.Serialize(_queries, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    File.WriteAllText(_filePath, json);
                }
                catch (Exception ex)
                {
                    // Log exception properly in production code
                    Console.WriteLine($"Error saving queries: {ex.Message}");
                }
            }
        }

        public Task<List<HistoryItem>> GetQueries(string connectionName, QueryType queryType)
        {
            lock (_fileLock)
            {
                // Refresh from file to get latest data
                _queries = LoadQueries();
                return Task.FromResult(_queries.Where(x => x.QueryType == queryType).ToList());
            }
        }

        public Task SaveQuery(string query, string connectionName, QueryType queryType)
        {
            lock (_fileLock)
            {
                _queries = LoadQueries(); // Ensure we have the latest data
                
                _queries.Add(new HistoryItem()
                {
                    Id = _queries.Count > 0 ? _queries.Max(q => q.Id) + 1 : 1,
                    Query = query,
                    Name = query,
                    ConnectionName = connectionName,
                    QueryType = queryType
                });

                SaveQueriesToFile();
                return Task.CompletedTask;
            }
        }
    }
}
