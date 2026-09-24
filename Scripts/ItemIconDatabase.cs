using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class ItemIconDatabase : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public string itemId;
        public Sprite icon;
    }

    public List<Entry> entries = new List<Entry>();

    Dictionary<string, Sprite> map;

    void Awake()
    {
        map = new Dictionary<string, Sprite>();
        foreach (var e in entries)
        {
            if (!string.IsNullOrEmpty(e.itemId) && e.icon != null)
                map[e.itemId] = e.icon;
        }
    }

    public Sprite GetIcon(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        return map != null && map.TryGetValue(itemId, out var s) ? s : null;
    }
}
