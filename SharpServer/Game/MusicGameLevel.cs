namespace SharpServer.Game;

public class MusicGameLevel
{
    private List<TimeSpan> list;
    private List<float> listFloats;

    public MusicGameLevel(List<TimeSpan> list, List<float> listFloats)
    {
        this.list = list;
        this.listFloats = listFloats;
    }
}
