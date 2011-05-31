/**
 * Global class to contain player config info. Good for passing stuff from config screens and shit for now.
 * This can go away when someone gets off their ass to write a proper config manager.
 */
public static class PlayerVars
{
    static string _playerName;
    public static string PlayerName
    {
        get
        {
            return _playerName;
        }
        set
        {
            _playerName = value;
        }
    }

}