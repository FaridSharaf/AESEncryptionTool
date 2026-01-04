using System.IO;
using System.Text.Json;
using ConsoleApp1.Models;

namespace ConsoleApp1.Services
{
    public class HistoryManager
    {
        private static readonly string HistoryFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AESEncryptionTool",
            "history.json");

        private static List<HistoryEntry>? _entries;

        /// <summary>
        /// Loads history from file
        /// </summary>
        public static List<HistoryEntry> LoadHistory()
        {
            if (_entries != null)
                return _entries;

            if (!File.Exists(HistoryFile))
            {
                _entries = new List<HistoryEntry>();
                return _entries;
            }

            try
            {
                string json = File.ReadAllText(HistoryFile);
                var data = JsonSerializer.Deserialize<HistoryData>(json);
                _entries = data?.Entries ?? new List<HistoryEntry>();
                return _entries;
            }
            catch
            {
                _entries = new List<HistoryEntry>();
                return _entries;
            }
        }

        /// <summary>
        /// Saves history to file
        /// </summary>
        public static void SaveHistory()
        {
            if (_entries == null)
                return;

            try
            {
                var data = new HistoryData { Entries = _entries };
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                
                string? directory = Path.GetDirectoryName(HistoryFile);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(HistoryFile, json);
            }
            catch
            {
                // Silently fail - history is not critical
            }
        }

        /// <summary>
        /// Adds an entry to history
        /// </summary>
        public static void AddEntry(HistoryEntry entry)
        {
            if (_entries == null)
                _entries = LoadHistory();

            _entries.Insert(0, entry); // Add to beginning
            SaveHistory();
        }

        /// <summary>
        /// Updates an entry in history
        /// </summary>
        public static void UpdateEntry(HistoryEntry entry)
        {
            if (_entries == null)
                _entries = LoadHistory();

            var existing = _entries.FirstOrDefault(e => e.Id == entry.Id);
            if (existing != null)
            {
                existing.Note = entry.Note;
                existing.IsFavorite = entry.IsFavorite;
                SaveHistory();
            }
        }

        /// <summary>
        /// Deletes an entry from history
        /// </summary>
        public static void DeleteEntry(string id)
        {
            if (_entries == null)
                _entries = LoadHistory();

            _entries.RemoveAll(e => e.Id == id);
            SaveHistory();
        }

        /// <summary>
        /// Gets recent items (last N items)
        /// </summary>
        public static List<HistoryEntry> GetRecentItems(int count)
        {
            if (_entries == null)
                _entries = LoadHistory();

            return _entries.Take(count).ToList();
        }

        /// <summary>
        /// Gets favorite items
        /// </summary>
        public static List<HistoryEntry> GetFavorites()
        {
            if (_entries == null)
                _entries = LoadHistory();

            return _entries.Where(e => e.IsFavorite).ToList();
        }

        /// <summary>
        /// Searches history by input, output, or note
        /// </summary>
        public static List<HistoryEntry> SearchHistory(string searchText)
        {
            if (_entries == null)
                _entries = LoadHistory();

            if (string.IsNullOrWhiteSpace(searchText))
                return _entries;

            string lowerSearch = searchText.ToLower();
            return _entries.Where(e =>
                e.Input.ToLower().Contains(lowerSearch) ||
                e.Output.ToLower().Contains(lowerSearch) ||
                e.Note.ToLower().Contains(lowerSearch)
            ).ToList();
        }

        /// <summary>
        /// Clears all history
        /// </summary>
        public static void ClearHistory()
        {
            _entries = new List<HistoryEntry>();
            SaveHistory();
        }

        private class HistoryData
        {
            public List<HistoryEntry> Entries { get; set; } = new();
        }
    }
}

