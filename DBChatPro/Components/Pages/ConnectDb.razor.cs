using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using DBChatPro;
using Microsoft.AspNetCore.Components.Forms;

namespace DBChatPro.Components.Pages
{
    public partial class ConnectDb : ComponentBase
    {
        string Error = String.Empty;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";

        AIConnection? Connection = null;
        // bool success;
        //private bool Loading = false;
        AIConnection aiConnection = new() { Name = "", ConnectionString = "", SchemaStructured = new List<TableSchema>(), SchemaRaw = new List<string>() };
        List<AIConnection> ExistingDbs = new List<AIConnection>();

        protected override void OnInitialized()
        {
            try
            {
                ExistingDbs = DatabaseService.GetAIConnections();
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.Message);
                throw;
            }
            Connection = new AIConnection() { Name = "", ConnectionString = "", SchemaRaw = new List<string>(), SchemaStructured = new List<TableSchema>() };

        }

        private void DeleteConnection(string name)
        {
            ExistingDbs = DatabaseService.GetAIConnections();

            var existing = ExistingDbs.FirstOrDefault(x => x.Name == name);
            if (existing != null) { ExistingDbs.Remove(existing); }
            File.WriteAllText(@"AIConnections.txt", JsonSerializer.Serialize(ExistingDbs));
        }

        private void OnValidSubmit(EditContext context)
        {
            try
            {
                //Loading = true;
                if (Connection != null)
                {
                    aiConnection = GenerateSchema(Connection);
                }
                //Loading = false;
                Error = String.Empty;
            }
            catch (Exception e)
            {
                Error = e.Message;
            }
        }
        private void SaveSchema()
        {
            var aiConns = new List<AIConnection>();
            try
            {
                aiConns = DatabaseService.GetAIConnections();
            }
            catch (Exception e)
            {
                Error = e.Message;
            }
            // Before saving the connection string need to remove the user id and password from the connection string
            if (aiConnection.ConnectionString.Contains("User ID") && aiConnection.ConnectionString.Contains("Password"))
            {
                aiConnection.ConnectionString = aiConnection.ConnectionString.Replace($"User ID={Username};", "User ID=;");
                aiConnection.ConnectionString = aiConnection.ConnectionString.Replace($"Password={Password};", "Password=;");

            }
            aiConns.Add(aiConnection);

            File.WriteAllText(@"AIConnections.txt", JsonSerializer.Serialize(aiConns));

            ExistingDbs = DatabaseService.GetAIConnections();
            Error = String.Empty;
        }
        private AIConnection GenerateSchema(AIConnection conn)
        {
            AIConnection aiCon = new() { Name = "", ConnectionString = "", SchemaRaw = new List<string>(), SchemaStructured = new List<TableSchema>() };
            aiCon.ConnectionString = conn.ConnectionString;
            aiCon.Name = conn.Name;
            aiCon.ExtraInformation = conn.ExtraInformation;
            if (aiCon.ConnectionString.Contains("User ID") && aiCon.ConnectionString.Contains("Password"))
            {
                aiCon.ConnectionString = aiCon.ConnectionString.Replace("User ID=;", $"User ID={Username};");
                aiCon.ConnectionString = aiCon.ConnectionString.Replace("Password=;", $"Password={Password};");
            }

            List<KeyValuePair<string, string>> rows = new();

            using (SqlConnection connection = new SqlConnection(aiCon.ConnectionString))
            {
                connection.Open();

                string sql = @"SELECT SCHEMA_NAME(schema_id) + '.' + o.Name AS 'TableName', c.Name as 'ColumName'
FROM sys.columns c
JOIN sys.objects o ON o.object_id = c.object_id
WHERE o.type = 'U'
ORDER BY o.Name";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var table = reader.GetValue(0).ToString();
                            var column = reader.GetValue(1).ToString();
                            if (table != null && column != null)
                            {
                                rows.Add(new KeyValuePair<string, string>(table, column));
                            }
                        }
                    }
                }
            }

            var groups = rows.GroupBy(x => x.Key);

            foreach (var group in groups)
            {
                if (!group.Key.Contains(".aspnet", StringComparison.OrdinalIgnoreCase))
                {
                    aiCon.SchemaStructured.Add(new TableSchema() { TableName = group.Key, Columns = group.Select(x => x.Value).ToList() });
                }
            }

            var textLines = new List<string>();

            foreach (var table in aiCon.SchemaStructured)
            {
                var schemaLine = $"- {table.TableName} (";

                if (table != null && table.Columns != null && table.Columns.Count > 0)
                {
                    foreach (var column in table.Columns)
                    {
                        schemaLine += column + ", ";
                    }
                }
                schemaLine += ")";
                schemaLine = schemaLine.Replace(", )", " )");

                Console.WriteLine(schemaLine);
                textLines.Add(schemaLine);
            }

            aiCon.SchemaRaw = textLines;

            // Before saving the connection string need to remove the user id and password from the connection string
            if (aiCon.ConnectionString.Contains("User ID") && aiCon.ConnectionString.Contains("Password"))
            {
                aiCon.ConnectionString = aiCon.ConnectionString.Replace($"User ID={Username};", "User ID=;");
                aiCon.ConnectionString = aiCon.ConnectionString.Replace($"Password={Password};", "Password=;");
            }

            return aiCon;
        }

    }
}