namespace CoreApp;

public class Container
{
    public string Name { get; set; }
    public string Location { get; set; }
    public List<Item> Items { get; set; }

    public void AddItem(Item item)
    {
        Items.Add(item);
    }

    public void RemoveItem(Item item)
    {
        Items.Remove(item);
    }
}
