

public abstract class Item : GameObject
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public Inventory Inventory { get; set; }
    public bool InInventory { get => Inventory != null; }
    public PlayerCharacter Owner { get; set; }

    public abstract void Use();

    public void PrintInfo()
    {
    }
}