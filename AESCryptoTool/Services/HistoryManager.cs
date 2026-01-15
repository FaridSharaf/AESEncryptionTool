using System.IO;
using System.Text.Json;
using AESCryptoTool.Models;

namespace AESCryptoTool.Services
{
    public class HistoryManager
    {
        private static string _dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AESEncryptionTool");

        // Allow tests to override the directory
        public static void SetDataDirectory(string path)
        {
            _dataDirectory = path;
            _historyEntries = null; // Reset cache
            _bookmarkEntries = null;
        }

        private static string HistoryFile => Path.Combine(_dataDirectory, "history.json");
        private static string BookmarksFile => Path.Combine(_dataDirectory, "bookmarks.json");

        private static List<HistoryEntry>? _historyEntries;
        private static List<HistoryEntry>? _bookmarkEntries;

        public static void ImportHistory(List<HistoryEntry> items)
        {
            if (_historyEntries == null) _historyEntries = LoadHistory();
            _historyEntries.AddRange(items);
            // Sort by Date descending
            _historyEntries = _historyEntries.OrderByDescending(x => x.Timestamp).ToList();
            SaveHistory();
        }

        public static void ImportBookmarks(List<HistoryEntry> items)
        {
            if (_bookmarkEntries == null) _bookmarkEntries = LoadBookmarks();
            _bookmarkEntries.AddRange(items);
            // Sort by Date descending
            _bookmarkEntries = _bookmarkEntries.OrderByDescending(x => x.Timestamp).ToList();
            SaveBookmarks();
        }

        // Helper for ImportService
        public static List<HistoryEntry> GetBookmarks()
        {
             if (_bookmarkEntries == null) _bookmarkEntries = LoadBookmarks();
             return _bookmarkEntries;
        }

        #region History Methods

        /// <summary>
        /// Loads history from file (ALL items including bookmarked)
        /// </summary>
        public static List<HistoryEntry> LoadHistory()
        {
            if (_historyEntries != null)
                return _historyEntries;

            _historyEntries = LoadFromFile(HistoryFile);
            return _historyEntries;
        }

        /// <summary>
        /// Saves history to file
        /// </summary>
        public static void SaveHistory()
        {
            SaveToFile(HistoryFile, _historyEntries);
        }

        /// <summary>
        /// Adds an entry to history (and bookmarks if IsFavorite)
        /// </summary>
        public static void AddEntry(HistoryEntry entry)
        {
            if (_historyEntries == null)
                _historyEntries = LoadHistory();

            _historyEntries.Insert(0, entry);
            SaveHistory();

            // If marked as favorite, also add to bookmarks
            if (entry.IsFavorite)
            {
                if (_bookmarkEntries == null)
                    _bookmarkEntries = LoadBookmarks();

                var bookmarkCopy = new HistoryEntry
                {
                    Id = entry.Id,
                    Input = entry.Input,
                    Output = entry.Output,
                    Operation = entry.Operation,
                    Timestamp = entry.Timestamp,
                    Note = entry.Note,
                    IsFavorite = true
                };
                _bookmarkEntries.Insert(0, bookmarkCopy);
                SaveBookmarks();
            }
        }

        /// <summary>
        /// Updates an entry - handles bookmark toggle
        /// </summary>
        public static void UpdateEntry(HistoryEntry entry)
        {
            if (_historyEntries == null)
                _historyEntries = LoadHistory();
            if (_bookmarkEntries == null)
                _bookmarkEntries = LoadBookmarks();

            // Find entry in history
            var historyItem = _historyEntries.FirstOrDefault(e => e.Id == entry.Id);
            if (historyItem != null)
            {
                historyItem.Note = entry.Note;
                historyItem.IsFavorite = entry.IsFavorite;
                SaveHistory();

                // If bookmarked, add to bookmarks file (copy)
                if (entry.IsFavorite)
                {
                    // Check if already in bookmarks
                    var existingBookmark = _bookmarkEntries.FirstOrDefault(e => e.Id == entry.Id);
                    if (existingBookmark == null)
                    {
                        // Add a copy to bookmarks
                        var bookmarkCopy = new HistoryEntry
                        {
                            Id = entry.Id,
                            Input = historyItem.Input,
                            Output = historyItem.Output,
                            Operation = historyItem.Operation,
                            Timestamp = historyItem.Timestamp,
                            Note = historyItem.Note,
                            IsFavorite = true
                        };
                        _bookmarkEntries.Insert(0, bookmarkCopy);
                        SaveBookmarks();
                    }
                    else
                    {
                        // Update existing bookmark
                        existingBookmark.Note = entry.Note;
                        SaveBookmarks();
                    }
                }
                else
                {
                    // Remove from bookmarks if unmarked
                    _bookmarkEntries.RemoveAll(e => e.Id == entry.Id);
                    SaveBookmarks();
                }
                return;
            }

            // Entry might only be in bookmarks (edge case)
            var bookmarkItem = _bookmarkEntries.FirstOrDefault(e => e.Id == entry.Id);
            if (bookmarkItem != null)
            {
                if (!entry.IsFavorite)
                {
                    // Remove from bookmarks
                    _bookmarkEntries.Remove(bookmarkItem);
                    SaveBookmarks();
                }
                else
                {
                    bookmarkItem.Note = entry.Note;
                    SaveBookmarks();
                }
            }
        }

        /// <summary>
        /// Deletes an entry ONLY from history (preserves bookmarks)
        /// </summary>
        public static void DeleteFromHistory(string id)
        {
            if (_historyEntries == null)
                _historyEntries = LoadHistory();
            
            _historyEntries.RemoveAll(e => e.Id == id);
            SaveHistory();
        }

        /// <summary>
        /// Deletes an entry ONLY from bookmarks (updates history status)
        /// </summary>
        public static void DeleteFromBookmarks(string id)
        {
            if (_historyEntries == null)
                _historyEntries = LoadHistory();
            if (_bookmarkEntries == null)
                _bookmarkEntries = LoadBookmarks();

            // Remove from bookmarks
            _bookmarkEntries.RemoveAll(e => e.Id == id);
            SaveBookmarks();

            // Update history entry if it exists
            var historyItem = _historyEntries.FirstOrDefault(e => e.Id == id);
            if (historyItem != null)
            {
                historyItem.IsFavorite = false;
                SaveHistory();
            }
        }

        /// <summary>
        /// Deletes multiple entries from history
        /// </summary>
        public static void DeleteHistoryEntries(IEnumerable<string> ids)
        {
            if (_historyEntries == null) _historyEntries = LoadHistory();
            
            var idSet = new HashSet<string>(ids);
            _historyEntries.RemoveAll(e => idSet.Contains(e.Id));
            SaveHistory();
        }

        /// <summary>
        /// Deletes multiple entries from bookmarks
        /// </summary>
        public static void DeleteBookmarkEntries(IEnumerable<string> ids)
        {
            if (_historyEntries == null) _historyEntries = LoadHistory();
            if (_bookmarkEntries == null) _bookmarkEntries = LoadBookmarks();

            var idSet = new HashSet<string>(ids);
            
            // Remove from bookmarks
            _bookmarkEntries.RemoveAll(e => idSet.Contains(e.Id));
            SaveBookmarks();

            // Update history entries to remove favorite status
            foreach (var id in idSet)
            {
                var historyItem = _historyEntries.FirstOrDefault(e => e.Id == id);
                if (historyItem != null)
                {
                    historyItem.IsFavorite = false;
                }
            }
            SaveHistory();
        }

        /// <summary>
        /// Gets recent items (from history)
        /// </summary>
        public static List<HistoryEntry> GetRecentItems(int count)
        {
            if (_historyEntries == null)
                _historyEntries = LoadHistory();

            return _historyEntries.Take(count).ToList();
        }

        /// <summary>
        /// Clears all history (but preserves bookmarks file)
        /// </summary>
        public static void ClearHistory()
        {
            if (_historyEntries == null)
                _historyEntries = LoadHistory();

            // Keep bookmarked items in history
            _historyEntries.RemoveAll(e => !e.IsFavorite);
            SaveHistory();
        }

        /// <summary>
        /// Enforces max limit on history entries
        /// </summary>
        public static void EnforceHistoryLimit(int maxHistory)
        {
            if (_historyEntries == null)
                _historyEntries = LoadHistory();

            if (_historyEntries.Count > maxHistory)
            {
                // Keep bookmarked items, trim non-bookmarked
                var bookmarked = _historyEntries.Where(e => e.IsFavorite).ToList();
                var nonBookmarked = _historyEntries.Where(e => !e.IsFavorite).ToList();

                if (nonBookmarked.Count > maxHistory)
                {
                    nonBookmarked = nonBookmarked.Take(maxHistory).ToList();
                }

                _historyEntries = bookmarked.Concat(nonBookmarked)
                    .OrderByDescending(e => e.Timestamp)
                    .ToList();
                SaveHistory();
            }
        }

        #endregion

        #region Bookmark Methods

        /// <summary>
        /// Loads bookmarks from separate file
        /// </summary>
        public static List<HistoryEntry> LoadBookmarks()
        {
            if (_bookmarkEntries != null)
                return _bookmarkEntries;

            _bookmarkEntries = LoadFromFile(BookmarksFile);
            foreach (var entry in _bookmarkEntries)
            {
                entry.IsFavorite = true;
            }
            return _bookmarkEntries;
        }

        /// <summary>
        /// Saves bookmarks to file
        /// </summary>
        public static void SaveBookmarks()
        {
            SaveToFile(BookmarksFile, _bookmarkEntries);
        }

        /// <summary>
        /// Gets all bookmarks
        /// </summary>
        public static List<HistoryEntry> GetFavorites()
        {
            return LoadBookmarks();
        }

        /// <summary>
        /// Clears all bookmarks
        /// </summary>
        public static void ClearBookmarks()
        {
            if (_historyEntries == null)
                _historyEntries = LoadHistory();

            // Unmark all favorites in history
            foreach (var entry in _historyEntries.Where(e => e.IsFavorite))
            {
                entry.IsFavorite = false;
            }
            SaveHistory();

            // Clear bookmarks file
            _bookmarkEntries = new List<HistoryEntry>();
            SaveBookmarks();
        }

        /// <summary>
        /// Enforces max limit on bookmarks
        /// </summary>
        public static void EnforceBookmarkLimit(int maxBookmarks)
        {
            if (_bookmarkEntries == null)
                _bookmarkEntries = LoadBookmarks();

            if (_bookmarkEntries.Count > maxBookmarks)
            {
                _bookmarkEntries = _bookmarkEntries.Take(maxBookmarks).ToList();
                SaveBookmarks();
            }
        }

        #endregion

        #region Search Methods

        /// <summary>
        /// Searches history by input, output, or note
        /// </summary>
        public static List<HistoryEntry> SearchHistory(string searchText)
        {
            if (_historyEntries == null)
                _historyEntries = LoadHistory();

            if (string.IsNullOrWhiteSpace(searchText))
                return _historyEntries;

            string lowerSearch = searchText.ToLower();
            return _historyEntries.Where(e =>
                e.Input.ToLower().Contains(lowerSearch) ||
                e.Output.ToLower().Contains(lowerSearch) ||
                e.Note.ToLower().Contains(lowerSearch)
            ).ToList();
        }

        /// <summary>
        /// Searches bookmarks by input, output, or note
        /// </summary>
        public static List<HistoryEntry> SearchBookmarks(string searchText)
        {
            if (_bookmarkEntries == null)
                _bookmarkEntries = LoadBookmarks();

            if (string.IsNullOrWhiteSpace(searchText))
                return _bookmarkEntries;

            string lowerSearch = searchText.ToLower();
            return _bookmarkEntries.Where(e =>
                e.Input.ToLower().Contains(lowerSearch) ||
                e.Output.ToLower().Contains(lowerSearch) ||
                e.Note.ToLower().Contains(lowerSearch)
            ).ToList();
        }

        #endregion

        #region Helper Methods

        private static List<HistoryEntry> LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return new List<HistoryEntry>();

            try
            {
                string json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<HistoryData>(json);
                return data?.Entries ?? new List<HistoryEntry>();
            }
            catch
            {
                return new List<HistoryEntry>();
            }
        }

        private static void SaveToFile(string filePath, List<HistoryEntry>? entries)
        {
            if (entries == null)
                return;

            try
            {
                var data = new HistoryData { Entries = entries };
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

                if (!Directory.Exists(_dataDirectory))
                {
                    Directory.CreateDirectory(_dataDirectory);
                }

                File.WriteAllText(filePath, json);
            }
            catch
            {
                // Silently fail
            }
        }

        private class HistoryData
        {
            public List<HistoryEntry> Entries { get; set; } = new();
        }

        #endregion
    }
}
