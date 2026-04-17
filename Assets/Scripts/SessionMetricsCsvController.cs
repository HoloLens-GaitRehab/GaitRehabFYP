using System;
using System.Globalization;
using UnityEngine;

public class SessionMetricsCsvController
{
    public struct Settings
    {
        public bool writeOnSessionComplete;
        public string fileName;
        public bool writeSampleDataOnLaunch;
        public int sampleRowCount;
    }

    private bool hasLoggedCurrentCompletion;
    private bool hasWrittenSampleDataOnLaunch;

    public void WriteSampleDatasetOnLaunch(
        Settings settings,
        CsvExportService csvExportService,
        string picturesFolderName,
        bool writeUsbVisibleCsvCopyOnNonUwp,
        string persistentDataPath,
        Action<string, string, string> onCsvRowWritten = null)
    {
        if (!settings.writeSampleDataOnLaunch || hasWrittenSampleDataOnLaunch || csvExportService == null)
            return;

        int rowCount = Mathf.Clamp(settings.sampleRowCount, 1, 120);
        DateTime baseTime = DateTime.UtcNow;
        System.Random rng = new System.Random(baseTime.Millisecond + rowCount * 13);

        string header = "timestamp_utc,completion_title,distance_m,elapsed_s,avg_speed_mps,pace_s_per_m,on_course_percent,off_course_percent,off_course_seconds,drift_avg_m,drift_max_m,app_version,unity_version,device_model";

        for (int i = 0; i < rowCount; i++)
        {
            float trend = rowCount <= 1 ? 0f : (float)i / (rowCount - 1);
            float jitter = (float)(rng.NextDouble() * 2.0 - 1.0);

            float elapsed = Mathf.Max(15f, 42f + (trend * 16f) + (jitter * 3.5f));
            float distance = Mathf.Max(3f, 9f + (trend * 8f) + (jitter * 0.8f));
            float avgSpeed = distance / elapsed;
            float pace = elapsed / Mathf.Max(0.001f, distance);

            float offCoursePercent = Mathf.Clamp(18f - (trend * 12f) + (jitter * 4f), 1f, 55f);
            float onCoursePercent = Mathf.Clamp(100f - offCoursePercent, 0f, 100f);
            float offCourseSeconds = elapsed * offCoursePercent / 100f;

            float driftAvg = Mathf.Clamp(0.19f - (trend * 0.07f) + (jitter * 0.03f), 0.02f, 0.45f);
            float driftMax = Mathf.Max(driftAvg + 0.04f, driftAvg * 1.7f + Mathf.Abs(jitter) * 0.06f);

            string completionTitle = i % 7 == 0 ? "Session ended early." : "Session complete!";
            string timestamp = baseTime.AddMinutes(-(rowCount - i) * 5).ToString("O", CultureInfo.InvariantCulture);

            string row = string.Join(",",
                CsvExportService.EscapeCsv(timestamp),
                CsvExportService.EscapeCsv(completionTitle),
                distance.ToString("F3", CultureInfo.InvariantCulture),
                elapsed.ToString("F3", CultureInfo.InvariantCulture),
                avgSpeed.ToString("F3", CultureInfo.InvariantCulture),
                pace.ToString("F3", CultureInfo.InvariantCulture),
                onCoursePercent.ToString("F2", CultureInfo.InvariantCulture),
                offCoursePercent.ToString("F2", CultureInfo.InvariantCulture),
                offCourseSeconds.ToString("F3", CultureInfo.InvariantCulture),
                driftAvg.ToString("F3", CultureInfo.InvariantCulture),
                driftMax.ToString("F3", CultureInfo.InvariantCulture),
                CsvExportService.EscapeCsv(Application.version),
                CsvExportService.EscapeCsv(Application.unityVersion),
                CsvExportService.EscapeCsv(SystemInfo.deviceModel));

            csvExportService.WriteCsvRow(
                settings.fileName,
                "session_metrics.csv",
                header,
                row,
                "Sample session metrics",
                picturesFolderName,
                writeUsbVisibleCsvCopyOnNonUwp,
                persistentDataPath);

            if (onCsvRowWritten != null)
            {
                onCsvRowWritten(
                    CsvExportService.SanitizeCsvFileName(settings.fileName, "session_metrics.csv"),
                    header,
                    row);
            }
        }

        hasWrittenSampleDataOnLaunch = true;
    }

    public void Update(
        WaypointSystemManager waypointManager,
        Settings settings,
        CsvExportService csvExportService,
        string picturesFolderName,
        bool writeUsbVisibleCsvCopyOnNonUwp,
        string persistentDataPath,
        Action<string, string, string> onCsvRowWritten = null)
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
            persistentDataPath,
            onCsvRowWritten);
        hasLoggedCurrentCompletion = true;
    }

    void WriteCompletedSessionMetricsCsv(
        WaypointSystemManager waypointManager,
        string fileName,
        CsvExportService csvExportService,
        string picturesFolderName,
        bool writeUsbVisibleCsvCopyOnNonUwp,
        string persistentDataPath,
        Action<string, string, string> onCsvRowWritten)
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

            if (onCsvRowWritten != null)
            {
                onCsvRowWritten(
                    CsvExportService.SanitizeCsvFileName(fileName, "session_metrics.csv"),
                    header,
                    row);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[SessionMetricsCsvController] Failed to write session metrics CSV: " + ex.Message);
        }
    }
}
