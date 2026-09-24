using TMPro;
using UnityEngine;

public class LocalLookHUD : MonoBehaviour
{
    public CanvasGroup pickupPromptGroup;
    public TextMeshProUGUI pickupPromptText;
    public RectTransform playerNameRoot;
    public TextMeshProUGUI playerNameText;
    public Camera targetCamera;
    public Vector3 playerNameScreenOffset = new Vector3(0f, 30f, 0f);

    void Awake()
    {
        HideAll();
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (playerNameRoot != null && playerNameRoot.gameObject.activeSelf && _hasPlayerNameWorldPos)
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
            if (targetCamera == null) return;

            Vector3 screen = targetCamera.WorldToScreenPoint(_playerNameWorldPos);
            bool visible = screen.z > 0f;
            playerNameRoot.gameObject.SetActive(visible);
            if (visible)
                playerNameRoot.position = screen + playerNameScreenOffset;
        }
    }

    Vector3 _playerNameWorldPos;
    bool _hasPlayerNameWorldPos;

    public void ShowPickupPrompt(string text = "Pick up / Взять")
    {
        if (pickupPromptText != null)
            pickupPromptText.text = text;
        if (pickupPromptGroup != null)
        {
            pickupPromptGroup.alpha = 1f;
            pickupPromptGroup.interactable = false;
            pickupPromptGroup.blocksRaycasts = false;
        }
    }

    public void HidePickupPrompt()
    {
        if (pickupPromptGroup != null)
        {
            pickupPromptGroup.alpha = 0f;
            pickupPromptGroup.interactable = false;
            pickupPromptGroup.blocksRaycasts = false;
        }
    }

    public void ShowPlayerName(string nameText, Vector3 worldPos)
    {
        _playerNameWorldPos = worldPos;
        _hasPlayerNameWorldPos = true;
        if (playerNameText != null)
            playerNameText.text = nameText;
        if (playerNameRoot != null)
            playerNameRoot.gameObject.SetActive(true);
    }

    public void HidePlayerName()
    {
        _hasPlayerNameWorldPos = false;
        if (playerNameRoot != null)
            playerNameRoot.gameObject.SetActive(false);
    }

    public void HideAll()
    {
        HidePickupPrompt();
        HidePlayerName();
    }
}
