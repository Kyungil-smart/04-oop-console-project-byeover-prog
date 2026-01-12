using System;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        GameManager gameManager = new GameManager();
        gameManager.Run();
    }
}