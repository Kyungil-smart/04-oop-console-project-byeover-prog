

public struct Ractangle
{
    public int X;
    public int Y;
    public int Width;
    public int Height;

    public Ractangle(int x = 0, int y = 0, int width = 2, int height = 2)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
    
    public void Draw()
    {
        if (Width < 2 || Height < 2) return;

        int bw = Console.BufferWidth;
        int bh = Console.BufferHeight;

        // 좌표/크기가 버퍼 밖이면 그리지 않음
        if (X < 0 || Y < 0) return;
        if (X >= bw || Y >= bh) return;
        if (X + Width - 1 >= bw) return;
        if (Y + Height - 1 >= bh) return;

        // 맨 윗줄
        Console.SetCursorPosition(X, Y);
        for (int i = 0; i < Width; i++)
        {
            if (i == 0 || i == Width - 1) '*'.Print();
            else '-'.Print();
        }

        // 중간
        for (int i = 1; i < Height - 1; i++)
        {
            Console.SetCursorPosition(X, Y + i);
            '|'.Print();

            for (int j = 1; j < Width - 1; j++)
            {
                Console.SetCursorPosition(X + j, Y + i);
                ' '.Print();
            }

            Console.SetCursorPosition(X + Width - 1, Y + i);
            '|'.Print();
        }

        // 맨 아랫줄
        Console.SetCursorPosition(X, Y + Height - 1);
        for (int i = 0; i < Width; i++)
        {
            if (i == 0 || i == Width - 1) '*'.Print();
            else '-'.Print();
        }
    }
}