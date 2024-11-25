namespace DBChatPro {
   public class HistoryItem {
      public int Id { get; set; }
      public string? Query { get; set; }
      public string? Prompt { get; set; }
      public string? ConnectionName { get; set; }
   }
}
