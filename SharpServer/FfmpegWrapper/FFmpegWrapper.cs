using System.Diagnostics;
using System.Drawing;
using System.Text;
using DotNetEnv;
using FfmpegWrapper;
using Serilog;

namespace SharpServer.FfmpegWrapper
{
    public class FFmpegWrapper : IDisposable
    {
        private static readonly FFmpegWrapper Instance = new(FFmpegConfig.GetFFmpegConfig());
        private readonly FFmpegConfig _config;
        private bool _disposed;
        private Process _process;

        public static FFmpegWrapper GetFFmpegWrapper()
        {
            return Instance;
        }

        private FFmpegWrapper(FFmpegConfig config)
        {
            _config = config;
            StartFFmpeg();
        }

        private void StartFFmpeg()
        {
            _process = new Process();
            _process.StartInfo.UseShellExecute = _config._useShellExecute;
            _process.StartInfo.FileName = _config._path;
            _process.StartInfo.CreateNoWindow = !_config._createAWindow;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardError = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _process.Dispose();
                }

                _disposed = true;
            }
        }

        public void CreateWavFile(string fileName, string path)
        {
            var pathFile = Env.GetString("CACHE_DIR") + "/Mp4Files/" + fileName + ".mp4";
            try
            {
                string command = $" -i {pathFile} -vn -ar 44100 -ac 2 -ab 192k -f wav " + path;
                _process.StartInfo.Arguments = command;
                _process.Start();
                _process.WaitForExit();
                _process.StandardOutput.ReadToEnd();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public string customCommand(string command)
        {
            try
            {
                _process.StartInfo.Arguments = command;

                _process.Start();
                StreamReader reade2 = _process.StandardError;
                string outputt = reade2.ReadToEnd();
                var duration = GetDurationLine(outputt);
                _process.WaitForExit();
                return duration;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception(e.Message);
            }
        }

        static string GetDurationLine(string output)
        {
            string[] lines = output.Split('\n');
            foreach (string line in lines)
            {
                if (line.Contains("Duration"))
                {
                    var tmpLine = line.Split(',')[0].Split('n')[1];
                    return tmpLine.Substring(2);
                }
            }

            return "Duration not found in output";
        }

        public string GetSongDuration(string songName)
        {
            var pathFile = Env.GetString("CACHE_DIR") + "/Mp4Files/" + songName + ".mp4";
            var commands = $"-i {pathFile}";
            var output = customCommand(commands);

            return output;
        }

        public async Task<string> CustomCommandTest(String command)
        {
            MemoryStream copyStream = new MemoryStream();
            Console.WriteLine("should start converting");
            try
            {
                string result = String.Empty;

                _process.StartInfo.Arguments = command;
                _process.StartInfo.RedirectStandardError = false;
                _process.StartInfo.RedirectStandardInput = false;

                _process.Start();
                var inputStream = _process.StandardOutput.BaseStream;
                bool headerFound = false;
                int counterFrames = 0;
                var watch = new Stopwatch();
                watch.Start();
                while (_process.StandardOutput.Peek() > -1)
                {
                    if (!headerFound)
                    {
                        byte[] chunkHeader = new byte[8];
                        for (int j = 0; j < 8; j++)
                        {
                            //take the tempBuf outside
                            byte[] tempBuf = new byte[1];
                            int x = inputStream.Read(tempBuf, 0, 1);
                            if (x == 0)
                            {
                                j--;

                                continue;
                            }

                            chunkHeader[j] = tempBuf[0];
                        }

                        headerFound = true;
                        copyStream.Write(chunkHeader, 0, 8);
                    }

                    byte[] chunk = new byte[4];
                    for (int j = 0; j < 4; j++)
                    {
                        //take the tempBuf outside
                        byte[] tempBuf = new byte[1];
                        int x = inputStream.Read(tempBuf, 0, 1);
                        if (x == 0)
                        {
                            j--;
                            continue;
                        }

                        chunk[j] = tempBuf[0];
                    }

                    copyStream.Write(chunk, 0, 4);
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(chunk);
                    }

                    int i = BitConverter.ToInt32(chunk, 0);

                    chunk = new byte[4];
                    for (int j = 0; j < 4; j++)
                    {
                        //take the tempBuf outside
                        byte[] tempBuf = new byte[1];
                        int x = inputStream.Read(tempBuf, 0, 1);
                        if (x == 0)
                        {
                            j--;
                            continue;
                        }

                        chunk[j] = tempBuf[0];
                    }

                    copyStream.Write(chunk, 0, 4);

                    var str = Encoding.ASCII.GetChars(chunk);

                    byte[] tempByteArray = new byte[i + 4];
                    for (int j = 0; j < i + 4; j++)
                    {
                        //!change
                        //take the tempBuf outside
                        byte[] tempBuf = new byte[1];
                        int x = inputStream.Read(tempBuf, 0, 1);
                        //!change
                        //chnage to single line with else block
                        if (x == 0)
                        {
                            j--;
                            continue;
                        }

                        tempByteArray[j] = tempBuf[0];
                    }

                    copyStream.Write(tempByteArray, 0, i + 4);
                    if (new string(str) == "IEND")
                    {
                        counterFrames++;
                        parseBitmap(copyStream);
                        copyStream.Dispose();
                        copyStream = new MemoryStream();
                        inputStream.Flush();
                        headerFound = false;

                        if (watch.ElapsedMilliseconds > 100)
                        {
                            Log.Logger.Debug(watch.ElapsedMilliseconds.ToString());
                        }
                        while (watch.ElapsedMilliseconds < 100)
                            ;
                        watch.Reset();
                        watch.Start();
                    }
                }

                _process.WaitForExit();
                return result;
            }
            catch (Exception e)
            {
         
                Console.WriteLine(e);
            }
            finally
            {
                copyStream.Dispose();
            }

            _process.StartInfo.RedirectStandardError = true;
            return String.Empty;
        }

        private void parseBitmap(Stream stream)
        {
            // Create a Bitmap object from the byte array
            using (Bitmap image = new Bitmap(stream))
            {
                Console.Clear();
                convertToText(image);
            }
        }

        private void convertToText(Bitmap bmp)
        {
            int pixelInterval = 8;

            double brightnessMultiplier = 1;
            string WrittenLine = "";
            for (
                int y = 0;
                y < bmp.Size.Height - (bmp.Size.Height % pixelInterval);
                y += pixelInterval
            )
            {
                for (int x = 0; x < bmp.Size.Width; x++)
                {
                    if (x % pixelInterval == 0 || x % pixelInterval == 1)
                    {
                        WrittenLine += getSymbolFromBrightness(
                            bmp.GetPixel(x, y).GetBrightness() * brightnessMultiplier
                        );
                    }
                }

                WrittenLine += '\n';
                Console.WriteLine(WrittenLine);
            }

        }

        private string getSymbolFromBrightness(double brightness)
        {
            switch ((int)(brightness * 10))
            {
                case 0:
                    return "@";
                case 1:
                    return "$";
                case 2:
                    return "#";
                case 3:
                    return "*";
                case 4:
                    return "!";
                case 5:
                    return "+";
                case 6:
                    return ":";
                case 7:
                    return "~";
                case 8:
                    return "-";
                case 9:
                    return ".";
                default:
                    return " ";
            }
        }
    }
}
