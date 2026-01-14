using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Win32;
using AESCryptoTool.Models;

namespace AESCryptoTool.Services
{
    using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
    using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
    
    public class BackendData
    {
        public string Version { get; set; } = "1.0";
        public DateTime ExportDate { get; set; } = DateTime.Now;
        public List<HistoryEntry> History { get; set; } = new List<HistoryEntry>();
        public List<HistoryEntry> Bookmarks { get; set; } = new List<HistoryEntry>();
    }

    public static class ImportExportService
    {
        public static async Task<bool> ExportDataAsync(bool includeHistory, bool includeBookmarks)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                FileName = $"AESCryptoTool_Backup_{DateTime.Now:yyyyMMdd}.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var data = new BackendData();

                if (includeHistory)
                {
                    data.History = HistoryManager.LoadHistory() ?? new List<HistoryEntry>();
                }

                if (includeBookmarks)
                {
                    data.Bookmarks = HistoryManager.GetBookmarks() ?? new List<HistoryEntry>();
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(data, options);

                await File.WriteAllTextAsync(saveFileDialog.FileName, json);
                return true;
            }
            return false;
        }

        public static async Task<int> ImportDataAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(openFileDialog.FileName);
                    var data = JsonSerializer.Deserialize<BackendData>(json);

                    if (data == null) return 0;

                    int count = 0;

                    // Import Bookmarks
                    if (data.Bookmarks != null && data.Bookmarks.Count > 0)
                    {
                        var currentBookmarks = HistoryManager.GetBookmarks();
                        var newBookmarks = new List<HistoryEntry>();
                        
                        foreach (var item in data.Bookmarks)
                        {
                            // Avoid duplicates by ID
                            if (!currentBookmarks.Any(b => b.Id == item.Id))
                            {
                                item.IsFavorite = true; 
                                newBookmarks.Add(item);
                                count++;
                            }
                        }
                        
                        if (newBookmarks.Count > 0)
                        {
                            HistoryManager.ImportBookmarks(newBookmarks);
                        }
                    }

                    // Import History
                    if (data.History != null && data.History.Count > 0)
                    {
                        var currentHistory = HistoryManager.LoadHistory();
                        var newHistory = new List<HistoryEntry>();
                        
                        foreach (var item in data.History)
                        {
                            if (!currentHistory.Any(h => h.Id == item.Id))
                            {
                                newHistory.Add(item);
                                count++;
                            }
                        }
                        
                        if (newHistory.Count > 0)
                        {
                            HistoryManager.ImportHistory(newHistory);
                        }
                    }

                    return count;
                }
                catch (Exception)
                {
                    // Log error or rethrow
                    return -1;
                }
            }
            return 0; // Cancelled
        }
    }
}
