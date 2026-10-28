using NAudio.Wave;

namespace AudioMaxing;

internal sealed class GainLimiterSampleProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly int channels;
    private readonly float ceiling = MathF.Pow(10f, -1f / 20f);
    private float currentGain = 1f;
    private float limiterGain = 1f;
    private volatile float targetDb;

    public WaveFormat WaveFormat => source.WaveFormat;
    public float OutputPeak { get; private set; }
    public bool Limiting { get; private set; }
    public float GainReductionDb => 20f * MathF.Log10(MathF.Max(limiterGain, 0.000001f));

    public GainLimiterSampleProvider(ISampleProvider source)
    {
        this.source = source;
        channels = Math.Max(1, source.WaveFormat.Channels);
    }

    public void SetGain(float db) => targetDb = Math.Clamp(db, 0, 24);

    public int Read(float[] buffer, int offset, int count)
    {
        int read = source.Read(buffer, offset, count);
        float targetLinear = MathF.Pow(10f, targetDb / 20f);
        float ramp = 1f - MathF.Exp(-1f / (WaveFormat.SampleRate * 0.050f)); // 50 ms
        float release = 1f - MathF.Exp(-1f / (WaveFormat.SampleRate * 0.120f));
        float peak = 0;
        bool limiting = false;

        for (int n = 0; n < read; n += channels)
        {
            currentGain += (targetLinear - currentGain) * ramp;
            int frameCount = Math.Min(channels, read - n);
            float framePeak = 0;
            for (int c = 0; c < frameCount; c++)
                framePeak = Math.Max(framePeak, Math.Abs(buffer[offset + n + c] * currentGain));

            float needed = framePeak > ceiling ? ceiling / framePeak : 1f;
            if (needed < limiterGain) limiterGain = needed; // ataque imediato
            else limiterGain += (1f - limiterGain) * release;
            limiting |= limiterGain < 0.999f;

            for (int c = 0; c < frameCount; c++)
            {
                int i = offset + n + c;
                buffer[i] = Math.Clamp(buffer[i] * currentGain * limiterGain, -ceiling, ceiling);
                peak = Math.Max(peak, Math.Abs(buffer[i]));
            }
        }
        OutputPeak = peak;
        Limiting = limiting;
        return read;
    }
}
