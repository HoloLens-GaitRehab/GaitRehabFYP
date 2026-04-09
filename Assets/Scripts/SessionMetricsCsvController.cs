using System;
using System.Globalization;
using UnityEngine;

public class SessionMetricsCsvController
{
    public struct Settings
    {
        public bool writeOnSessionComplete;
        public string fileName;
    }

    private bool hasLoggedCurrentCompletion;

    public void Update(
        WaypointSystemManager waypointManager,
        Settings settings,
        CsvExportService csvExportService,
        string picturesFolderName,
        bool writeUsbVisibleCsvCopyOnNonUwp,
        string persistentDataPath)
    {
        if (waypointManager == null || csvExportService == null)
            return;

        if (waypointManager.IsSessionActive)
        {
            hasLoggedCurrentCompletion = false;
            return;
        }

        if (!settings.writeOnSessionComplete || hasLoggedCurrentCompletion || !waypointManager.IsSessionCompleted)
            return;

        WriteCompletedSessionMetricsCsv(
            waypointManager,
            settings.fileName,
            csvExportService,
            picturesFolderName,
            writeUsbVisibleCsvCopyOnNonUwp,
            persistentDataPath);
        hasLoggedCurrentCompletion = true;
    }

    void WriteCompletedSessionMetricsCsv(
        WaypointSystemManager waypointManager,
        string fileName,
        CsvExportService csvExportService,
        string picturesFolderName,
        bool writeUsbVisibleCsvCopyOnNonUwp,
        string persistentDataPath)
    {
        try
        {
            string header = "timestamp_utc,completion_title,distance_m,elapsed_s,avg_speed_mps,pace_s_per_m,on_course_percent,off_course_percent,off_course_seconds,drift_avg_m,drift_max_m,app_version,unity_version,device_model";
            string row = string.Join(",",
                CsvExportService.EscapeCsv(DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)),
                CsvExportService.EscapeCsv(waypointManager.LastSessionCompletionTitle),
                waypointManager.LastSessionDistanceMeters.ToString("F3", CultureInfo.InvariantCulture),
                waypointManager.LastSessionElapsedSeconds.ToString("F3", CultureInfo.InvariantCulture),
                waypointManager.LastSessionAverageSpeedMps.ToString("F3", CultureInfo.InvariantCulture),
                waypointManager.LastSessionPaceSecondsPerMeter.ToString("F3", CultureInfo.InvariantCulture),
                waypointManager.LastSessionOnCoursePercent.ToString("F2", CultureInfo.InvariantCulture),
                waypointManager.LastSessionOffCoursePercent.ToString("F2", CultureInfo.InvariantCulture),
                waypointManager.LastSessionOffCourseSeconds.ToString("F3", CultureInfo.InvariantCulture),
                waypointManager.LastSessionDriftAverageMeters.ToString("F3", CultureInfo.InvariantCulture),
                waypointManager.LastSessionDriftMaxMeters.ToString("F3", CultureInfo.InvariantCulture),
                CsvExportService.EscapeCsv(Application.version),
                CsvExportService.EscapeCsv(Application.unityVersion),
                CsvExportService.EscapeCsv(SystemInfo.deviceModel));

            csvExportService.WriteCsvRow(
                fileName,
                "session_metrics.csv",
                header,
                row,
                "Session metrics",
                picturesFolderName,
                writeUsbVisibleCsvCopyOnNonUwp,
                persistentDataPath);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[SessionMetricsCsvController] Failed to write session metrics CSV: " + ex.Message);
        }
    }
}
