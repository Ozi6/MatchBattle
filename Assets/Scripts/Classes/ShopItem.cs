[System.Serializable]
public class ShopItem
{
    public Item item;
    public float price;
    public int stock;
    public bool isAvailable = true;

    public ShopItem(Item itemData, float itemPrice, int itemStock = 0)
    {
        item = itemData;
        price = itemPrice;
        stock = itemStock;
    }
}