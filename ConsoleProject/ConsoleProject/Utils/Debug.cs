

public static class Debug
{
    public enum LogType
    {
        Normal,
        Warning
    }
    
    private static List<(LogType type, string text)> _logList = new List<(LogType type, string text)>();

    public static void Log(string text)
    {
        _logList.Add((LogType.Normal, text));
    }

    public static void LogWarning(string text)
    {
        _logList.Add((LogType.Warning, text));
    }

    public static void Render()
    {
        foreach ((LogType type, string text) in _logList)
        {
            if (type == LogType.Normal) text.Print();
            else if (type == LogType.Warning) text.Print(ConsoleColor.Yellow);
            Console.WriteLine();
        }
    }
}