using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveGame()
    {
        PlayerInventory.Instance.SaveInventory();
        PerkManager.Instance.SavePerks();
    }

    public void LoadGame()
    {
        LevelManager.Instance.LoadProgress();
        PerkManager.Instance.LoadPerks();
        PlayerInventory.Instance.LoadInventory();
    }

    void OnApplicationQuit()
    {
        SaveGame();
    }
}