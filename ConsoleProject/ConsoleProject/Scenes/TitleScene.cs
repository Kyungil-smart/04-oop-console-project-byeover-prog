

public class TitleScene : Scene
{
    private MenuList _titleMenu;

    public TitleScene()
    {
        Init();
    }

    public void Init()
    {
        _titleMenu = new MenuList();
        _titleMenu.Add("게임 시작", GameStart);
        _titleMenu.Add("크레딧", ViewCredits);
        _titleMenu.Add("게임 종료", GameQuit);
    }

    public override void Enter()
    {
        _titleMenu.Reset();
        Debug.Log("타이틀 씬 진입");
    }

    public override void Update()
    {
        if (InputManager.GetKey(ConsoleKey.UpArrow))
        {
            _titleMenu.SelectUp();
        } 
        
        if (InputManager.GetKey(ConsoleKey.DownArrow))
        {
            _titleMenu.SelectDown();
        }

        if (InputManager.GetKey(ConsoleKey.Enter))
        {
            _titleMenu.Select();
        }
    }
    
    public override void Render()
    {
        Console.SetCursorPosition(5, 1);
        GameManager.GameName.Print(ConsoleColor.Yellow);
        
        _titleMenu.Render(8, 5);
    }

    public override void Exit()
    {
    }

    public void GameQuit()
    {
        GameManager.IsGameOver = true;
    }

    public void GameStart()
    {
        SceneManager.Change("Town");
    }

    public void ViewCredits()
    {
    }
}