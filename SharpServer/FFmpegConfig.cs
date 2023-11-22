using System;
using Serilog;
using Serilog.Events;

namespace FFMpegWrapper
{
    public class FFmpegConfig
    {
        private static FFmpegConfig Instance = null;
        public String _path = null;
        public bool _useShellExecute = false;
        public bool _createAWindow = false;

        public static void SetFFmpegConfig(String path, bool useShellExecute, bool createAWindow)
        {
            if (Instance != null)
            {
                throw new Exception("Config has already been declared");
            }

            Instance = new FFmpegConfig(path, useShellExecute, createAWindow);
        }

        public static FFmpegConfig GetFFmpegConfig()
        {
            if (Instance == null)
            {
                throw new Exception("No FFmpeg Config initialized. Please define config");
            }

            return Instance;
        }

        private FFmpegConfig(String path, bool useShellExecute, bool createAWindow )
        {
            _path = path;
            _useShellExecute = useShellExecute;
            _createAWindow = createAWindow;
        }
    }
}