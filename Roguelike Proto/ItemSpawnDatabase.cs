using System.Collections.Generic;
using UnityEngine;

public class ItemSpawnDatabase : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public string itemId;     // Тот же ID, что в ItemDef.itemId и в инвентаре
        public GameObject prefab; // Префаб с PickupItem + NetworkIdentity + Rigidbody + Collider
    }

    [Header("Пары itemId → prefab")]
    public List<Entry> entries = new List<Entry>();

    private static ItemSpawnDatabase _instance;
    public static ItemSpawnDatabase Instance
    {
        get
        {
            if (_instance == null) _instance = FindFirstObjectByType<ItemSpawnDatabase>();
            return _instance;
        }
    }

    public GameObject GetPrefab(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var e in entries)
            if (e != null && e.prefab && e.itemId == id)
                return e.prefab;
        return null;
    }
}
