using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace AESCryptoTool.Services
{
    public class BatchProcessor
    {
        public event Action<int, int, string>? ProgressChanged;

        private readonly string _key;
        private readonly string _iv;

        public BatchProcessor(string key, string iv)
        {
            _key = key;
            _iv = iv;
        }

        /// <summary>
        /// Processes a file (XLSX or CSV) and returns the output path
        /// </summary>
        public async Task<BatchResult> ProcessFileAsync(
            string inputPath,
            string outputPath,
            string columnPattern,
            bool hasHeader,
            bool encrypt,
            CancellationToken cancellationToken = default)
        {
            var result = new BatchResult { InputPath = inputPath, OutputPath = outputPath };
            var extension = Path.GetExtension(inputPath).ToLower();

            try
            {
                if (extension == ".xlsx")
                {
                    await ProcessXlsxAsync(inputPath, outputPath, columnPattern, hasHeader, encrypt, result, cancellationToken);
                }
                else if (extension == ".csv")
                {
                    await ProcessCsvAsync(inputPath, outputPath, columnPattern, hasHeader, encrypt, result, cancellationToken);
                }
                else
                {
                    result.ErrorMessage = $"Unsupported file format: {extension}";
                }
            }
            catch (OperationCanceledException)
            {
                result.ErrorMessage = "Operation cancelled";
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private async Task ProcessXlsxAsync(
            string inputPath,
            string outputPath,
            string columnPattern,
            bool hasHeader,
            bool encrypt,
            BatchResult result,
            CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                using var workbook = new XLWorkbook(inputPath);
                var worksheet = workbook.Worksheets.First();

                int startRow = hasHeader ? 2 : 1;
                int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
                int totalRows = lastRow - startRow + 1;

                // Find target column
                int targetColumn = FindColumnByPattern(worksheet, columnPattern, hasHeader);
                if (targetColumn == -1)
                {
                    result.ErrorMessage = $"Column matching pattern '{columnPattern}' not found";
                    return;
                }

                result.TargetColumn = hasHeader 
                    ? worksheet.Cell(1, targetColumn).GetString() 
                    : $"Column {targetColumn}";

                // Add output column
                int outputColumn = worksheet.LastColumnUsed()?.ColumnNumber() + 1 ?? targetColumn + 1;
                string outputColumnName = $"{result.TargetColumn}_{(encrypt ? "Encrypted" : "Decrypted")}";
                
                if (hasHeader)
                {
                    worksheet.Cell(1, outputColumn).Value = outputColumnName;
                }

                // Process rows
                for (int row = startRow; row <= lastRow; row++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var cell = worksheet.Cell(row, targetColumn);
                    string value = cell.GetString();

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        result.SkippedRows++;
                        continue;
                    }

                    try
                    {
                        string processedValue = encrypt
                            ? AESCryptography.Encrypt(value, _key, _iv)
                            : AESCryptography.Decrypt(value, _key, _iv);

                        worksheet.Cell(row, outputColumn).Value = processedValue;
                        result.ProcessedRows++;
                    }
                    catch
                    {
                        result.FailedRows++;
                        worksheet.Cell(row, outputColumn).Value = "[ERROR]";
                    }

                    int currentRow = row - startRow + 1;
                    ProgressChanged?.Invoke(currentRow, totalRows, value);
                }

                workbook.SaveAs(outputPath);
                result.TotalRows = totalRows;

            }, cancellationToken);
        }

        private async Task ProcessCsvAsync(
            string inputPath,
            string outputPath,
            string columnPattern,
            bool hasHeader,
            bool encrypt,
            BatchResult result,
            CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = hasHeader,
                };

                var allRecords = new List<Dictionary<string, string>>();
                string[]? headers = null;
                int targetColumnIndex = -1;
                string targetColumnName = "";

                // Read all records
                using (var reader = new StreamReader(inputPath))
                using (var csv = new CsvReader(reader, config))
                {
                    if (hasHeader)
                    {
                        csv.Read();
                        csv.ReadHeader();
                        headers = csv.HeaderRecord;

                        if (headers != null)
                        {
                            // Find target column by pattern
                            var regex = new Regex(columnPattern, RegexOptions.IgnoreCase);
                            for (int i = 0; i < headers.Length; i++)
                            {
                                if (regex.IsMatch(headers[i]))
                                {
                                    targetColumnIndex = i;
                                    targetColumnName = headers[i];
                                    break;
                                }
                            }
                        }

                        if (targetColumnIndex == -1)
                        {
                            result.ErrorMessage = $"Column matching pattern '{columnPattern}' not found";
                            return;
                        }
                    }
                    else
                    {
                        targetColumnIndex = 0; // Default to first column
                        targetColumnName = "Column1";
                    }

                    result.TargetColumn = targetColumnName;

                    while (csv.Read())
                    {
                        var record = new Dictionary<string, string>();
                        if (hasHeader && headers != null)
                        {
                            foreach (var header in headers)
                            {
                                record[header] = csv.GetField(header) ?? "";
                            }
                        }
                        else
                        {
                            for (int i = 0; i < csv.Parser.Count; i++)
                            {
                                record[$"Column{i + 1}"] = csv.GetField(i) ?? "";
                            }
                        }
                        allRecords.Add(record);
                    }
                }

                if (allRecords.Count == 0)
                {
                    result.ErrorMessage = "No records found in file. Check if file is empty or format is correct.";
                    return;
                }

                int totalRows = allRecords.Count;
                string outputColumnName = $"{targetColumnName}_{(encrypt ? "Encrypted" : "Decrypted")}";

                // Process and add output column
                for (int i = 0; i < allRecords.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var record = allRecords[i];
                    
                    if (record.ContainsKey(targetColumnName))
                    {
                         string value = record[targetColumnName];

                        if (string.IsNullOrWhiteSpace(value))
                        {
                            record[outputColumnName] = "";
                            result.SkippedRows++;
                            if (result.SkippedRows == 1) // First skip debug
                            {
                                System.Diagnostics.Debug.WriteLine($"Skipped row {i} - empty value");
                            }
                        }
                        else
                        {
                            try
                            {
                                string processedValue = encrypt
                                    ? AESCryptography.Encrypt(value, _key, _iv)
                                    : AESCryptography.Decrypt(value, _key, _iv);

                                record[outputColumnName] = processedValue;
                                result.ProcessedRows++;
                            }
                            catch
                            {
                                record[outputColumnName] = "[ERROR]";
                                result.FailedRows++;
                            }
                        }
                    }
                    else
                    {
                        // Should technically not happen if record construction logic is sound
                        result.FailedRows++;
                    }

                    ProgressChanged?.Invoke(i + 1, totalRows, record.ContainsKey(targetColumnName) ? record[targetColumnName] : "");
                }

                // Write output
                using (var writer = new StreamWriter(outputPath))
                using (var csv = new CsvWriter(writer, config))
                {
                    if (allRecords.Count > 0)
                    {
                        var allHeaders = allRecords[0].Keys.ToList();
                        foreach (var header in allHeaders)
                        {
                            csv.WriteField(header);
                        }
                        csv.NextRecord();

                        foreach (var record in allRecords)
                        {
                            foreach (var header in allHeaders)
                            {
                                csv.WriteField(record[header] ?? "");
                            }
                            csv.NextRecord();
                        }
                    }
                    writer.Flush();
                }

                result.TotalRows = totalRows;

            }, cancellationToken);
        }

        private int FindColumnByPattern(IXLWorksheet worksheet, string pattern, bool hasHeader)
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);

            if (hasHeader)
            {
                var headerRow = worksheet.Row(1);
                int lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 1;

                for (int col = 1; col <= lastColumn; col++)
                {
                    string headerValue = worksheet.Cell(1, col).GetString();
                    if (regex.IsMatch(headerValue))
                    {
                        return col;
                    }
                }
            }
            else
            {
                // No header - return first column by default
                return 1;
            }

            return -1;
        }

        /// <summary>
        /// Gets headers from a file for column selection
        /// </summary>
        public static List<string> GetFileHeaders(string filePath, bool hasHeader)
        {
            var headers = new List<string>();
            var extension = Path.GetExtension(filePath).ToLower();

            try
            {
                if (extension == ".xlsx")
                {
                    using var workbook = new XLWorkbook(filePath);
                    var worksheet = workbook.Worksheets.First();
                    int lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 1;

                    for (int col = 1; col <= lastColumn; col++)
                    {
                        string value = hasHeader 
                            ? worksheet.Cell(1, col).GetString() 
                            : $"Column {col}";
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            headers.Add(value);
                        }
                    }
                }
                else if (extension == ".csv")
                {
                    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = hasHeader,
                    };

                    using var reader = new StreamReader(filePath);
                    using var csv = new CsvReader(reader, config);

                    if (hasHeader)
                    {
                        csv.Read();
                        csv.ReadHeader();
                        headers.AddRange(csv.HeaderRecord ?? Array.Empty<string>());
                    }
                    else
                    {
                        csv.Read();
                        for (int i = 0; i < csv.Parser.Count; i++)
                        {
                            headers.Add($"Column {i + 1}");
                        }
                    }
                }
            }
            catch
            {
                // Return empty list on error
            }

            return headers;
        }

        /// <summary>
        /// Gets the first non-empty value from a column for auto-detection
        /// </summary>
        public static string? GetFirstValue(string filePath, string columnName, bool hasHeader)
        {
            var extension = Path.GetExtension(filePath).ToLower();

            try
            {
                if (extension == ".xlsx")
                {
                    using var workbook = new XLWorkbook(filePath);
                    var worksheet = workbook.Worksheets.First();
                    int lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 1;

                    // Find column index
                    int targetCol = -1;
                    if (hasHeader)
                    {
                        for (int col = 1; col <= lastColumn; col++)
                        {
                            if (worksheet.Cell(1, col).GetString() == columnName)
                            {
                                targetCol = col;
                                break;
                            }
                        }
                    }
                    else
                    {
                        // Parse column number from "Column X"
                        if (columnName.StartsWith("Column ") && int.TryParse(columnName.Substring(7), out int colNum))
                        {
                            targetCol = colNum;
                        }
                        else
                        {
                            targetCol = 1;
                        }
                    }

                    if (targetCol > 0)
                    {
                        int startRow = hasHeader ? 2 : 1;
                        int lastRow = Math.Min(startRow + 10, worksheet.LastRowUsed()?.RowNumber() ?? 1);
                        
                        for (int row = startRow; row <= lastRow; row++)
                        {
                            string value = worksheet.Cell(row, targetCol).GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                return value;
                            }
                        }
                    }
                }
                else if (extension == ".csv")
                {
                    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = hasHeader,
                    };

                    using var reader = new StreamReader(filePath);
                    using var csv = new CsvReader(reader, config);

                    if (hasHeader)
                    {
                        csv.Read();
                        csv.ReadHeader();
                    }

                    int? targetIndex = null;
                    if (!hasHeader)
                    {
                         // Parse column number from "Column X" for index (0-based)
                        if (columnName.StartsWith("Column ") && int.TryParse(columnName.Substring(7), out int colNum))
                        {
                            targetIndex = colNum - 1;
                        }
                        else
                        {
                            targetIndex = 0;
                        }
                    }

                    while (csv.Read())
                    {
                        string value;
                        if (targetIndex.HasValue)
                        {
                            value = csv.GetField(targetIndex.Value) ?? "";
                        }
                        else
                        {
                            value = csv.GetField(columnName) ?? "";
                        }

                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            return value;
                        }
                    }
                }
            }
            catch
            {
                // Return null on error
            }

            return null;
        }

        /// <summary>
        /// Gets the first N values from a column for preview
        /// </summary>
        public static List<string> GetPreviewRows(string filePath, string columnName, bool hasHeader, int count = 5)
        {
            var values = new List<string>();
            var extension = Path.GetExtension(filePath).ToLower();

            try
            {
                if (extension == ".xlsx")
                {
                    using var workbook = new XLWorkbook(filePath);
                    var worksheet = workbook.Worksheets.First();
                    int lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 1;

                    // Find column index
                    int targetCol = -1;
                    if (hasHeader)
                    {
                        for (int col = 1; col <= lastColumn; col++)
                        {
                            if (worksheet.Cell(1, col).GetString() == columnName)
                            {
                                targetCol = col;
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (columnName.StartsWith("Column ") && int.TryParse(columnName.Substring(7), out int colNum))
                        {
                            targetCol = colNum;
                        }
                        else
                        {
                            targetCol = 1;
                        }
                    }

                    if (targetCol > 0)
                    {
                        int startRow = hasHeader ? 2 : 1;
                        int lastRow = Math.Min(startRow + count - 1, worksheet.LastRowUsed()?.RowNumber() ?? 1);
                        
                        for (int row = startRow; row <= lastRow; row++)
                        {
                            string value = worksheet.Cell(row, targetCol).GetString();
                            values.Add(value);
                        }
                    }
                }
                else if (extension == ".csv")
                {
                    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = hasHeader,
                    };

                    using var reader = new StreamReader(filePath);
                    using var csv = new CsvReader(reader, config);

                    if (hasHeader)
                    {
                        csv.Read();
                        csv.ReadHeader();
                    }

                    int? targetIndex = null;
                    if (!hasHeader)
                    {
                        if (columnName.StartsWith("Column ") && int.TryParse(columnName.Substring(7), out int colNum))
                        {
                            targetIndex = colNum - 1;
                        }
                        else
                        {
                            targetIndex = 0;
                        }
                    }

                    int rowsRead = 0;
                    while (csv.Read() && rowsRead < count)
                    {
                        string value;
                        if (targetIndex.HasValue)
                        {
                            value = csv.GetField(targetIndex.Value) ?? "";
                        }
                        else
                        {
                            value = csv.GetField(columnName) ?? "";
                        }
                        values.Add(value);
                        rowsRead++;
                    }
                }
            }
            catch
            {
                // Return empty list on error
            }

            return values;
        }

        /// <summary>
        /// Gets the total row count for a file (excluding header if applicable)
        /// </summary>
        public static int GetRowCount(string filePath, bool hasHeader)
        {
            var extension = Path.GetExtension(filePath).ToLower();

            try
            {
                if (extension == ".xlsx")
                {
                    using var workbook = new XLWorkbook(filePath);
                    var worksheet = workbook.Worksheets.First();
                    int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                    return hasHeader ? Math.Max(0, lastRow - 1) : lastRow;
                }
                else if (extension == ".csv")
                {
                    int count = 0;
                    using var reader = new StreamReader(filePath);
                    while (reader.ReadLine() != null)
                    {
                        count++;
                    }
                    return hasHeader ? Math.Max(0, count - 1) : count;
                }
            }
            catch
            {
                // Return 0 on error
            }

            return 0;
        }
    }

    public class BatchResult
    {
        public string InputPath { get; set; } = "";
        public string OutputPath { get; set; } = "";
        public string TargetColumn { get; set; } = "";
        public int TotalRows { get; set; }
        public int ProcessedRows { get; set; }
        public int FailedRows { get; set; }
        public int SkippedRows { get; set; }
        public string? ErrorMessage { get; set; }

        public bool Success => string.IsNullOrEmpty(ErrorMessage);
        public TimeSpan Duration { get; set; }
    }
}
