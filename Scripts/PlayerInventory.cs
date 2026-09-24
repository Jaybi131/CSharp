using System.Reflection;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class PlayerInventory : NetworkBehaviour
{
    [SyncVar] double _nextEquipAllowed;

    [Header("Слоты")]
    [Range(1, 32)] public int capacity = 11;

    public readonly SyncList<string> slots = new SyncList<string>();
    public UnityEvent OnInventoryChanged;

    void Awake()
    {
        EnsureSize();
        slots.Callback += OnSlotsChanged;
    }

    public override void OnStartServer() => EnsureSize();

    public override void OnStartClient()
    {
        EnsureSize();
        OnInventoryChanged?.Invoke();
    }

    void OnDestroy() => slots.Callback -= OnSlotsChanged;

    void OnSlotsChanged(SyncList<string>.Operation op, int index, string oldItem, string newItem)
        => OnInventoryChanged?.Invoke();

    void EnsureSize()
    {
        while (slots.Count < capacity) slots.Add(string.Empty);
    }

    int FirstFreeSlotIndex()
    {
        for (int i = 0; i < slots.Count; i++)
            if (string.IsNullOrEmpty(slots[i])) return i;
        return -1;
    }

    PickupItem FindHeldItemLocal()
    {
        var all = GameObject.FindObjectsByType<PickupItem>(FindObjectsSortMode.None);
        foreach (var pi in all)
            if (pi && pi.HolderNetId == netId) return pi;
        return null;
    }

    public void RequestToggleSlot(int index)
    {
        if (!isLocalPlayer) return;
        if (index < 0 || index >= slots.Count) return;

        var held = FindHeldItemLocal();

        if (held != null)
        {
            if (string.IsNullOrEmpty(slots[index]))
                CmdStowHeldToSlot(held.GetComponent<NetworkIdentity>(), index);
            return;
        }

        if (!string.IsNullOrEmpty(slots[index]))
            CmdEquipFromSlot(index);
    }

    [Command]
    void CmdStowHeldToSlot(NetworkIdentity itemIdentity, int slotIndex)
    {
        if (CooldownActive()) return;
        if (slots == null) return;
        if (slotIndex < 0 || slotIndex >= slots.Count) return;
        if (!string.IsNullOrEmpty(slots[slotIndex])) return;
        if (!itemIdentity) return;

        var item = itemIdentity.GetComponent<PickupItem>();
        if (!item) return;
        if (item.HolderNetId != netId) return;

        var def = item.GetComponent<ItemDef>();
        if (!def || string.IsNullOrEmpty(def.itemId)) return;

        slots[slotIndex] = def.itemId;
        NetworkServer.Destroy(item.gameObject);

        TargetClearHeldAndNotify(connectionToClient);
        SetEquipCooldown();
    }

    [TargetRpc]
    void TargetClearHeldAndNotify(NetworkConnectionToClient _conn)
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

        OnInventoryChanged?.Invoke();
    }

    public void RequestEquipSlot(int index)
    {
        if (!isLocalPlayer) return;
        CmdEquipFromSlot(index);
    }

    [Command]
    void CmdEquipFromSlot(int index)
    {
        if (CooldownActive()) return;
        if (slots == null) return;
        if (index < 0 || index >= slots.Count) return;

        string id = slots[index];
        if (string.IsNullOrEmpty(id)) return;

        var allHeld = GameObject.FindObjectsByType<PickupItem>(FindObjectsSortMode.None);
        foreach (var pi in allHeld)
            if (pi && pi.HolderNetId == netId)
                return;

        var db = ItemSpawnDatabase.Instance;
        if (!db)
        {
            TargetInvDebug(connectionToClient, "[Inventory] ItemSpawnDatabase missing (server)");
            return;
        }

        var prefab = db.GetPrefab(id);
        if (!prefab)
        {
            TargetInvDebug(connectionToClient, $"[Inventory] No prefab for id={id}");
            return;
        }

        var go = Instantiate(prefab);
        var spawned = go.GetComponent<PickupItem>();
        var pickup = GetComponent<PlayerPickup>();
        Transform hp = (pickup && pickup.holdPoint) ? pickup.holdPoint : transform;

        if (!spawned || !hp)
        {
            Destroy(go);
            return;
        }

        bool ok = spawned.ServerPickup(netIdentity, hp);
        if (!ok)
        {
            Destroy(go);
            return;
        }

        go.transform.SetPositionAndRotation(
            hp.TransformPoint(spawned.heldLocalPosition),
            hp.rotation * Quaternion.Euler(spawned.heldLocalEuler));

        NetworkServer.Spawn(go, connectionToClient);

        slots[index] = string.Empty;

        // Больше не передаём NetworkIdentity через TargetRpc сразу после spawn.
        // Локальный PlayerPickup сам найдёт предмет по HolderNetId.
        TargetRefreshHeldReference(connectionToClient);

        SetEquipCooldown();
    }

    [TargetRpc]
    void TargetRefreshHeldReference(NetworkConnectionToClient _conn)
    {
        var pickups = GameObject.FindObjectsByType<PlayerPickup>(FindObjectsSortMode.None);
        foreach (var p in pickups)
        {
            if (!p.isLocalPlayer) continue;
            var method = typeof(PlayerPickup).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            break;
        }

        OnInventoryChanged?.Invoke();
    }

    void TargetInvDebug(NetworkConnectionToClient _conn, string msg)
    {
        Debug.Log(msg);
    }

    void SetEquipCooldown(double sec = 0.15)
    {
        _nextEquipAllowed = Mirror.NetworkTime.time + sec;
    }

    bool CooldownActive()
    {
        return Mirror.NetworkTime.time < _nextEquipAllowed;
    }
}
