

using System.ComponentModel.Design;

public struct Tile
{
    // 타일 위에 뭐가 올라와있는지?
    public GameObject OnTileObject { get; set; }
    // 타일 위에 올라서면 발생해야 하는 이벤트
    public event Action OnStepPlayer;
    // 자신의 좌표
    public Vector Position { get; set; }
    
    public bool HasGameObject => OnTileObject != null;

    public Tile(Vector position)
    {
        Position = position;
    }

    public void Print()
    {
        if(HasGameObject)
        {
            OnTileObject.Symbol.Print();
        }
        else
        {
            ' '.Print();
        }
    }
}