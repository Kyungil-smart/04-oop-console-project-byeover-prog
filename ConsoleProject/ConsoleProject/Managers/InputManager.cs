

public static class InputManager
{
    private static ConsoleKey _current;

    private static readonly ConsoleKey[] _keys =
    {
        ConsoleKey.UpArrow, 
        ConsoleKey.DownArrow, 
        ConsoleKey.LeftArrow, 
        ConsoleKey.RightArrow,
        ConsoleKey.Enter,
        ConsoleKey.I,
        ConsoleKey.L,
        ConsoleKey.T
    };

    public static bool GetKey(ConsoleKey input)
    {
        return _current == input;
    }

    // GameManager에서만 호출
    public static void GetUserInput()
    {
        ConsoleKey input = Console.ReadKey(true).Key;
        _current = ConsoleKey.Clear;

        foreach (ConsoleKey key in _keys)
        {
            if (key == input)
            {
                _current = input;
                break;
            }
        }
    }

    public static void ResetKey()
    {
        _current = ConsoleKey.Clear;
    }
}