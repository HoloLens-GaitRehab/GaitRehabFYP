using System;
using UnityEngine.Windows.Speech;

public class VoiceCommandController
{
    private KeywordRecognizer keywordRecognizer;

    public void Initialize(
        Action onShowStats,
        Action onHideStats,
        Action onToggleMetronome,
        Action onMetronomeOn,
        Action onMetronomeOff,
        Action onPause,
        Action onResume,
        Action onEndSession)
    {
        Dispose();

        string[] keywords =
        {
            "show stats",
            "hide stats",
            "metronome",
            "metronome on",
            "metronome off",
            "pause",
            "resume",
            "end session"
        };

        keywordRecognizer = new KeywordRecognizer(keywords);
        keywordRecognizer.OnPhraseRecognized += args =>
        {
            switch (args.text.ToLower())
            {
                case "show stats":
                    onShowStats?.Invoke();
                    break;
                case "hide stats":
                    onHideStats?.Invoke();
                    break;
                case "metronome":
                    onToggleMetronome?.Invoke();
                    break;
                case "metronome on":
                    onMetronomeOn?.Invoke();
                    break;
                case "metronome off":
                    onMetronomeOff?.Invoke();
                    break;
                case "pause":
                    onPause?.Invoke();
                    break;
                case "resume":
                    onResume?.Invoke();
                    break;
                case "end session":
                    onEndSession?.Invoke();
                    break;
            }
        };

        keywordRecognizer.Start();
    }

    public void Dispose()
    {
        if (keywordRecognizer != null && keywordRecognizer.IsRunning)
        {
            keywordRecognizer.Stop();
        }

        if (keywordRecognizer != null)
        {
            keywordRecognizer.Dispose();
            keywordRecognizer = null;
        }
    }
}
