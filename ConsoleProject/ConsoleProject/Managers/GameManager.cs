using System;
using ConsoleProject.Scenes;
public class GameManager
{
    public static bool IsGameOver { get; set; }
    public const string GameName = "폐교 탐험 (이상현상 게임)";
    private PlayerCharacter _player;

    public void Run()
    {
        Init();

        while (!IsGameOver)
        {
            

            SceneManager.Render();
            InputManager.GetUserInput();

            if (InputManager.GetKey(ConsoleKey.L))
                SceneManager.Change("Log");

            SceneManager.Update();
        }
    }

    private void Init()
    {
        IsGameOver = false;
        SceneManager.OnChangeScene += InputManager.ResetKey;
        _player = new PlayerCharacter();
        
        SceneManager.AddScene("Title", new TitleScene());
        SceneManager.AddScene("Story", new StoryScene());
        SceneManager.AddScene("Town", new TownScene(_player));
        SceneManager.AddScene("Log", new LogScene());
        
        SceneManager.Change("Title");
        
        Debug.Log("게임 데이터 초기화 완료");
    }
}