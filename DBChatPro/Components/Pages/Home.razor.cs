using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using System.Text;
using DBChatPro.Models;
using DBChatPro;
using MudBlazor;
using DBChatPro.Services;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Web;

namespace DBChatPro.Components.Pages
{
   public partial class Home : ComponentBase
   {
      private string defaultConnectionName = "VoiceAdmin";
      [Inject] public required IJSRuntime JSRuntime { get; set; }
      // Table styling
      private bool dense = false;
      private MudTabs? mudResultTabs = null;
      // private bool hover = true;
      private bool striped = true;
      private bool bordered = true;
      // Webpage Settings
      private bool showResultsOnly = false;
      private bool IncludeLatestError = false;
      public string Username { get; set; } = "";
      public string Password { get; set; } = "";
      // Form data
      public FormModel FmModel { get; set; } = new FormModel();
      // General UI data
      private bool Loading = false;
      private string LoadingMessage = String.Empty;
      public AIConnection? ActiveConnection { get; set; }
      // Data lists
      public List<HistoryItem> History { get; set; } = new();
      public List<HistoryItem> Favorites { get; set; } = new();
      public List<List<string>> RowData = new();
      public List<AIConnection> Connections { get; set; } = new();

      // Prompt & completion data
      private string Prompt = String.Empty;
      private string Summary = String.Empty;
      private string Query = String.Empty;
      private string Error = String.Empty;
      private string filterText = String.Empty;

      // UI Drawer stuff
      bool open = true;
      Anchor anchor;
      void ToggleDrawer(Anchor anchor)
      {
         open = !open;
         this.anchor = anchor;
      }

      protected override void OnInitialized()
      {
         Connections = DatabaseService.GetAIConnections();
         if (Connections != null && Connections.Count > 0)
         {
            var connection = Connections.FirstOrDefault(c => c.Name == defaultConnectionName);
            if (connection != null)
            {
               ActiveConnection = connection;
            }
         }
         else
         {
            ActiveConnection = new AIConnection() { Name = "New Connection", ConnectionString = "TBC", SchemaRaw = new List<string>(), SchemaStructured = new List<TableSchema>() };
         }
         if (ActiveConnection == null)
         {
            return;
         }
         History = HistoryService.GetQueries(ActiveConnection.Name ?? "Name Undefined");
         Favorites = HistoryService.GetFavorites(ActiveConnection.Name ?? "Name Undefined");
      }

      private void SaveFavorite()
      {
         if (ActiveConnection == null)
         {
            Snackbar.Add("Please select a database connection first.", Severity.Error);
            return;
         }
         HistoryService.SaveFavorite(FmModel.Prompt, ActiveConnection.Name ?? "Name Undefined");
         Favorites = HistoryService.GetFavorites(ActiveConnection.Name ?? "Name Undefined");
         Snackbar.Add("Saved favorite!", Severity.Success);
      }

      private void ExecuteDatabaseQuery()
      {
         if (ActiveConnection == null)
         {
            Snackbar.Add("Please select a database connection first.", Severity.Error);
            return;
         }
         // Query = $"Break Query {Query}";
         Error = String.Empty;
         bool isSelectOnly = MakeSureSelectQueryOnly(Query);
         if (!isSelectOnly)
         {
            return;
         }
         Connections = DatabaseService.GetAIConnections();
         ActiveConnection = Connections.FirstOrDefault(c => c.Name == ActiveConnection.Name);
         if (ActiveConnection == null)
         {
            Snackbar.Add("Please select a database connection first.", Severity.Error);
            return;
         }
         if (ActiveConnection.ConnectionString.Contains("User ID") && ActiveConnection.ConnectionString.Contains("Password"))
         {
            if (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password))
            {
               ActiveConnection.ConnectionString = ActiveConnection.ConnectionString.Replace("User ID=;", $"User ID={Username};");
               ActiveConnection.ConnectionString = ActiveConnection.ConnectionString.Replace("Password=;", $"Password={Password};");
            }
            else if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
               Snackbar.Add("Please enter a username and password.", Severity.Error);
               return;
            }
         }
         Loading = true;
         RowData = DatabaseService.GetDataTable(ActiveConnection, Query);
         if (RowData != null && RowData.Count > 0)
         {
            var record = RowData?.FirstOrDefault();
            string? error = null;
            if (record != null)
            {
               error = record.FirstOrDefault(c => c.Contains("There appears to be a mistake in the SQL statement"));
            }
            if (!string.IsNullOrWhiteSpace(error))
            {
               Error = error;
            }
         }

         Loading = false;
         Snackbar.Add("Results updated.", Severity.Success);
         if (mudResultTabs != null)
         {
            mudResultTabs.ActivatePanel(0);
         }
      }

      private bool MakeSureSelectQueryOnly(string query)
      {
         if ((!Query.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) && Query.Trim().Contains("EXEC", StringComparison.OrdinalIgnoreCase)) || Query.Count(c => c == ';') > 1)
         {
            Snackbar.Add("Only single SELECT queries are allowed. You can however copy the SQL and run it in a different tool, for example Azure Data Studio", Severity.Error);
            return false;
         }
         return true;

      }

      public void LoadDatabase(string dbName)
      {
         var connection = DatabaseService.GetAIConnections().FirstOrDefault(x => x.Name == dbName);
         if (connection != null)
         {
            ActiveConnection = connection;
         }
         if (ActiveConnection == null)
         {
            Snackbar.Add("Please select a database connection first.", Severity.Error);
            return;
         }
         History = HistoryService.GetQueries(ActiveConnection.Name ?? "Name Undefined");
         Favorites = HistoryService.GetFavorites(ActiveConnection.Name ?? "Name Undefined");
         ClearUI();
      }

      private void ClearUI()
      {
         Prompt = String.Empty;
         Summary = String.Empty;
         Query = String.Empty;
         Error = String.Empty;
         RowData = new List<List<string>>();
         FmModel = new FormModel();
      }

      public async Task LoadHistoryItem(string prompt)
      {
         FmModel.Prompt = prompt;
         await GenerateSQLQueryFromPrompt(prompt);
      }
      private void DeleteFavoriteItem(int id)
      {
         if (ActiveConnection == null)
         {
            Snackbar.Add("Please select a database connection first.", Severity.Error);
            return;
         }
         HistoryService.DeleteFavoriteItem(id);
         Favorites = HistoryService.GetFavorites(ActiveConnection.Name ?? "Name Undefined");
         Snackbar.Add("Favorite item deleted!", Severity.Success);
      }
      public async Task OnSubmit()
      {
         if (string.IsNullOrWhiteSpace(FmModel.Prompt))
         {
            Snackbar.Add("Please enter a prompt.", Severity.Error);
            return;
         }
         if (IncludeLatestError && Error != null && Error.Length > 0)
         {
            string temporaryPrompt = $"{FmModel.Prompt} {Summary} Note the last time this was run we got this error: {Error}";
            await GenerateSQLQueryFromPrompt(temporaryPrompt);
            return;
         }
         await GenerateSQLQueryFromPrompt(FmModel.Prompt);
      }

      public async Task GenerateSQLQueryFromPrompt(string Prompt)
      {
         if (ActiveConnection == null)
         {
            Snackbar.Add("Please select a database connection first.", Severity.Error);
            return;
         }
         try
         {
            Loading = true;
            LoadingMessage = "Getting the AI SQL Server query please stand by...";
            var aiResponse = await OpenAIService.GetAISQLQuery(Prompt, ActiveConnection);

            if (aiResponse.query != null)
            {
               Query = aiResponse.query;
            }
            if (aiResponse.summary != null)
            {
               Summary = aiResponse.summary;
            }
            HistoryService.SaveHistory(Prompt, ActiveConnection.Name ?? "Name Undefined", aiResponse.
            query ?? "Query Undefined");
            History = HistoryService.GetQueries(ActiveConnection.Name ?? "Name Undefined");
            Loading = false;
            //Show the insight tab here
            if (mudResultTabs != null)
            {
               mudResultTabs.ActivatePanel(1);
            }
            StateHasChanged();
            // Move the focus to the generate button
            await JSRuntime.InvokeVoidAsync("focusElement", "generateButton");
         }
         catch (Exception e)
         {
            Error = e.Message;
            Loading = false;
            LoadingMessage = String.Empty;
         }
      }
      private async Task Clear()
      {
         FmModel.Prompt = string.Empty;
         // Move the focus to the prompt input
         await JSRuntime.InvokeVoidAsync("focusElement", "prompt");

      }
      List<TableSchema> beforeFilter = new List<TableSchema>();
      private void FilterTables()
      {
         if (!string.IsNullOrEmpty(filterText) && ActiveConnection != null)
         {
            beforeFilter = ActiveConnection.SchemaStructured ?? new List<TableSchema>();
            ActiveConnection.SchemaStructured = ActiveConnection?.SchemaStructured?.Where(x => x.TableName != null && x.TableName.ToLower().Contains(filterText.ToLower())).ToList();
         }
         else if (string.IsNullOrEmpty(filterText) && ActiveConnection != null)
         {
            ActiveConnection.SchemaStructured = beforeFilter;
         }
      }
      private async Task CopyToClipboard()
      {
         await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", Query);
         Snackbar.Add("Query copied to clipboard!", Severity.Success);
      }
      private async Task ExportToCsv()
      {
         if (RowData == null || RowData.Count == 0)
         {
            Snackbar.Add("No data to export.", Severity.Error);
            return;
         }
         var csv = new StringBuilder();
         foreach (var row in RowData!)
         {
            csv.AppendLine(string.Join(",", row));
         }

         var csvContent = csv.ToString();
         var bytes = Encoding.UTF8.GetBytes(csvContent);
         var base64 = Convert.ToBase64String(bytes);
         await JSRuntime.InvokeVoidAsync("downloadFile", "ResultExport.csv", base64);
      }
      private async Task HandleKeyDown(KeyboardEventArgs e)
      {
         if (e.Key == "Enter")
         {
            await OnSubmit();
         }
      }
   }
}