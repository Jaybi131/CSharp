using Mirror;
using TMPro;
using UnityEngine;

public class PlayerNameTag : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnDisplayNameChanged))]
    public string displayName;

    public Transform nameAnchor;
    public TextMeshPro worldText;

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = $"Player {netId}";
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplyName();
        if (worldText != null)
            worldText.gameObject.SetActive(false);
    }

    void OnDisplayNameChanged(string _, string __)
    {
        ApplyName();
    }

    void ApplyName()
    {
        if (worldText != null)
            worldText.text = displayName;
    }

    public string GetDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName;
        return $"Player {netId}";
    }
}
