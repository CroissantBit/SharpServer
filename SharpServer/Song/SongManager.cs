using NAudio.Wave;
using SharpServer.Game;

namespace SharpServer.Song;

public class SongManager
{
    private List<TimeSpan> list;
    private List<float> listFloats;
    private string songName;

    public SongManager(string songName)
    {
        this.songName = songName;
        list = new List<TimeSpan>();
        listFloats = new List<float>();
    }

    public MusicGameLevel GenerateMusicGame()
    {
        var wavFilePath = $"./bin/WavFiles/{songName}.wav"; // Replace with your WAV file path

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
                    list.Add(span);
                    listFloats.Add(high);
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

            for (int d = 0; d < list.Count; d++)
            {
                Console.WriteLine("time : " + list[d]);
                Console.WriteLine("floatvalue : " + listFloats[d].ToString("C"));
            }
        }

        return new MusicGameLevel(list, listFloats);
    }

    public async Task PlayAudio()
    {
        var wavFilePath = $"./bin/WavFiles/{songName}.wav";
        var lastTime = new TimeSpan(0, 0, 0, 0, 0);
        var counter = 0;
        var waveOut = new WaveOutEvent();
        var audioFileReader = new AudioFileReader(wavFilePath);
        waveOut.Init(audioFileReader);
        waveOut.Play();

        while (waveOut.PlaybackState == PlaybackState.Playing)
        {
            if (list[counter] >= audioFileReader.CurrentTime)
                continue;
            DisplayButtonToClick(listFloats[counter]);
            counter++;
        }

        while (counter < listFloats.Count - 1)
        {
            Thread.Sleep(100);
            lastTime = lastTime.Add(TimeSpan.FromMilliseconds(100));
            Console.WriteLine(lastTime);
            if (list[counter] >= lastTime)
                continue;
            DisplayButtonToClick(listFloats[counter]);
            counter++;
            if (list[counter] >= lastTime)
                continue;
            Console.WriteLine("need to be higher density");
            counter++;
        }
    }

    private static void DisplayButtonToClick(float value)
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
