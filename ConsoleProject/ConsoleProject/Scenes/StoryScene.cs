using System;

namespace ConsoleProject.Scenes
{
    public class StoryScene : Scene
    {
        private int _page;
        private bool _dirty;

        public override void Enter()
        {
            _page = 0;
            _dirty = true;
            Console.Clear();
        }

        public override void Update()
        {
            if (InputManager.GetKey(ConsoleKey.Enter) || InputManager.GetKey(ConsoleKey.Spacebar))
            {
                _page++;
                _dirty = true;

                if (_page >= 2)
                {
                    SceneManager.Change("Town");
                }
            }

            if (InputManager.GetKey(ConsoleKey.Escape))
            {
                SceneManager.Change("Title");
            }
        }

        public override void Render()
        {
            if (!_dirty) return;
            _dirty = false;

            Console.Clear();

            if (_page == 0)
            {
                PrintStory();
                PrintFooter("Enter: 다음");
                return;
            }

            if (_page == 1)
            {
                PrintGateArt();
                PrintFooter("Enter: 들어간다");
                return;
            }
        }

        public override void Exit()
        {
        }

        private void PrintStory()
        {
            Console.WriteLine("녀석들과 내기를 한 내 자신이 미워지기 시작한다.");
            Console.WriteLine();
            Console.WriteLine("내기의 벌칙은 혼자서 폐교의 들어가 촬영을 하고 오는 것 이였다.");
            Console.WriteLine("누가 알았겠는가? 그 멍청한 내기에서 내가 질줄은...");
            Console.WriteLine();
            Console.WriteLine("온갖 후회와 혼잣말을 중얼 거리다 보니 ");
            Console.WriteLine("내 눈 앞에는 그트록 보고 싶지 않던 건물이 들어섰다.");
            Console.WriteLine();
            Console.WriteLine("귓등을 때리는 바람소리와 휘날리는 낙엽들");
            Console.WriteLine("주변 환경들이 충분히 무서운 내 심정을 더욱 자극한다.");
            Console.WriteLine();
            Console.WriteLine("호흡 한번에 한 걸음을 내딛고 학교 정문을 향한다.");
            Console.WriteLine("'그땐 몰랐다.보이는게 다가 아니라는 것을'");
            Console.WriteLine();
        }

        private void PrintGateArt()
        {
            Console.WriteLine("                 ┌───────────────────────────┐");
            Console.WriteLine("                 │     폐   교   정   문     │");
            Console.WriteLine("┌────────────────┴───────────────────────────┴────────────────-┐");
            Console.WriteLine("│                                                              │");
            Console.WriteLine("│      |||||||||||||||||||||||||||||||||||||||||||||||||       │");
            Console.WriteLine("│      ||                                             ||       │");
            Console.WriteLine("│      ||         ┌───────────────┐                   ||       │");
            Console.WriteLine("│      ||         │   출입금지    │                   ||       │");
            Console.WriteLine("│      ||         └───────────────┘                   ||       │");
            Console.WriteLine("│      ||                                             ||       │");
            Console.WriteLine("│      |||||||||||||||||||||||||||||||||||||||||||||||||       │");
            Console.WriteLine("│                                                              │");
            Console.WriteLine("│                 바람에 철문이 흔들린다.                      │");
            Console.WriteLine("└──────────────────────────────────────────────────────────────┘");
            Console.WriteLine();
            Console.WriteLine("당신에겐 선택지가 없습니다.");
            Console.WriteLine("Enter = 들어간다.");
        }

        private void PrintFooter(string text)
        {
            int y = Console.WindowHeight - 2;
            if (y < 0) y = 0;

            Console.SetCursorPosition(0, y);

            int width = Math.Max(1, Console.WindowWidth - 1);
            if (text.Length > width) text = text.Substring(0, width);

            Console.Write(text.PadRight(width));
        }
    }
}
