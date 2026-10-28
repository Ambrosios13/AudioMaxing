using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AudioMaxing;

internal sealed class AudioEngine : IDisposable
{
    private static AudioEngine? active;
    private readonly MMDeviceEnumerator enumerator = new();
    private WasapiCapture? capture;
    private WasapiOut? output;
    private BufferedWaveProvider? buffer;
    private GainLimiterSampleProvider? dsp;
    private string? previousDefaultId;
    private MMDevice? cableRender;
    private MMDevice? cableCapture;

    public bool IsRunning { get; private set; }
    public float Peak => dsp?.OutputPeak ?? 0;
    public bool IsLimiting => dsp?.Limiting ?? false;
    public float GainReductionDb => dsp?.GainReductionDb ?? 0;

    public IReadOnlyList<AudioDevice> GetOutputs() => enumerator
        .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
        .Where(d => !IsAnyCable(d))
        .Select(d => new AudioDevice(d.ID, d.FriendlyName)).ToList();

    public void Start(string outputId, float gainDb)
    {
        if (IsRunning) return;
        var renders = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        cableRender = renders.FirstOrDefault(IsStandardCableInput) ?? throw new InvalidOperationException(
            "VB-CABLE não encontrado. Instale-o e reinicie o Windows antes de ativar.");
        cableCapture = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
            .FirstOrDefault(IsStandardCableOutput) ?? throw new InvalidOperationException("A entrada 'CABLE Output' não foi encontrada.");
        var target = enumerator.GetDevice(outputId);
        previousDefaultId = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;
        if (target.ID == cableRender.ID) throw new InvalidOperationException("O dispositivo de destino não pode ser o próprio VB-CABLE.");

        try
        {
            RecoveryState.Save(previousDefaultId);
            capture = new WasapiCapture(cableCapture, true, 20);
            buffer = new BufferedWaveProvider(capture.WaveFormat)
            {
                BufferDuration = TimeSpan.FromMilliseconds(250),
                DiscardOnBufferOverflow = true,
                ReadFully = true
            };
            dsp = new GainLimiterSampleProvider(buffer.ToSampleProvider());
            dsp.SetGain(gainDb);
            output = new WasapiOut(target, AudioClientShareMode.Shared, true, 30);
            output.Init(dsp.ToWaveProvider());
            capture.DataAvailable += (_, e) => buffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
            capture.RecordingStopped += (_, e) => { if (e.Exception != null) Stop(); };
            output.PlaybackStopped += (_, e) => { if (e.Exception != null) Stop(); };
            output.Play();
            capture.StartRecording();
            PolicyConfig.SetDefaultEndpoint(cableRender.ID);
            IsRunning = true;
            active = this;
        }
        catch
        {
            Stop();
            throw;
        }
    }

    public void SetGain(float db) => dsp?.SetGain(db);

    public void Stop()
    {
        try { capture?.StopRecording(); } catch { }
        try { output?.Stop(); } catch { }
        bool restored = string.IsNullOrWhiteSpace(previousDefaultId);
        if (!restored)
        {
            try { PolicyConfig.SetDefaultEndpoint(previousDefaultId!); restored = true; } catch { }
        }
        capture?.Dispose(); output?.Dispose(); cableRender?.Dispose(); cableCapture?.Dispose();
        capture = null; output = null; dsp = null; cableRender = null; cableCapture = null;
        previousDefaultId = null; IsRunning = false;
        if (restored) RecoveryState.Clear();
        if (ReferenceEquals(active, this)) active = null;
    }

    private static bool IsStandardCableInput(MMDevice d) =>
        d.FriendlyName.Contains("CABLE Input", StringComparison.OrdinalIgnoreCase) &&
        !d.FriendlyName.Contains("16ch", StringComparison.OrdinalIgnoreCase);

    private static bool IsStandardCableOutput(MMDevice d) =>
        d.FriendlyName.Contains("CABLE Output", StringComparison.OrdinalIgnoreCase) &&
        !d.FriendlyName.Contains("16ch", StringComparison.OrdinalIgnoreCase);

    private static bool IsAnyCable(MMDevice d) =>
        d.FriendlyName.Contains("CABLE In", StringComparison.OrdinalIgnoreCase) ||
        d.FriendlyName.Contains("CABLE Out", StringComparison.OrdinalIgnoreCase) ||
        d.DeviceFriendlyName.Contains("VB-Audio Virtual Cable", StringComparison.OrdinalIgnoreCase);

    public static void EmergencyStopAll() => active?.Stop();
    public void Dispose() { Stop(); enumerator.Dispose(); }
}
