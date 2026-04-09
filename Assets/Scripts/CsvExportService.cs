using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
#if WINDOWS_UWP
using Windows.Storage;
#endif

public class CsvExportService
{
    public void WriteCsvRow(
        string fileName,
        string defaultFileName,
        string header,
        string row,
        string logContext,
        string picturesFolderName,
        bool writeUsbVisibleCsvCopyOnNonUwp,
        string persistentDataPath)
    {
        string safeFileName = SanitizeCsvFileName(fileName, defaultFileName);

#if WINDOWS_UWP
        _ = WriteCsvRowToPicturesAsync(
            safeFileName,
            header,
            row,
            logContext,
            picturesFolderName,
            writeUsbVisibleCsvCopyOnNonUwp,
            persistentDataPath);
#else
        TryWriteCsvToPersistent(
            safeFileName,
            header,
            row,
            logContext,
            writeUsbVisibleCsvCopyOnNonUwp,
            persistentDataPath);
#endif
    }

#if WINDOWS_UWP
    async Task WriteCsvRowToPicturesAsync(
        string fileName,
        string header,
        string row,
        string logContext,
        string picturesFolderName,
        bool writeUsbVisibleCsvCopyOnNonUwp,
        string persistentDataPath)
    {
        try
        {
            string folderName = string.IsNullOrWhiteSpace(picturesFolderName)
                ? "GaitRehabFYP_CSV"
                : picturesFolderName.Trim();

            StorageFolder rootFolder = await KnownFolders.PicturesLibrary.CreateFolderAsync(
                folderName,
                CreationCollisionOption.OpenIfExists);
            StorageFile csvFile = await rootFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);

            var properties = await csvFile.GetBasicPropertiesAsync();
            bool shouldWriteHeader = properties.Size == 0;
            if (shouldWriteHeader)
            {
                await FileIO.AppendTextAsync(csvFile, header + Environment.NewLine);
            }

            await FileIO.AppendTextAsync(csvFile, row + Environment.NewLine);
            Debug.Log("[CsvExportService] " + logContext + " CSV written to PicturesLibrary: " + csvFile.Path);

            if (writeUsbVisibleCsvCopyOnNonUwp)
            {
                Debug.Log("[CsvExportService] USB copy toggle ignored on UWP because file is already in PicturesLibrary.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[CsvExportService] " + logContext + " PicturesLibrary CSV write failed: " + ex.Message + " | Falling back to persistent path.");
            TryWriteCsvToPersistent(
                fileName,
                header,
                row,
                logContext,
                writeUsbVisibleCsvCopyOnNonUwp,
                persistentDataPath);
        }
    }
#endif

    void TryWriteCsvToPersistent(
        string fileName,
        string header,
        string row,
        string logContext,
        bool writeUsbVisibleCsvCopyOnNonUwp,
        string persistentDataPath)
    {
        try
        {
            string csvPath = Path.Combine(persistentDataPath, fileName);
            Debug.Log("[CsvExportService] " + logContext + " CSV write target resolved to: " + csvPath + " (persistent root: " + persistentDataPath + ")");
            if (!TryAppendCsvRow(csvPath, header, row, out string persistentError, false))
            {
                Debug.LogWarning("[CsvExportService] Failed to write " + logContext + " CSV to persistent path: " + persistentError);
                return;
            }

            Debug.Log("[CsvExportService] " + logContext + " CSV written to: " + csvPath);

            if (writeUsbVisibleCsvCopyOnNonUwp)
            {
                bool wroteUsbCopy = false;
                foreach (string folder in GetUsbVisibleCandidateFolders(persistentDataPath))
                {
                    string copyPath = Path.Combine(folder, fileName);
                    if (TryAppendCsvRow(copyPath, header, row, out string copyError, true))
                    {
                        Debug.Log("[CsvExportService] USB-visible CSV copy written to: " + copyPath);
                        wroteUsbCopy = true;
                        break;
                    }

                    Debug.Log("[CsvExportService] USB copy attempt failed at: " + copyPath + " | " + copyError);
                }

                if (!wroteUsbCopy)
                {
                    Debug.LogWarning("[CsvExportService] Could not write a USB-visible CSV copy. Use persistent path or Device Portal fallback.");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[CsvExportService] Failed to write " + logContext + " CSV to persistent path: " + ex.Message);
        }
    }

    static bool TryAppendCsvRow(string csvPath, string header, string row, out string error, bool ensureDirectory)
    {
        try
        {
            string normalizedPath = csvPath
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);

            string directory = Path.GetDirectoryName(normalizedPath);
            if (ensureDirectory && !string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            bool fileExists = File.Exists(normalizedPath);
            using (StreamWriter writer = new StreamWriter(normalizedPath, true))
            {
                if (!fileExists)
                {
                    writer.WriteLine(header);
                }

                writer.WriteLine(row);
            }

            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    static IEnumerable<string> GetUsbVisibleCandidateFolders(string persistentDataPath)
    {
        HashSet<string> candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (!string.IsNullOrWhiteSpace(myDocuments))
            candidates.Add(myDocuments);

        string commonDocuments = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
        if (!string.IsNullOrWhiteSpace(commonDocuments))
            candidates.Add(commonDocuments);

        string normalized = (persistentDataPath ?? string.Empty)
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        string appDataMarker = Path.DirectorySeparatorChar + "AppData" + Path.DirectorySeparatorChar + "Local" + Path.DirectorySeparatorChar + "Packages";
        int markerIndex = normalized.IndexOf(appDataMarker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex > 0)
        {
            string userRoot = normalized.Substring(0, markerIndex);
            candidates.Add(Path.Combine(userRoot, "Documents"));
            candidates.Add(Path.Combine(userRoot, "Downloads"));
        }

        return candidates;
    }

    public static string SanitizeCsvFileName(string requestedName, string defaultFileName)
    {
        string fallback = string.IsNullOrWhiteSpace(defaultFileName) ? "data.csv" : defaultFileName.Trim();
        string fileName = string.IsNullOrWhiteSpace(requestedName)
            ? fallback
            : requestedName.Trim();

        fileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = fallback;
        }

        char[] invalidChars = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalidChars.Length; i++)
        {
            fileName = fileName.Replace(invalidChars[i], '_');
        }

        if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            fileName += ".csv";
        }

        return fileName;
    }

    public static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        bool needsQuotes = value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
        if (!needsQuotes)
            return value;

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
