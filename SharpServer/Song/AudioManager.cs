using DotNetEnv;
using NAudio.Wave;
using Serilog;
using SharpServer.Game;

namespace SharpServer.Song;

public class AudioManager
{
    private readonly List<TimeSpan> _list;
    private readonly List<float> _listSignals;
    private readonly string _videoName;

    public delegate void SignalUpdateCallback(float value);

    private readonly SignalUpdateCallback _signalUpdateCallback;

    public AudioManager(string videoName, SignalUpdateCallback? signalUpdateCallback = null)
    {
        _videoName = videoName;
        _list = new List<TimeSpan>();
        _listSignals = new List<float>();
        _signalUpdateCallback = DisplaySignalToConsole;

        if (signalUpdateCallback != null)
            _signalUpdateCallback = signalUpdateCallback;
    }

    public MusicGameLevel GenerateAudioMap()
    {
        var wavFilePath = $"{Env.GetString("CACHE_DIR")}/Mp3Files/{_videoName}.wav";

        using (var reader = new WaveFileReader(wavFilePath))
        {
            Log.Debug("Amount of Channels " + reader.WaveFormat.Channels);
            Log.Debug("Sample rate : " + reader.WaveFormat.SampleRate);
            Log.Debug("Bits per Sample : " + reader.WaveFormat.BitsPerSample);
            var counter = 0;
            var avg = 0f;
            var high = 0f;
            var span = new TimeSpan();
            try
            {
                float[] floatArr;
                while ((floatArr = reader.ReadNextSampleFrame()) != Array.Empty<float>())
                {
                    counter++;
                    if (floatArr[0] > high)
                    {
                        high = floatArr[0];
                        span = reader.CurrentTime;
                    }

                    if (floatArr[1] > high)
                    {
                        high = floatArr[1];
                        span = reader.CurrentTime;
                    }

                    avg += MathF.Abs(floatArr[0]);
                    avg += MathF.Abs(floatArr[1]);
                    if (counter < 48000)
                        continue;
                    counter = 0;

                    _list.Add(span);
                    _listSignals.Add(high);
                    high = 0f;
                    span = new TimeSpan();
                    avg = 0f;
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        return new MusicGameLevel(_list, _listSignals);
    }

    public Task Play()
    {
        var wavFilePath = $"{Env.GetString("CACHE_DIR")}/Mp3Files/{_videoName}.wav";
        var counter = 0;
        var waveOut = new WaveOutEvent();
        var audioFileReader = new AudioFileReader(wavFilePath);
        waveOut.Init(audioFileReader);
        waveOut.Play();

        while (waveOut.PlaybackState == PlaybackState.Playing)
        {
            try
            {
                if (_list[counter] >= audioFileReader.CurrentTime)
                    continue;
                _signalUpdateCallback.Invoke(_listSignals[counter]);
                counter++;
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        return Task.CompletedTask;
    }

    private static void DisplaySignalToConsole(float value)
    {
        switch (value)
        {
            case < 0.2f:
                Console.WriteLine("-------------------------------------------RIGHT");
                break;
            case < 0.4f:
                Console.WriteLine("------------------MIDDLE-----------------------------");
                break;
            case < 0.6f:
                Console.WriteLine("LEFT------------------------------------------------");
                break;
            default:
                Console.WriteLine(
                    "--------------------------------------------------------------------------------TOP"
                );
                break;
        }
    }
}
