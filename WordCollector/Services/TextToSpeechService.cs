using System.Speech.Synthesis;

namespace WordCollector.Services;

public class TextToSpeechService
{
    private readonly SpeechSynthesizer _synthesizer;
    private readonly SettingsService _settingsService;
    private bool _isSpeaking;

    public bool IsSpeaking => _isSpeaking;

    public event EventHandler? SpeakStarted;
    public event EventHandler? SpeakCompleted;

    public TextToSpeechService(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _synthesizer = new SpeechSynthesizer();
        _synthesizer.SpeakStarted += (s, e) =>
        {
            _isSpeaking = true;
            SpeakStarted?.Invoke(this, EventArgs.Empty);
        };
        _synthesizer.SpeakCompleted += (s, e) =>
        {
            _isSpeaking = false;
            SpeakCompleted?.Invoke(this, EventArgs.Empty);
        };

        ApplySettings();
    }

    public void ApplySettings()
    {
        var settings = _settingsService.Load();

        // Set rate (-10 to 10, default 0)
        _synthesizer.Rate = settings.TtsRate;

        // Set voice
        var voices = GetAvailableEnglishVoices();
        if (voices.Count > 0)
        {
            if (settings.TtsVoiceName == "auto" || string.IsNullOrWhiteSpace(settings.TtsVoiceName))
            {
                // Auto-select the first English voice
                _synthesizer.SelectVoice(voices[0].VoiceInfo.Name);
            }
            else
            {
                try
                {
                    _synthesizer.SelectVoice(settings.TtsVoiceName);
                }
                catch
                {
                    if (voices.Count > 0)
                        _synthesizer.SelectVoice(voices[0].VoiceInfo.Name);
                }
            }
        }
    }

    public List<InstalledVoice> GetAvailableEnglishVoices()
    {
        try
        {
            return _synthesizer.GetInstalledVoices()
                .Where(v => v.VoiceInfo.Culture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        catch
        {
            return new List<InstalledVoice>();
        }
    }

    public List<InstalledVoice> GetAllVoices()
    {
        try
        {
            return _synthesizer.GetInstalledVoices().ToList();
        }
        catch
        {
            return new List<InstalledVoice>();
        }
    }

    public string? GetEnglishVoiceHint()
    {
        var voices = GetAvailableEnglishVoices();
        if (voices.Count == 0)
        {
            var allVoices = GetAllVoices();
            if (allVoices.Count == 0)
            {
                return "未找到可用语音，请在 Windows 设置中安装语音包。";
            }
            return "未找到可用英文语音，请在 Windows 语音设置中安装英文语音包。";
        }
        return null;
    }

    public void SpeakAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        try
        {
            var settings = _settingsService.Load();
            if (!settings.TtsEnabled)
                return;

            if (_isSpeaking)
            {
                Stop();
                return;
            }

            ApplySettings();
            _synthesizer.SpeakAsyncCancelAll();
            _synthesizer.SpeakAsync(text);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TTS SpeakAsync error: {ex.Message}");
            _isSpeaking = false;
        }
    }

    public void Stop()
    {
        try
        {
            _synthesizer.SpeakAsyncCancelAll();
            _isSpeaking = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TTS Stop error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        try
        {
            _synthesizer.SpeakAsyncCancelAll();
            _synthesizer.Dispose();
        }
        catch
        {
            // Ignore disposal errors
        }
    }
}
