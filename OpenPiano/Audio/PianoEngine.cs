using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace OpenPiano.Audio;

/// <summary>
/// Low-latency polyphonic output via WASAPI.
/// PLACEHOLDER SOUND: simple decaying sine voices. Replace <see cref="Voice"/> with
/// sample playback (e.g. a freely licensed piano SoundFont/SFZ) for a real piano tone.
/// </summary>
public sealed class PianoEngine : IDisposable
{
    private const int SampleRate = 44100;
    private readonly WasapiOut _output;
    private readonly MixingSampleProvider _mixer;
    private readonly Dictionary<int, Voice> _voices = new();
    private readonly object _lock = new();

    public PianoEngine()
    {
        _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 2))
        {
            ReadFully = true, // keep the stream alive when no notes are playing
        };
        // Shared mode with a small latency target; try exclusive mode later for even lower latency.
        _output = new WasapiOut(AudioClientShareMode.Shared, 30);
        _output.Init(_mixer);
        _output.Play();
    }

    public void NoteOn(int midiNote)
    {
        lock (_lock)
        {
            if (_voices.TryGetValue(midiNote, out var old)) old.Release();
            var voice = new Voice(midiNote, SampleRate);
            _voices[midiNote] = voice;
            _mixer.AddMixerInput(voice);
        }
    }

    public void NoteOff(int midiNote)
    {
        lock (_lock)
        {
            if (_voices.Remove(midiNote, out var v)) v.Release();
        }
    }

    public void Dispose()
    {
        _output.Stop();
        _output.Dispose();
    }

    private sealed class Voice : ISampleProvider
    {
        private readonly double _freq;
        private double _phase;
        private double _env = 1.0;
        private bool _released;
        public WaveFormat WaveFormat { get; }

        public Voice(int midiNote, int sampleRate)
        {
            _freq = 440.0 * Math.Pow(2, (midiNote - 69) / 12.0);
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);
        }

        public void Release() => _released = true;

        public int Read(float[] buffer, int offset, int count)
        {
            double step = 2 * Math.PI * _freq / WaveFormat.SampleRate;
            double decay = _released ? 0.9990 : 0.99996; // fast fade after release, slow ring while held
            for (int n = 0; n < count; n += 2)
            {
                float s = (float)(Math.Sin(_phase) * _env * 0.25);
                buffer[offset + n] = s;
                buffer[offset + n + 1] = s;
                _phase += step;
                _env *= decay;
            }
            return _env < 0.0005 ? 0 : count; // returning 0 removes the voice from the mixer
        }
    }
}
