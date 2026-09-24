using System.Reflection;
using Mirror;
using UnityEngine;

[DisallowMultipleComponent]
public class InventoryHotbarExtras : NetworkBehaviour
{
    [Range(1, 10)] public int hotbarUsableSlots = 4;

    PlayerInventory inv;

    void Awake()
    {
        inv = GetComponent<PlayerInventory>();
    }

    void Update()
    {
        if (!isLocalPlayer || inv == null) return;

        int pressedIndex = GetPressedHotbarIndex();
        if (pressedIndex >= 0 && pressedIndex < hotbarUsableSlots)
        {
            if (FindHeldItemLocal() == null)
                inv.RequestEquipSlot(pressedIndex);
        }
    }

    int GetPressedHotbarIndex()
    {
        if      (Input.GetKeyDown(KeyCode.Alpha1)) return 0;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) return 1;
        else if (Input.GetKeyDown(KeyCode.Alpha3)) return 2;
        else if (Input.GetKeyDown(KeyCode.Alpha4)) return 3;
        else return -1;
    }

    PickupItem FindHeldItemLocal()
    {
        var all = GameObject.FindObjectsByType<PickupItem>(FindObjectsSortMode.None);
        foreach (var pi in all)
            if (pi && pi.HolderNetId == netId) return pi;
        return null;
    }

    [Command]
    void CmdStowHeldFirstFree()
    {
        if (inv == null || inv.slots == null) return;

        int slotIndex = -1;
        for (int i = 0; i < inv.slots.Count; i++)
            if (string.IsNullOrEmpty(inv.slots[i])) { slotIndex = i; break; }
        if (slotIndex < 0) return;

        PickupItem held = null;
        var all = GameObject.FindObjectsByType<PickupItem>(FindObjectsSortMode.None);
        foreach (var pi in all)
            if (pi && pi.HolderNetId == netId) { held = pi; break; }
        if (held == null) return;

        var def = held.GetComponent<ItemDef>();
        if (!def || string.IsNullOrEmpty(def.itemId)) return;

        inv.slots[slotIndex] = def.itemId;
        NetworkServer.Destroy(held.gameObject);

        TargetClearClientHeldAndNotify(connectionToClient);
    }

    [TargetRpc]
    void TargetClearClientHeldAndNotify(NetworkConnectionToClient _)
    {
        var pickups = GameObject.FindObjectsByType<PlayerPickup>(FindObjectsSortMode.None);
        foreach (var p in pickups)
        {
            if (!p.isLocalPlayer) continue;
            var f = typeof(PlayerPickup).GetField("heldItem",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (f != null) f.SetValue(p, null);
            break;
        }

        var invs = GameObject.FindObjectsByType<PlayerInventory>(FindObjectsSortMode.None);
        foreach (var pinv in invs)
        {
            if (pinv.isLocalPlayer)
            {
                pinv.OnInventoryChanged?.Invoke();
                break;
            }
        }
    }
}
