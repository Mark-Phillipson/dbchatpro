using Amazon.BedrockRuntime;
using Azure.AI.Inference;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using DBChatPro.Models;
using Microsoft.Extensions.AI;
using OpenAI;
using System.Text;
using System.Text.Json;

namespace DBChatPro.Services
{
    // Use this constructor if you're using vanilla OpenAI instead of Azure OpenAI
    // Make sure to update your Program.cs as well
    //public class OpenAIService(OpenAIClient aiClient)d

    public class AIService(IConfiguration config, IServiceProvider serviceProvider)
    {
        // Make the field nullable to prevent CS8618 warning
        private IChatClient? aiClient;

        public async Task<AIQuery> GetAISQLQuery(string aiModel, string aiService, string userPrompt, DatabaseSchema dbSchema, string databaseType)
        {
            if (aiClient == null)
            {
                aiClient = CreateChatClient(aiModel, aiService);
            }

            List<ChatMessage> chatHistory = new List<ChatMessage>();
            var builder = new StringBuilder();
            var maxRows = config.GetValue<string>("MAX_ROWS") ?? "100"; // Default to 100 if not specified

            builder.AppendLine("Your are a helpful, cheerful database assistant. Do not respond with any information unrelated to databases or queries. Use the following database schema when creating your answers:");

            foreach(var table in dbSchema.SchemaRaw)
            {
                builder.AppendLine(table);
            }

            builder.AppendLine("Include column name headers in the query results.");
            builder.AppendLine("Always provide your answer in the JSON format below:");
            builder.AppendLine(@"{ ""summary"": ""your-summary"", ""query"":  ""your-query"" }");
            builder.AppendLine("Output ONLY JSON formatted on a single line. Do not use new line characters.");
            builder.AppendLine(@"In the preceding JSON response, substitute ""your-query"" with the database query used to retrieve the requested data.");
            builder.AppendLine(@"In the preceding JSON response, substitute ""your-summary"" with an explanation of each step you took to create this query in a detailed paragraph.");
            builder.AppendLine($"Only use {databaseType} syntax for database queries.");
            builder.AppendLine($"Always limit the SQL Query to {maxRows} rows.");
            builder.AppendLine("Always include all of the table columns and details.");

            // Build the AI chat/prompts
            if (string.IsNullOrEmpty(config.GetValue<string>("OLLAMA_ENDPOINT")))
            {
                // Ollama doesn't play well with system prompts and large context windows, so the main prompt can't be a system prompt when Ollama is enabled
                // This also means we have to disable supplemental chat tab :(
                chatHistory.Add(new ChatMessage(Microsoft.Extensions.AI.ChatRole.System, builder.ToString()));
            }
            else
            {
                chatHistory.Add(new ChatMessage(Microsoft.Extensions.AI.ChatRole.User, builder.ToString()));
            }
            
            chatHistory.Add(new ChatMessage(Microsoft.Extensions.AI.ChatRole.User, userPrompt));

            // Send request to Azure OpenAI model
            var response = await aiClient.GetResponseAsync(chatHistory);
            var responseContent = response.Messages[0].Text.Replace("```json", "").Replace("```", "").Replace("\\n", " ");

            try
            {
                // Note: JsonSerializer.Deserialize can return null, so we'll handle that
                var result = JsonSerializer.Deserialize<AIQuery>(responseContent);
                return result ?? new AIQuery { summary = "Failed to parse response", query = "SELECT 'Error parsing AI response'" };
            }
            catch (Exception ex)
            {
                // Use the exception variable to avoid CS0168 warning
                throw new Exception($"Failed to parse AI response as a SQL Query. Error: {ex.Message}. The AI response was: {response.Messages[0].Text}");
            }
        }

        private IChatClient CreateChatClient(string aiModel, string aiService)
        {
            switch (aiService)
            {
                case "AzureOpenAI":
                    var azureEndpoint = config.GetValue<string>("AZURE_OPENAI_ENDPOINT") ?? 
                        throw new ArgumentNullException("AZURE_OPENAI_ENDPOINT", "Azure OpenAI endpoint is not configured");
                    return new AzureOpenAIClient(
                            new Uri(azureEndpoint),
                            new DefaultAzureCredential())
                                .AsChatClient(modelId: aiModel);
                case "OpenAI":
                    var openAiKey = config.GetValue<string>("OPENAI_KEY") ?? 
                        throw new ArgumentNullException("OPENAI_KEY", "OpenAI API key is not configured");
                    return new OpenAIClient(openAiKey)
                                .AsChatClient(modelId: aiModel);
                case "Ollama":
                    var ollamaEndpoint = config.GetValue<string>("OLLAMA_ENDPOINT") ?? 
                        throw new ArgumentNullException("OLLAMA_ENDPOINT", "Ollama endpoint is not configured");
                    return new OllamaChatClient(ollamaEndpoint, aiModel);
                case "GitHubModels":
                    var githubKey = config.GetValue<string>("GITHUB_MODELS_KEY") ?? 
                        throw new ArgumentNullException("GITHUB_MODELS_KEY", "GitHub models API key is not configured");
                    return new ChatCompletionsClient(
                            endpoint: new Uri("https://models.inference.ai.azure.com"),
                            new AzureKeyCredential(githubKey))
                                .AsChatClient(aiModel);
                case "AWSBedrock":
                    var bedrockClient = serviceProvider.GetRequiredService<IAmazonBedrockRuntime>();
                    return new AWSBedrockClient(bedrockClient, aiModel);
                default:
                    throw new ArgumentException($"Unsupported AI service: {aiService}", nameof(aiService));
            }
        }

        public async Task<ChatResponse> ChatPrompt(List<ChatMessage> prompt, string aiModel, string aiService)
        {
            if (aiClient == null)
            {
                aiClient = CreateChatClient(aiModel, aiService);
            }

            return await aiClient.GetResponseAsync(prompt);
        }
    }
}
