using DBChatPro;
using Xunit;
using System.Collections.Generic;

namespace XUnitTestProject
{
    public class TestHistoryService
    {
        [Fact]
        public void SaveFavorite_ShouldAddFavorite()
        {
            // Arrange
            string prompt = "Test Prompt";
            string connectionName = "Test Connection";
            HistoryService.SaveFavorite(prompt, connectionName);

            // Act
            List<HistoryItem> favorites = HistoryService.GetFavorites(connectionName);


            // Assert
            Assert.Contains(favorites, item => item.Prompt == prompt && item.ConnectionName == connectionName);
        }

        [Fact]
        public void SaveHistory_ShouldAddHistoryItem()
        {
            // Arrange
            string prompt = "Test Prompt";
            string connectionName = "Test Connection";
            string query = "SELECT * FROM TestTable";
            HistoryService.SaveHistory(prompt, connectionName, query);

            // Act
            List<HistoryItem> queries = HistoryService.GetQueries(connectionName);

            // Assert
            Assert.Contains(queries, item => item.Query == query && item.ConnectionName == connectionName);
        }


        [Fact]
        public void GetFavorites_ShouldReturnFavoritesForConnection()
        {
            // Arrange
            string connectionName = "Test Favorite Connection";
            HistoryService.SaveFavorite("Favorite 1", connectionName);
            HistoryService.SaveFavorite("Favorite 2", connectionName);

            // Act
            List<HistoryItem> favorites = HistoryService.GetFavorites(connectionName);

            // Assert
            Assert.Equal(2, favorites.Count(c => c.ConnectionName == connectionName));
        }
        [Fact]
        public void GetQueries_ShouldReturnQueriesForConnection()
        {
            // Arrange
            string connectionName = "Test Connection";
            HistoryService.SaveHistory("Prompt 1", connectionName, "Query 1");
            HistoryService.SaveHistory("Prompt 2", connectionName, "Query 2");

            // Act
            List<HistoryItem> queries = HistoryService.GetQueries(connectionName);

            // Assert
            Assert.Equal(2, queries.Count);
        }
    }
}