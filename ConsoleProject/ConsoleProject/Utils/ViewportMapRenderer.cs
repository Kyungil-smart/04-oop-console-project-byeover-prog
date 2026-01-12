using System;
using ConsoleProject.GameObjects;

namespace ConsoleProject.Utils
{
    public sealed class ViewportMapRenderer
    {
        private const int CellWidth = 2;

        public int ViewWidth { get; }
        public int ViewHeight { get; }

        private readonly string[,] _prev;
        private bool _forceRedraw = true;

        public ViewportMapRenderer(int viewWidth, int viewHeight)
        {
            ViewWidth = viewWidth;
            ViewHeight = viewHeight;

            _prev = new string[viewHeight, viewWidth];
            Console.CursorVisible = false;
        }

        public void ForceRedraw() => _forceRedraw = true;

        public void Render(
            Tile[,] field,
            int startX,
            Vector playerPos,
            int stage,
            string message,
            Func<int, int, string> overlayToken
        )
        {
            int mapH = field.GetLength(0);
            int mapW = field.GetLength(1);

            for (int y = 0; y < ViewHeight; y++)
            {
                int worldY = y;

                for (int x = 0; x < ViewWidth; x++)
                {
                    int worldX = startX + x;

                    string token = GetBaseTile(mapW, mapH, worldX, worldY);

                    if (worldX >= 0 && worldX < mapW && worldY >= 0 && worldY < mapH)
                    {
                        var obj = field[worldY, worldX].OnTileObject;
                        if (obj is StaticProp prop) token = prop.EmojiToken;
                    }

                    string overlay = overlayToken?.Invoke(worldX, worldY) ?? "";
                    if (!string.IsNullOrEmpty(overlay)) token = overlay;

                    if (worldX == playerPos.X && worldY == playerPos.Y) token = EmojiTiles.Player;

                    if (_forceRedraw || _prev[y, x] != token)
                    {
                        DrawCell(x, y, token);
                        _prev[y, x] = token;
                    }
                }
            }

            _forceRedraw = false;

            WriteUiLine(ViewHeight + 0, "이동: 방향키  |  이상이 보이면 ← 왼쪽 끝, 이상이 없으면 → 오른쪽 끝");
            WriteUiLine(ViewHeight + 1, "녹색선은 시작선 입니다. 이상 현상이 있을경우 녹색선 넘어서 왼쪽 끝으로 이동 해주세요");
            WriteUiLine(ViewHeight + 2, "이상이 발견되면 즉시 왼쪽으로 도망가세요!!");
            WriteUiLine(ViewHeight + 3, $"Stage: {stage}/8");
            WriteUiLine(ViewHeight + 4, message ?? "");
        }

        private static string GetBaseTile(int mapW, int mapH, int x, int y)
        {
            if (x < 0 || x >= mapW || y < 0 || y >= mapH) return EmojiTiles.Wall;
            if (y <= 1 || y >= 9) return EmojiTiles.Wall;
            return EmojiTiles.Floor;
        }

        private static void DrawCell(int x, int y, string token)
        {
            int cx = x * CellWidth;

            Console.SetCursorPosition(cx, y);
            Console.Write("  ");

            if (!string.IsNullOrEmpty(token))
            {
                Console.SetCursorPosition(cx, y);
                Console.Write(token);
            }
        }

        private static void WriteUiLine(int y, string text)
        {
            int width = Math.Max(1, Console.WindowWidth - 1);

            Console.SetCursorPosition(0, y);

            if (text.Length > width) text = text.Substring(0, width);

            Console.Write(text.PadRight(width));
        }
    }
}
