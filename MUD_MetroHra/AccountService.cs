namespace MUD_MetroHra;

using System.Security.Cryptography;
using System.Text.Json;

public class AccountService
{
    private readonly string _accountsPath = "accounts.json";
    private readonly Dictionary<string, AccountRecord> _accounts;

    public AccountService()
    {
        _accounts = LoadAccounts();
    }

    public bool Register(string username, string password)
    {
        if (_accounts.ContainsKey(username))
            return false;

        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = HashPassword(password, salt);

        _accounts[username] = new AccountRecord
        {
            Username = username,
            SaltBase64 = Convert.ToBase64String(salt),
            PasswordHashBase64 = Convert.ToBase64String(hash)
        };

        SaveAccounts();
        return true;
    }

    public bool Login(string username, string password)
    {
        if (!_accounts.TryGetValue(username, out var account))
            return false;

        var salt = Convert.FromBase64String(account.SaltBase64);
        var hash = HashPassword(password, salt);
        var hashBase64 = Convert.ToBase64String(hash);

        return hashBase64 == account.PasswordHashBase64;
    }

    private static byte[] HashPassword(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            10000,
            HashAlgorithmName.SHA256,
            32);
    }

    private Dictionary<string, AccountRecord> LoadAccounts()
    {
        if (!File.Exists(_accountsPath))
            return new Dictionary<string, AccountRecord>(StringComparer.OrdinalIgnoreCase);

        var json = File.ReadAllText(_accountsPath);
        var data = JsonSerializer.Deserialize<Dictionary<string, AccountRecord>>(json);

        return data ?? new Dictionary<string, AccountRecord>(StringComparer.OrdinalIgnoreCase);
    }

    private void SaveAccounts()
    {
        var json = JsonSerializer.Serialize(_accounts, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_accountsPath, json);
    }
}

public class AccountRecord
{
    public string Username { get; set; } = "";
    public string SaltBase64 { get; set; } = "";
    public string PasswordHashBase64 { get; set; } = "";
}