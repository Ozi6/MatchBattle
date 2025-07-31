using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Inventory/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    public List<Character> characters;
}