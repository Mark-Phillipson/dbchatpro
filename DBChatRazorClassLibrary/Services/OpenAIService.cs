using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using OpenAI.Chat;
namespace DBChatPro.Services
{
   public class OpenAIService
   {
      private static readonly HttpClient httpClient = new HttpClient();
      static OpenAIService()
      {
         // httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
         // Configure HttpClient to include your OpenAI API key in each request
         // string openAIKey = "sk-?";

         //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAIKey);
      }

      public static async Task<AIQuery> GetAISQLQuery(string userPrompt, AIConnection aiConnection)
      {
         System.ClientModel.ApiKeyCredential apiKeyCredential = new System.ClientModel.ApiKeyCredential(Constants.OpenAIAPIKEY);
         var openAI = new ChatClient("gpt-4o", apiKeyCredential);

         string prompt = BuildPrompt(userPrompt, aiConnection);
         var response = await openAI.CompleteChatAsync(prompt);
         var responseContent = response.Value.Content[0].Text.Replace("```json", "").Replace("```", "").Replace("\\n", "");

         try
         {
            var results = JsonSerializer.Deserialize<AIQuery>(responseContent);
            if (results != null)
            {
               return results;
            }
            else
            {
               return new AIQuery();
            }
         }
         catch (Exception exception)
         {
            throw new Exception("Failed to parse AI response as a SQL Query. The AI response was: " + response.Value.Content[0].Text + " The error was: " + exception.Message);
         }
      }

      private static string BuildPrompt(string userPrompt, AIConnection aiConnection)
      {
         if (aiConnection == null)
         {
            throw new ArgumentNullException(nameof(aiConnection));
         }
         if (aiConnection.SchemaRaw == null)
         {
            throw new ArgumentNullException(nameof(aiConnection.SchemaRaw));
         }
         var builder = new StringBuilder();
         builder.AppendLine("You are a helpful, cheerful database assistant. Do not respond with any information unrelated to databases or queries. Also bear in mind that some users may not be familiar with SQL queries so use clear language instead.  Also note that a user is possibly using speech recognition to enter the query, so try to match on similar sounding words. Use the following database schema when creating your answers:");
         // Append database schema and user prompt to the builder
         // Assuming aiConnection.SchemaRaw is a collection of table definitions or similar
         foreach (var table in aiConnection!.SchemaRaw)
         {
            builder.AppendLine(table);
         }
         builder.AppendLine(userPrompt); // Append the user's prompt last
         builder.AppendLine("Include column name headers in the query results.");
         builder.AppendLine("Always provide your answer in the JSON format below:");
         builder.AppendLine(@"{ ""summary"": ""your-summary"", ""query"":  ""your-query"" }");
         builder.AppendLine("Output ONLY JSON formatted on a single line. Do not use new line characters.");
         builder.AppendLine(@"In the preceding JSON response, substitute ""your-query"" with Microsoft SQL Server Query to retrieve the requested data.");
         builder.AppendLine(@"In the preceding JSON response, substitute ""your-summary"" with an explanation of each step you took to create this query in a detailed paragraph.");
         builder.AppendLine("Do not use MySQL syntax.");
         builder.AppendLine("Always limit the SQL Query to 300 rows for example SELECT TOP(300)");
         builder.AppendLine("Only include pertinent columns of the table in the query.");
         builder.AppendLine("If the request is to insert update or delete records, still create the query but note it will not be allowed to be executed.");

         return builder.ToString();
      }
   }
}