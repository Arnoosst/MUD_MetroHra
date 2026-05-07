using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MudClient;

class Program
{
    static async Task Main(string[] args)
    {
        string host = "127.0.0.1";
        int port = 5000;

        if (args.Length >= 1) host = args[0];
        if (args.Length >= 2 && int.TryParse(args[1], out var p)) port = p;

        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        PrintHeader();

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"Pripojuji se na {host}:{port}...");
        Console.ResetColor();

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Pripojeno!\n");
            Console.ResetColor();

            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            using var cts = new CancellationTokenSource();

            var readTask = Task.Run(async () =>
            {
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync(cts.Token);
                        if (line == null) break;
                        PrintServerLine(line);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n[Spojeni se serverem bylo preruseno]");
                    Console.ResetColor();
                }
                finally
                {
                    cts.Cancel();
                }
            }, cts.Token);

            while (!cts.Token.IsCancellationRequested)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("> ");
                Console.ResetColor();

                string? input;
                try
                {
                    input = await Task.Run(() => Console.ReadLine(), cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (input == null) break;

                if (string.IsNullOrWhiteSpace(input)) continue;

                if (input.Trim().ToLower() == "/exit")
                {
                    cts.Cancel();
                    break;
                }

                if (input.Trim().ToLower() == "/help")
                {
                    PrintClientHelp();
                    continue;
                }

                try
                {
                    await writer.WriteLineAsync(input);
                }
                catch
                {
                    break;
                }

                if (input.Trim().ToLower() == "konec")
                {
                    await Task.Delay(500);
                    cts.Cancel();
                    break;
                }
            }

            try { await readTask; } catch { }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nOdpojen. Stiskni Enter pro ukonceni.");
            Console.ResetColor();
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Chyba: {ex.Message}");
            Console.WriteLine("Zkontroluj, ze server bezi a adresa/port jsou spravne.");
            Console.ResetColor();
            Console.WriteLine("\nStiskni Enter pro ukonceni.");
            Console.ReadLine();
        }
    }

    static void PrintServerLine(string line)
    {
        if (line.StartsWith("***"))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(line);
            Console.ResetColor();
        }
        else if (line.StartsWith("[") && line.Contains("]"))
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(line);
            Console.ResetColor();
        }
        else if (line.StartsWith("Vychody:"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(line);
            Console.ResetColor();
        }
        else if (line.StartsWith("Predmety:") || line.StartsWith("Inventar:") || line.StartsWith("Kapacita:"))
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(line);
            Console.ResetColor();
        }
        else if (line.StartsWith("NPC:") || line.StartsWith("Hraci:"))
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(line);
            Console.ResetColor();
        }
        else if (line.Contains("vstoupil") || line.Contains("prisel") || line.Contains("odesel") || line.Contains("opustil"))
        {
            // Pohyby hracu
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(line);
            Console.ResetColor();
        }
        else if (line.StartsWith("Chyba") || line.StartsWith("Nelze") || line.Contains("neni") || line.Contains("nemas"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(line);
            Console.ResetColor();
        }
        else if (line.StartsWith("Prikazy:") || line.StartsWith("pomoc") || line.StartsWith("prozkoumej")
                 || line.StartsWith("jdi") || line.StartsWith("vezmi") || line.StartsWith("odloz")
                 || line.StartsWith("inventar") || line.StartsWith("mluv") || line.StartsWith("konec")
                 || line.StartsWith("rekni") || line.StartsWith("krik") || line.StartsWith("pouzij"))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(line);
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(line);
            Console.ResetColor();
        }
    }

    static void PrintHeader()
    {
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║        MUD METRO HRA - KLIENT        ║");
        Console.WriteLine("╚══════════════════════════════════════╝");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Napis /help pro prikazy klienta, nebo rovnou hraj.");
        Console.ResetColor();
        Console.WriteLine();
    }

    static void PrintClientHelp()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("--- Prikazy klienta ---");
        Console.WriteLine("/help  - zobrazi tuto napovedu");
        Console.WriteLine("/exit  - ukonci klienta (bez odhlaseni)");
        Console.WriteLine("konec  - odesle 'konec' serveru a odpoji se");
        Console.WriteLine("--- Herní prikazy posli primo ---");
        Console.ResetColor();
    }
}
