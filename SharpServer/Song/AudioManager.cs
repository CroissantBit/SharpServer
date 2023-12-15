using DotNetEnv;
using NAudio.Wave;
using Serilog;
using SharpServer.Game;

namespace SharpServer.Song;

public class AudioManager
{
    private readonly List<TimeSpan> _list;
    private readonly List<float> _listFloats;
    private readonly string _videoName;

    public delegate void SignalUpdateCallback(float value);

    private readonly SignalUpdateCallback _signalUpdateCallback;

    public AudioManager(string videoName, SignalUpdateCallback? signalUpdateCallback = null)
    {
        _videoName = videoName;
        _list = new List<TimeSpan>();
        _listFloats = new List<float>();
        _signalUpdateCallback = DisplaySignalToConsole;

        if (signalUpdateCallback != null)
            _signalUpdateCallback = signalUpdateCallback;
    }

    public MusicGameLevel GenerateMusicGame()
    {
        var wavFilePath = $"./bin/WavFiles/{_videoName}.wav"; // Replace with your WAV file path

        using (var reader = new WaveFileReader(wavFilePath))
        {
            Console.WriteLine("Amount of Channels " + reader.WaveFormat.Channels);
            Console.WriteLine("Sample rate : " + reader.WaveFormat.SampleRate);
            Console.WriteLine("Bits per Sample : " + reader.WaveFormat.BitsPerSample);
            var floatArr = new float[2];
            var counter = 0;
            var avg = 0f;
            var high = 0f;
            var span = new TimeSpan();
            try
            {
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
                    avg /= (48000 * 2);
                    Console.WriteLine("avg : " + avg.ToString("C"));
                    Console.WriteLine(reader.CurrentTime);
                    _list.Add(span);
                    _listFloats.Add(high);
                    high = 0f;
                    span = new TimeSpan();
                    Console.WriteLine("-----------------------------------");
                    avg = 0f;
                }
            }
            catch (Exception e)
            {
                // ignored
            }

            for (int d = 0; d < _list.Count; d++)
            {
                Console.WriteLine("time : " + _list[d]);
                Console.WriteLine("floatvalue : " + _listFloats[d].ToString("C"));
            }
        }

        return new MusicGameLevel(_list, _listFloats);
    }

    public Task Play()
    {
        var wavFilePath = $"{Env.GetString("CACHE_DIR")}/WavFiles/{_videoName}.wav";
        var lastTime = new TimeSpan(0, 0, 0, 0, 0);
        var counter = 0;
        var waveOut = new WaveOutEvent();
        var audioFileReader = new AudioFileReader(wavFilePath);
        waveOut.Init(audioFileReader);
        waveOut.Play();

        while (waveOut.PlaybackState == PlaybackState.Playing)
        {
            if (_list[counter] >= audioFileReader.CurrentTime)
                continue;
            _signalUpdateCallback.Invoke(_listFloats[counter]);
            counter++;
        }

        while (counter < _listFloats.Count - 1)
        {
            Thread.Sleep(100);
            lastTime = lastTime.Add(TimeSpan.FromMilliseconds(100));
            if (_list[counter] >= lastTime)
                continue;
            _signalUpdateCallback.Invoke(_listFloats[counter]);
            counter++;
            if (_list[counter] >= lastTime)
                continue;
            Log.Debug("Not enough content to play at");
            counter++;
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
