using System;
using UnityEngine;

public class StartupCsvController
{
    public struct Settings
    {
        public bool writeOnLaunch;
        public string fileName;
    }

    public void WriteStartupSampleCsv(
        Settings settings,
        CsvExportService csvExportService,
        string picturesFolderName,
        bool writeUsbVisibleCsvCopyOnNonUwp,
        string persistentDataPath)
    {
        if (!settings.writeOnLaunch || csvExportService == null)
            return;

        try
        {
            string header = "timestamp_utc,device_model,app_version,unity_version,note";
            string row = string.Format(
                "{0},{1},{2},{3},{4}",
                CsvExportService.EscapeCsv(DateTime.UtcNow.ToString("O")),
                CsvExportService.EscapeCsv(SystemInfo.deviceModel),
                CsvExportService.EscapeCsv(Application.version),
                CsvExportService.EscapeCsv(Application.unityVersion),
                CsvExportService.EscapeCsv("startup_test"));

            csvExportService.WriteCsvRow(
                settings.fileName,
                "startup_usb_test.csv",
                header,
                row,
                "Startup sample",
                picturesFolderName,
                writeUsbVisibleCsvCopyOnNonUwp,
                persistentDataPath);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[StartupCsvController] Failed to write startup sample CSV: " + ex.Message);
        }
    }
}
