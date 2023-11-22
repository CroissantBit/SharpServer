using NAudio.Wave;
using Serilog;
using Timer = System.Timers.Timer;

namespace SharpServer;

public class SongManager
{
    private List<TimeSpan> list;
    private List<float> listFloats;
    private string songName;

    public SongManager(string songName)
    {
        this.songName = songName;
    }

    public MusicGameLevel generateMusicGame()
    {
        string wavFilePath = $"./bin/WavFiles/{songName}.wav"; // Replace with your WAV file path
        list = new List<TimeSpan>();
        listFloats = new List<float>();
        Timer aTimer;

        using (var reader = new WaveFileReader(wavFilePath))
        {
            Console.WriteLine("Amount of Channels " + reader.WaveFormat.Channels);
            Console.WriteLine("Samplerare : " + reader.WaveFormat.SampleRate);
            Console.WriteLine("Bits per Sample : " + reader.WaveFormat.BitsPerSample);
            float[] floatArr = new float[2];
            int counter = 0;
            float avg = 0f;
            float high = 0f;
            TimeSpan span = new TimeSpan();
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
            catch (Exception e) { }

            for (int d = 0; d < list.Count; d++)
            {
                Console.WriteLine("time : " + list[d]);
                Console.WriteLine("floatvalue : " + listFloats[d].ToString("C"));
            }
        }

        return new MusicGameLevel(list, listFloats);
        //SetTimer();
    }

    public async Task playAudio()
    {
        string wavFilePath = $"./bin/WavFiles/{songName}.wav";
        TimeSpan lastTime = new TimeSpan(0, 0, 0, 0, 0);
        int counterr = 0;
        var waveOut = new WaveOutEvent();
        var audioFileReader = new AudioFileReader(wavFilePath);
        waveOut.Init(audioFileReader);
        waveOut.Play();

        while (waveOut.PlaybackState == PlaybackState.Playing)
        {
            //Console.WriteLine(audioFileReader.CurrentTime);
            if (list[counterr] < audioFileReader.CurrentTime)
            {
                DisplayButtonToClick(listFloats[counterr]);
                counterr++;
            }
        }
        while (counterr < listFloats.Count - 1)
        {
            Thread.Sleep(100);
            lastTime = lastTime.Add(TimeSpan.FromMilliseconds(100));
            Console.WriteLine(lastTime);
            if (list[counterr] < lastTime)
            {
                DisplayButtonToClick(listFloats[counterr]);
                counterr++;
                if (list[counterr] < lastTime)
                {
                    Console.WriteLine("need to be higher density");
                    counterr++;
                }
            }
        }
    }

    void DisplayButtonToClick(float value)
    {
        if (value < 0.2f)
        {
            Console.WriteLine("-------------------------------------------RIGHT");
        }
        else if (value < 0.4f)
        {
            Console.WriteLine("------------------MIDDLE-----------------------------");
        }
        else if (value < 0.6f)
        {
            Console.WriteLine("LEFT------------------------------------------------");
        }
        else
        {
            Console.WriteLine(
                "--------------------------------------------------------------------------------TOP"
            );
        }
    }
}
