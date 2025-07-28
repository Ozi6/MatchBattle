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
            Destroy(gameObject);
    }

    public void SaveGame()
    {
        PerkManager.Instance.SavePerks();
        PlayerInventory.Instance.SaveInventory();
    }

    public void LoadGame()
    {
        if (PerkManager.Instance != null)
            PerkManager.Instance.LoadPerks();

        if (LevelManager.Instance != null)
            LevelManager.Instance.LoadProgress();

        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.LoadInventory();
    }

    void OnApplicationQuit()
    {
        SaveGame();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveGame();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            SaveGame();
    }
}