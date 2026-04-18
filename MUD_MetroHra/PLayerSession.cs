namespace MUD_MetroHra;

using System.IO;

public class PlayerSession
{
    public Player Player { get; set; } = new();
    public StreamWriter Writer { get; set; } = null!;
}