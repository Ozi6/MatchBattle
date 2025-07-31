using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CharacterSelectUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject characterSelectPanel;
    [SerializeField] private Transform characterButtonContainer;
    [SerializeField] private Button characterButtonPrefab;
    [SerializeField] private RawImage selectedCharacterDisplay;
    [SerializeField] private TextMeshProUGUI selectedCharacterName;
    [SerializeField] private Transform blockTypeContainer;
    [SerializeField] private GameObject blockImagePrefab;
    [SerializeField] private GameObject purchasePopup;
    [SerializeField] private TextMeshProUGUI purchaseCharacterName;
    [SerializeField] private TextMeshProUGUI purchaseCostText;
    [SerializeField] private Transform purchaseBlockTypeContainer;
    [SerializeField] private RawImage purchaseCharacterImage;
    [SerializeField] private Button purchaseConfirmButton;
    [SerializeField] private Button purchaseCancelButton;
    [SerializeField] private TextMeshProUGUI currencyText;

    [Header("Block Data")]
    [SerializeField] private BlockData[] blockDataArray;

    [Header("Audio")]
    [SerializeField] private AudioSource buttonClickSound;

    private List<Character> characters;
    private Character selectedCharacter;
    private Character characterToPurchase;
    private Dictionary<BlockType, BlockData> blockDataDict;

    void Start()
    {
        InitializeBlockData();
        InitializeCharacterSelection();
        SetupPurchasePopup();
    }

    private void InitializeBlockData()
    {
        blockDataDict = new Dictionary<BlockType, BlockData>();
        foreach (BlockData data in blockDataArray)
            blockDataDict[data.type] = data;
    }

    private void InitializeCharacterSelection()
    {
        if (PlayerInventory.Instance == null)
        {
            Debug.LogError("PlayerInventory instance not found!");
            return;
        }

        characters = PlayerInventory.Instance.GetAvailableCharacters();
        selectedCharacter = PlayerInventory.Instance.GetSelectedCharacter();
        UpdateCurrencyDisplay();

        foreach (Transform child in characterButtonContainer)
            Destroy(child.gameObject);

        foreach (Character character in characters)
        {
            Button characterButton = Instantiate(characterButtonPrefab, characterButtonContainer);
            RawImage buttonImage = characterButton.GetComponent<RawImage>();
            if (buttonImage == null)
                buttonImage = characterButton.GetComponentInChildren<RawImage>();

            if (buttonImage != null)
                buttonImage.texture = character.characterRenderTexture;

            TextMeshProUGUI buttonText = characterButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = character.isLocked ? "Locked" : character.characterName;

            characterButton.onClick.AddListener(() => OnCharacterButtonClicked(character));

            if (character.isLocked && buttonImage != null)
            {
                Color buttonColor = buttonImage.color;
                buttonColor.a = 0.5f;
                buttonImage.color = buttonColor;
            }
        }

        UpdateSelectedCharacterDisplay();
    }

    private void SetupPurchasePopup()
    {
        purchasePopup.SetActive(false);
        if (purchaseConfirmButton != null)
            purchaseConfirmButton.onClick.AddListener(ConfirmPurchase);
        if (purchaseCancelButton != null)
            purchaseCancelButton.onClick.AddListener(CancelPurchase);
    }

    private void OnCharacterButtonClicked(Character character)
    {
        PlayButtonSound();
        if (character.isLocked)
        {
            characterToPurchase = character;
            purchaseCharacterName.text = character.characterName;
            purchaseCostText.text = $"Cost: {character.purchaseCost} Coins";
            CreateBlockTypeImages(purchaseBlockTypeContainer, character.blockTypes);
            if (purchaseCharacterImage != null)
                purchaseCharacterImage.texture = character.characterRenderTexture;
            purchasePopup.SetActive(true);
        }
        else
        {
            PlayerInventory.Instance.SelectCharacter(character.characterID);
            selectedCharacter = character;
            UpdateSelectedCharacterDisplay();
        }
    }

    private void ConfirmPurchase()
    {
        PlayButtonSound();
        if (PlayerInventory.Instance.UnlockCharacter(characterToPurchase.characterID))
        {
            InitializeCharacterSelection();
            purchasePopup.SetActive(false);
            UpdateCurrencyDisplay();
        }
    }

    private void CancelPurchase()
    {
        PlayButtonSound();
        purchasePopup.SetActive(false);
    }

    private void UpdateSelectedCharacterDisplay()
    {
        if (selectedCharacter != null)
        {
            selectedCharacterDisplay.texture = selectedCharacter.characterRenderTexture;
            selectedCharacterName.text = selectedCharacter.characterName;
            CreateBlockTypeImages(blockTypeContainer, selectedCharacter.blockTypes);
        }
    }

    private void CreateBlockTypeImages(Transform container, List<BlockType> blockTypes)
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);

        foreach (BlockType blockType in blockTypes)
        {
            GameObject blockImageObj = Instantiate(blockImagePrefab, container);
            Image blockImage = blockImageObj.GetComponent<Image>();

            if (blockDataDict.ContainsKey(blockType))
            {
                BlockData blockData = blockDataDict[blockType];
                blockImage.sprite = blockData.sprite;
                blockImage.color = blockData.color;
            }
            else
            {
                blockImage.sprite = null;
                blockImage.color = Color.white;
            }
        }
    }

    private void UpdateCurrencyDisplay()
    {
        currencyText.text = $"Coins: {PlayerInventory.Instance.GetCurrency()}";
    }

    private void PlayButtonSound()
    {
        if (buttonClickSound != null)
            buttonClickSound.Play();
    }
}