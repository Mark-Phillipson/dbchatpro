using System.Text.Json;

namespace DBChatPro
{
    public static class HistoryService
    {
        private static List<HistoryItem> Queries = new();
        private static List<HistoryItem> Favorites = new();

        public static void SaveFavorite(string prompt, string connectionName)
        {
            Favorites.Add(new HistoryItem()
            {
                Id = new Random().Next(0, 10000),
                Prompt = prompt,
                Query = prompt,//This is wrong but trying to work out why it doesn't work if it's not here
                ConnectionName = connectionName
            });
            SaveFavoritesToFile(Favorites);
        }
        private static void SaveFavoritesToFile(List<HistoryItem> favorites)
        {
            string json = JsonSerializer.Serialize(favorites);
            File.WriteAllText("favorites.json", json);
        }

        public static void SaveHistory(string prompt, string connectionName, string query)
        {
            Queries.Add(new HistoryItem()
            {
                Id = new Random().Next(0, 10000),
                Query = query,
                Prompt = prompt,
                ConnectionName = connectionName
            });
        }

        public static List<HistoryItem> GetQueries(string connectionName)
        {
            return Queries.Where(x => x.ConnectionName == connectionName).ToList();
        }

        public static List<HistoryItem> GetFavorites(string connectionName)
        {
            Favorites = LoadFavoritesFromFile();
            return Favorites.Where(x => x.ConnectionName == connectionName).ToList();
        }
        private static List<HistoryItem> LoadFavoritesFromFile()
        {
            if (File.Exists("favorites.json"))
            {
                string json = File.ReadAllText("favorites.json");
                var results = JsonSerializer.Deserialize<List<HistoryItem>>(json);
                if (results != null)
                {
                    return results;
                }
            }
            return new List<HistoryItem>();
        }

        internal static void DeleteFavoriteItem(int id)
        {
            var favorite = Favorites.FirstOrDefault(x => x.Id == id);
            if (favorite != null)
            {
                Favorites.Remove(favorite);
                SaveFavoritesToFile(Favorites);
            }
        }
    }
}
