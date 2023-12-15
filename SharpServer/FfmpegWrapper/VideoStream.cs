using Croissantbit;
using Color = Croissantbit.Color;

namespace SharpServer.FfmpegWrapper;

public static class VideoStream
{
    public static Pixel[] ReadFrame(Stream videoStream, int width, int height)
    {
        return null;
    }

    /// <summary>
    /// Finds the nearest color to the given RGB values using Euclidean distance
    /// See:
    /// https://stackoverflow.com/questions/7639741/find-nearest-color
    /// https://en.wikipedia.org/wiki/Color_difference
    /// https://stackoverflow.com/questions/61443941/processing-how-to-get-the-nearest-color-from-a-collection-of-colors
    /// https://www.codeproject.com/Articles/1172815/Finding-Nearest-Colors-using-Euclidean-Distance
    /// https://www.dfstudios.co.uk/articles/programming/image-programming-algorithms/image-processing-algorithms-part-1-finding-nearest-colour/
    /// </summary>
    /// <param name="r"></param>
    /// <param name="g"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    private static Color FindNearestColor(byte r, byte g, byte b)
    {
        var nearestColor = Color.Black;
        var nearestDistance = double.MaxValue;

        foreach (Color color in Enum.GetValues(typeof(Color)))
        {
            var (cr, cg, cb) = GetRgbValues(color);
            var distance = Math.Sqrt(
                (r - cr) * (r - cr) + (g - cg) * (g - cg) + (b - cb) * (b - cb)
            );

            if (!(distance < nearestDistance))
                continue;
            nearestDistance = distance;
            nearestColor = color;
        }

        return nearestColor;
    }

    /// <summary>
    /// Maps each RGB value to the Color enum
    /// Mirrors the colors defined in https://github.com/Bodmer/TFT_eSPI
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private static (byte, byte, byte) GetRgbValues(Color color)
    {
        return color switch
        {
            Color.Black => (0, 0, 0),
            Color.Navy => (0, 0, 128),
            Color.Darkgreen => (0, 128, 0),
            Color.Darkcyan => (0, 128, 128),
            Color.Maroon => (128, 0, 0),
            Color.Purple => (128, 0, 128),
            Color.Olive => (128, 128, 0),
            Color.Lightgrey => (211, 211, 211),
            Color.Darkgrey => (128, 128, 128),
            Color.Blue => (0, 0, 255),
            Color.Green => (0, 255, 0),
            Color.Cyan => (0, 255, 255),
            Color.Red => (255, 0, 0),
            Color.Magenta => (255, 0, 255),
            Color.Yellow => (255, 255, 0),
            Color.White => (255, 255, 255),
            Color.Orange => (255, 180, 0),
            Color.Greenyellow => (180, 255, 0),
            Color.Pink => (255, 192, 203),
            _ => throw new ArgumentException($"Invalid color: {color}")
        };
    }
}
