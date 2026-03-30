namespace MUD_MetroHra;

public class CommandProcessor
{
    public static string Process(string input, Player player)
    {
        if (input == "prozkoumej")
        {
            var room = player.CurrentRoom;

            return $"{room.Name}\n{room.Description}\n" +
                   $"Vychody: {string.Join(", ", room.Exits.Keys)}";
        }

        return "Neznamy prikaz";
    }
}