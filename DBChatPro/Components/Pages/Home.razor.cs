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

namespace DBChatPro.Components.Pages
{
   public partial class Home : ComponentBase
   {
      private string defaultConnectionName = "Packtex";
      [Inject] public required IJSRuntime JSRuntime { get; set; }
      // Table styling
      private bool dense = false;
      private MudTabs? mudResultTabs = null;
      // private bool hover = true;
      private bool striped = true;
      private bool bordered = true;
      private bool showResultsOnly = false;
      // Form data
      public FormModel FmModel { get; set; } = new FormModel();

      // General UI data
      private bool Loading = false;
      private string LoadingMessage = String.Empty;
      public AIConnection ActiveConnection { get; set; } = new();
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
            ActiveConnection = new AIConnection() { SchemaRaw = new List<string>(), SchemaStructured = new List<TableSchema>() };
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
         // Check if the query is a SELECT statement
         if ((!Query.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) || Query.Trim().Contains("EXEC", StringComparison.OrdinalIgnoreCase)) || Query.Count(c => c == ';') > 1)
         {
            Snackbar.Add("Only single SELECT queries are allowed. You can however copy the SQL and run it in a different tool, for example Azure Data Studio", Severity.Error);
            return;
         }
         RowData = DatabaseService.GetDataTable(ActiveConnection, Query);
         Snackbar.Add("Results updated.", Severity.Success);
         if (mudResultTabs != null)
         {
            mudResultTabs.ActivatePanel(0);
         }
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

      public async Task OnSubmit()
      {
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
            LoadingMessage = "Getting the AI query...";
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
   }
}