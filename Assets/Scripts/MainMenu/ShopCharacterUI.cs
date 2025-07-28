using UnityEngine;

[System.Serializable]
public class ShopCharacter
{
    public Character character;
    public float price;
    public bool isAvailable = true;

    public ShopCharacter(Character character, float price)
    {
        this.character = character;
        this.price = price;
    }
}