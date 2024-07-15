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

namespace DBChatPro.Components.Pages {
   public partial class Home : ComponentBase {
      // Table styling
      private bool dense = false;
      private bool hover = true;
      private bool striped = true;
      private bool bordered = true;

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
      void ToggleDrawer(Anchor anchor) {
         open = !open;
         this.anchor = anchor;
      }

      protected override async Task OnInitializedAsync() {
         Connections = DatabaseService.GetAIConnections();
         if (Connections.Count > 0) {
            ActiveConnection = Connections.FirstOrDefault();
         } else {
            ActiveConnection = new AIConnection() { SchemaRaw = new List<string>(), SchemaStructured = new List<TableSchema>() };
         }
         History = HistoryService.GetQueries(ActiveConnection.Name);
         Favorites = HistoryService.GetFavorites(ActiveConnection.Name);
      }

      private void SaveFavorite() {
         HistoryService.SaveFavorite(FmModel.Prompt, ActiveConnection.Name);
         Favorites = HistoryService.GetFavorites(ActiveConnection.Name);
         Snackbar.Add("Saved favorite!", Severity.Success);
      }

      private void EditQuery() {
         RowData = DatabaseService.GetDataTable(ActiveConnection, Query);
         Snackbar.Add("Results updated.", Severity.Success);
      }

      public void LoadDatabase(string dbName) {
         ActiveConnection = DatabaseService.GetAIConnections().FirstOrDefault(x => x.Name == dbName);
         History = HistoryService.GetQueries(ActiveConnection.Name);
         Favorites = HistoryService.GetFavorites(ActiveConnection.Name);
         ClearUI();
      }

      private void ClearUI() {
         Prompt = String.Empty;
         Summary = String.Empty;
         Query = String.Empty;
         Error = String.Empty;
         RowData = new List<List<string>>();
         FmModel = new FormModel();
      }

      public async Task LoadHistoryItem(string query) {
         FmModel.Prompt = query;
         await RunDataChat(query);
      }

      public async Task OnSubmit() {
         await RunDataChat(FmModel.Prompt);
      }

      public async Task RunDataChat(string Prompt) {
         try {
            Loading = true;
            LoadingMessage = "Getting the AI query...";
            var aiResponse = await OpenAIService.GetAISQLQuery(Prompt, ActiveConnection);

            Query = aiResponse.query;
            Summary = aiResponse.summary;

            LoadingMessage = "Running the Database query...";
            if (!Query.ToLower().Contains("delete")) {
               RowData = DatabaseService.GetDataTable(ActiveConnection, aiResponse.query);

               Loading = false;
               HistoryService.SaveQuery(Prompt, ActiveConnection.Name);
               History = HistoryService.GetQueries(ActiveConnection.Name);
               Favorites = HistoryService.GetFavorites(ActiveConnection.Name);
               Error = string.Empty;
            } else {
               Loading = false;
               LoadingMessage = String.Empty;
               Error = "Delete queries are not allowed.";
            }
         } catch (Exception e) {
            Error = e.Message;
            Loading = false;
            LoadingMessage = String.Empty;
         }
      }
      private void FilterTables() {
         if (!string.IsNullOrEmpty(filterText)) {
            ActiveConnection.SchemaStructured = ActiveConnection.SchemaStructured.Where(x => x.TableName.ToLower().Contains(filterText.ToLower())).ToList();
         }
      }
   }
}