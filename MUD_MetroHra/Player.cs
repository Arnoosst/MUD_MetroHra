namespace MUD_MetroHra;

public class Player
{
    public string Name { get; set; }
    public Room CurrentRoom { get; set; }

    public List<Item> Inventory { get; set; } = new();
}