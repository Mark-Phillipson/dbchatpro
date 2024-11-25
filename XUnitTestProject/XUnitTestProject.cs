using DBChatPro;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace XUnitTestProject
{
    public class DatabaseServiceTest
    {
        public string connectionString = "Data Source=Localhost;Initial Catalog=PacktexContext;Integrated Security=True;Connect Timeout=120;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

        [Fact]
        public void GetDataTable_ShouldReturnDataTable()
        {
            // Arrange
            var conn = new AIConnection() { ConnectionString = connectionString, Name = "Test Packtex Connection" };
            string sqlQuery = "SELECT * FROM Customers";

            // Act
            List<List<string>> result = DatabaseService.GetDataTable(conn, sqlQuery);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count > 0);
        }

        [Fact]
        public void GetDataBlob_ShouldReturnDataBlob()
        {
            // Arrange
            var conn = new AIConnection { ConnectionString = connectionString, Name = "Test Packtex Connection" };
            string sqlQuery = "SELECT * FROM Customers";

            // Act
            string result = DatabaseService.GetDataBlob(conn, sqlQuery);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public void GetAIConnections_ShouldReturnAIConnections()
        {
            // Arrange
            File.WriteAllText("AIConnections.txt", "[{\"ConnectionString\":\"your_connection_string\", \"Name\":\"your_connection_name\"}]");
            // Act
            List<AIConnection> result = DatabaseService.GetAIConnections();

            // Assert  
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("your_connection_string", result[0].ConnectionString);
        }
    }
}