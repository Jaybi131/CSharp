using UnityEngine;

[DisallowMultipleComponent]
public class ItemDef : MonoBehaviour
{
    [Header("ID и визуал")]
    public string itemId;           // уникальный ID предмета, например "medkit_small"
    public Sprite icon;             // иконка для UI

    [Header("Респаун")]
    public GameObject worldPrefab;  // оригинальный префаб для появления из инвентаря (если понадобится)
}
