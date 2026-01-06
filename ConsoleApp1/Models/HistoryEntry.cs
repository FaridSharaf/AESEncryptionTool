using System;

namespace ConsoleApp1.Models
{
    public class HistoryEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Operation { get; set; } = string.Empty; // "encrypt" or "decrypt"
        public string Input { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public bool IsFavorite { get; set; } = false;
    }
}



