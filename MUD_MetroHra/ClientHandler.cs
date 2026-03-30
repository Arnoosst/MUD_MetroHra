namespace MUD_MetroHra;


using System.Net.Sockets;
using System.Text;

public class ClientHandler
{
    private TcpClient _client;

    public ClientHandler(TcpClient client)
    {
        _client = client;
    }

    public async Task HandleAsync()
    {
        string playerName = "Neznamy";

        try
        {
            using var stream = _client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            await writer.WriteLineAsync("Vitej v MUD Metro Hra");
            await writer.WriteLineAsync("Zadej jmeno:");

            string? name = await reader.ReadLineAsync();
            playerName = name ?? "Neznamy";

            Console.WriteLine($"[INFO] {playerName} se pripojil");

            await writer.WriteLineAsync($"Ahoj {playerName}");
            await writer.WriteLineAsync("Napis 'pomoc'");

            while (true)
            {
                string? input = await reader.ReadLineAsync();
                if (input == null) break;

                string cmd = input.Trim().ToLower();
                string response = ProcessCommand(cmd);

                await writer.WriteLineAsync(response);

                if (cmd == "konec")
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] {ex.Message}");
        }
        finally
        {
            _client.Close();
            Console.WriteLine($"[INFO] {playerName} odpojen");
        }
    }

    private string ProcessCommand(string cmd)
    {
        return cmd switch
        {
            "pomoc" => "Prikazy: pomoc, prozkoumej, konec",
            "prozkoumej" => "Jsi ve stanici Zakladna.",
            "konec" => "Bye!",
            _ => "Neznamy prikaz"
        };
    }
}