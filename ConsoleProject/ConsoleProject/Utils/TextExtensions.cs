

public static class TextExtensions
{
    public static void Print(this string text, ConsoleColor color = ConsoleColor.Gray)
    {
        if (color != ConsoleColor.Gray) Console.ForegroundColor = color;

        Console.Write(text);
        
        if (color != ConsoleColor.Gray) Console.ResetColor();
    }

    public static void Print(this char character, ConsoleColor color = ConsoleColor.Gray)
    {
        if (color != ConsoleColor.Gray) Console.ForegroundColor = color;

        Console.Write(character);
        
        if (color != ConsoleColor.Gray) Console.ResetColor();
    }
    
    public static int GetTextWidth(this string text)
    {
        int width = 0;
        foreach (char c in text)
        {
            width += c.GetCharacterWidth();
        }
        return width;
    }

    public static int GetCharacterWidth(this char character)
    {
        // 한글 음절(가-힣), CJK 호환문자, 전각 기호/문자 범위는 2칸으로 처리
        if ((character >= '\uAC00' && character <= '\uD7A3') || // 한글 완성형
            (character >= '\u1100' && character <= '\u11FF') || // 한글 자모
            (character >= '\u3130' && character <= '\u318F') || // 한글 호환 자모
            (character >= '\uFF01' && character <= '\uFF60') || // 전각 기호/영숫자
            (character >= '\uFFE0' && character <= '\uFFE6'))   // 전각 특수기호
        {
            return 2;
        }
        else
        {
            return 1;
        }
    }
}