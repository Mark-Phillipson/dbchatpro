using System;
using System.ClientModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace DBChatPro.Services
{
   public class OpenAIService
   {
      private static readonly HttpClient httpClient = new HttpClient();
      private static readonly bool useAzureOpenAI;
      private static readonly IChatCompletionService? chatService;
      static OpenAIService()
      {
         var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
         string model = "gpt-4o-mini";
         string machineName = Environment.MachineName;
         useAzureOpenAI = !machineName.Equals("J40L4V3", StringComparison.OrdinalIgnoreCase);

         if (useAzureOpenAI)
         {
            string? apiKey = configuration["AzureOpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
               throw new ArgumentNullException(nameof(apiKey), "API key cannot be null or empty.");
            }
            string? endpoint = configuration["AzureOpenAI:Endpoint"];
            if (string.IsNullOrEmpty(endpoint))
            {
               throw new ArgumentNullException(nameof(endpoint), "Endpoint cannot be null or empty.");
            }

            chatService = new AzureOpenAIChatCompletionService(model, endpoint, apiKey);
         }
         else
         {
            string? OpenAI_ApiKey = configuration["OpenAIKey"];
            if (string.IsNullOrEmpty(OpenAI_ApiKey))
            {
               throw new ArgumentNullException(nameof(OpenAI_ApiKey), "OpenAI API key cannot be null or empty.");
            }
            chatService = new OpenAIChatCompletionService(model, OpenAI_ApiKey);
         }
         if (chatService == null)
         {

            throw new ArgumentNullException(nameof(chatService), "Chat service cannot be null.");
         }
      }
      public static async Task<AIQuery> GetAISQLQuery(string userPrompt, AIConnection aiConnection)
      {
         string prompt = BuildPrompt(userPrompt, aiConnection);

         if (chatService == null)
         {
            throw new InvalidOperationException("Chat service is not initialized.");
         }
         IReadOnlyList<ChatMessageContent> response = await chatService.GetChatMessageContentsAsync(prompt);
         ChatMessageContent? chatMessageContent = response.FirstOrDefault();
         string? responseContent = chatMessageContent?.Content;
         responseContent = responseContent?.Replace("```json", "").Replace("```", "").Replace("\\n", "");

         try
         {
            var results = JsonSerializer.Deserialize<AIQuery>(responseContent ?? string.Empty);
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
            throw new Exception("Failed to parse AI response as a SQL Query. The AI response was: " + responseContent + " The error was: " + exception.Message);
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