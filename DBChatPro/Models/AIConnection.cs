namespace DBChatPro
{
    public class AIConnection
    {
        public required string ConnectionString { get; set; }
        public required string Name { get; set; }
        public List<TableSchema>? SchemaStructured { get; set; }
        public List<string>? SchemaRaw { get; set; }
        public string? ExtraInformation { get; set; }
    }
}
