namespace SharpServer;

public static class Util
{
    public static int GenerateClientId()
    {
        var random = new Random();
        var timestamp = DateTime.UtcNow.Ticks % int.MaxValue;
        var randomNumber = random.Next(int.MaxValue);

        // XOR the two numbers together to get a unique-ish number
        // Then mod it by int.MaxValue to clamp the number to a int32
        var clientId = (int)((timestamp ^ randomNumber) % int.MaxValue);
        return clientId;
    }
}
