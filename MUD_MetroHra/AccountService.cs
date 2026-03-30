namespace MUD_MetroHra;



public class AccountService
{
    private Dictionary<string, string> _users = new();

    public bool Register(string username, string password)
    {
        if (_users.ContainsKey(username))
            return false;

        _users[username] = password;
        return true;
    }

    public bool Login(string username, string password)
    {
        return _users.TryGetValue(username, out var pass) && pass == password;
    }
}