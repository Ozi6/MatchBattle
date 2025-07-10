using UnityEngine;

[CreateAssetMenu(fileName = "New Perk", menuName = "Items/Perk")]
[System.Serializable]
public class Perk : ScriptableObject
{
    public string perkID;
    public string perkName;
    public string description;
    public Sprite icon;
    public PerkType perkType;
    public float value;
    public int requiredLevel;

    public static Perk CreatePerk(string id, string name, string desc, Sprite icon, PerkType type, float val, int level)
    {
        Perk perk = CreateInstance<Perk>();
        perk.perkID = id;
        perk.perkName = name;
        perk.description = desc;
        perk.icon = icon;
        perk.perkType = type;
        perk.value = val;
        perk.requiredLevel = level;
        return perk;
    }

    public void ApplyPerk(Player player)
    {
        switch (perkType)
        {
            case PerkType.HealthBoost:
                player.SetMaxHealth(player.GetMaxHealth() + value);
                break;
            case PerkType.DamageBoost:
                player.SetBaseDamageMultiplier(player.GetDamageMultiplier() + value);
                break;
            case PerkType.CritChanceBoost:
                player.SetBaseCritChance(player.GetBaseCritChance() + value);
                break;
        }
    }
}

public enum PerkType
{
    HealthBoost,
    DamageBoost,
    CritChanceBoost
}