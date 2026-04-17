using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SessionCsvUploadController
{
    public struct Settings
    {
        public bool uploadEnabled;
        public string uploadUrl;
        public float timeoutSeconds;
    }

    public void TryUploadCsvRow(
        MonoBehaviour owner,
        Settings settings,
        string fileName,
        string header,
        string row,
        string logContext)
    {
        if (!settings.uploadEnabled)
            return;

        if (owner == null)
        {
            Debug.LogWarning("[SessionCsvUploadController] Upload skipped because owner is null.");
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.uploadUrl))
        {
            Debug.LogWarning("[SessionCsvUploadController] Upload enabled but URL is empty.");
            return;
        }

        string safeName = CsvExportService.SanitizeCsvFileName(fileName, "session_metrics.csv");
        string csvPayload = header + "\n" + row + "\n";
        owner.StartCoroutine(UploadCsvCoroutine(settings, safeName, csvPayload, logContext));
    }

    IEnumerator UploadCsvCoroutine(Settings settings, string fileName, string csvPayload, string logContext)
    {
        byte[] csvBytes = Encoding.UTF8.GetBytes(csvPayload);

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", csvBytes, fileName, "text/csv");

        using (UnityWebRequest request = UnityWebRequest.Post(settings.uploadUrl.Trim(), form))
        {
            int timeout = Mathf.Clamp(Mathf.RoundToInt(settings.timeoutSeconds), 3, 120);
            request.timeout = timeout;
            yield return request.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool hasError = request.result != UnityWebRequest.Result.Success;
#else
            bool hasError = request.isNetworkError || request.isHttpError;
#endif

            if (hasError)
            {
                Debug.LogWarning("[SessionCsvUploadController] " + logContext + " CSV upload failed: " + request.error + " | URL: " + settings.uploadUrl);
                yield break;
            }

            Debug.Log("[SessionCsvUploadController] " + logContext + " CSV uploaded: " + fileName + " | URL: " + settings.uploadUrl);
        }
    }
}
