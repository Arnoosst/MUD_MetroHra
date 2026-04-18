namespace MUD_MetroHra;

public static class LoggerService
{
    private static readonly object LockObj = new();
    private static readonly string LogPath = "server.log";

    public static void Info(string message) => Write("INFO", message);
    public static void Error(string message) => Write("ERROR", message);
    public static void Command(string playerName, string command) => Write("CMD", $"{playerName}: {command}");

    private static void Write(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";

        lock (LockObj)
        {
            File.AppendAllText(LogPath, line + Environment.NewLine);
        }

        Console.WriteLine(line);
    }
}