namespace MUD_MetroHra;

public class Player
{
    public string Name { get; set; } = "";
    public string CurrentRoomId { get; set; } = "zakladna";
    public List<Item> Inventory { get; set; } = new();
    public int InventoryCapacity { get; set; } = 5;

    public bool HasFinishedGame { get; set; } = false;
    public DateTime? FinishedAt { get; set; }
}