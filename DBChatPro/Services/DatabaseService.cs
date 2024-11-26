using Microsoft.Data.SqlClient;
using System.Text;
using System.Text.Json;

namespace DBChatPro
{
    public class DatabaseService
    {
        public static List<List<string>> GetDataTable(AIConnection conn, string sqlQuery)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

            var rows = new List<List<string>>();
            using (SqlConnection connection = new SqlConnection(conn.ConnectionString))
            {
                try
                {
                    connection.Open();
                }
                catch (System.Exception exception)
                {
                    System.Console.WriteLine(exception.Message);
                    List<string> strings = new List<string>() { $"There appears to be a mistake in the connection string: {exception.Message}" };
                    return new List<List<string>>() { strings };
                }

                using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                {
                    try
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            bool headersAdded = false;
                            while (reader.Read())
                            {
                                var cols = new List<string>();
                                var headerCols = new List<string>();
                                if (!headersAdded)
                                {
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        headerCols.Add(reader.GetName(i).ToString());
                                    }
                                    headersAdded = true;
                                    rows.Add(headerCols);
                                }

                                for (int i = 0; i <= reader.FieldCount - 1; i++)
                                {
                                    try
                                    {
                                        var value = reader.GetValue(i).ToString();
                                        if (value != null)
                                        {
                                            cols.Add(value);
                                        }
                                    }
                                    catch
                                    {
                                        cols.Add("DataTypeConversionError");
                                    }
                                }
                                rows.Add(cols);
                            }
                        }
                    }
                    catch (System.Exception exception)
                    {
                        System.Console.WriteLine(exception.Message);
                        List<string> strings = new List<string>() { $"There appears to be a mistake in the SQL statement: {exception.Message}" };
                        return new List<List<string>>() { strings };
                    }
                }
            }

            return rows;
        }

        public static string GetDataBlob(AIConnection conn, string sqlQuery)
        {
            StringBuilder results = new StringBuilder();
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

            var rows = new List<List<string>>();
            using (SqlConnection connection = new SqlConnection(conn.ConnectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        bool headersAdded = false;
                        while (reader.Read())
                        {
                            var cols = new List<string>();
                            var headerCols = new List<string>();
                            if (!headersAdded)
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    results.Append(reader.GetName(i).ToString()).Append(" | ");
                                }
                                headersAdded = true;
                                results.AppendLine();
                            }

                            for (int i = 0; i <= reader.FieldCount - 1; i++)
                            {
                                try
                                {
                                    results.Append(reader.GetValue(i).ToString()).Append(" | ");
                                }
                                catch
                                {
                                    results.Append("DataTypeConversionError").Append(" | ");
                                }
                            }
                            results.AppendLine();
                        }
                    }
                }
            }
            return results.ToString();
        }

        public static List<AIConnection> GetAIConnections()
        {
            try
            {
                var schema = File.ReadAllText("AIConnections.txt");
                var result = JsonSerializer.Deserialize<List<AIConnection>>(schema);
                if (result != null)
                {
                    return result;
                }
                else
                {
                    return new List<AIConnection>();
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.Message);
                return new List<AIConnection>();
            }
        }
    }
}
