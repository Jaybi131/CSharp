using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class InventoryUI : MonoBehaviour
{
    [Header("Ссылки (можно оставить пустыми — найдутся автоматически)")]
    [SerializeField] private Transform inventoryRoot;   // верхний "Inventory" под Canvas
    [SerializeField] private Transform backGround;      // "BackGround"
    [SerializeField] private Transform grid;            // "Grid"
    [SerializeField] private ItemIconDatabase iconDb;   // база иконок (по itemId)

    [Header("Выбор слота")]
    [Tooltip("Сколько первых слотов используются реально (1..N → клавиши 1..N; по умолчанию 4).")]
    [SerializeField] private int usableSlots = 4;
    [Tooltip("Прокрутка колесом только по занятым слотам.")]
    [SerializeField] private bool selectOnlyFilled = false;

    [Header("Визуал")]
    [SerializeField] private Color emptyColor     = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private Color selectedColor  = new Color(1f, 1f, 1f, 0.18f);

    private PlayerInventory playerInv;
    private readonly List<Image> slotImages = new List<Image>();        // Image "item" внутри каждой ячейки
    private readonly List<Image> highlightOverlays = new List<Image>(); // накладка SelectedHL поверх ячейки
    private bool subscribed;
    private int selectedIndex = -1;

    // -------------------- LIFECYCLE --------------------
    private void Start()
    {
        AutoWireHierarchy();
        BuildSlotImages();
        ApplyUsableSlots();
        BuildHighlights();
        ClearAllSlotsVisuals();

        if (!iconDb)
            iconDb = GameObject.FindFirstObjectByType<ItemIconDatabase>();

        SetVisible(false); // инвентарь скрыт до спавна локального игрока

        StartCoroutine(BindWhenReady());
    }

    private void Update()
    {
        // управление только когда UI показан
        if (!inventoryRoot || !inventoryRoot.gameObject.activeInHierarchy)
            return;

        HandleNumberKeys();
        HandleScroll();

        // ЭКИПИРОВАТЬ выбранный слот (или первый занятый, если ничего не выбрано)
        //if (Input.GetKeyDown(KeyCode.R) && playerInv)
        //{
            //int idx = selectedIndex;
            //if (idx < 0 || (selectOnlyFilled && !IsSlotFilled(idx)))
              //  idx = FirstFilledIndex();

            //if (idx >= 0 && IsSlotFilled(idx))
            //{
            //    SelectSlot(idx);
            //    playerInv.RequestEquipSlot(idx);
          //  }
        //}
    }

    private void OnDestroy()
    {
        if (subscribed && playerInv)
            playerInv.OnInventoryChanged.RemoveListener(Redraw);
    }

    // -------------------- BIND / FIND --------------------
    private void AutoWireHierarchy()
    {
        if (!inventoryRoot)
        {
            if (string.Equals(transform.name, "Inventory", System.StringComparison.Ordinal))
                inventoryRoot = transform;
            else
            {
                var found = transform.Find("Inventory");
                if (found) inventoryRoot = found;
            }
        }

        if (!inventoryRoot)
        {
            Debug.LogWarning("[InventoryUI] Не найден верхний объект 'Inventory'. Укажи ссылку вручную.");
            return;
        }

        if (!backGround) backGround = inventoryRoot.Find("BackGround");
        if (!grid && backGround) grid = backGround.Find("Grid");
        if (!grid) Debug.LogWarning("[InventoryUI] Не найден 'Grid' под BackGround.");
    }

    private void BuildSlotImages()
    {
        slotImages.Clear();
        if (!grid) return;

        foreach (Transform cell in grid)
        {
            var img = FindItemImageRecursive(cell);
            if (img) slotImages.Add(img);
            else Debug.LogWarning($"[InventoryUI] В '{cell.name}' нет Image с именем 'item'.");
        }
    }

    private Image FindItemImageRecursive(Transform root)
    {
        var imgs = root.GetComponentsInChildren<Image>(true);
        foreach (var i in imgs)
            if (string.Equals(i.name, "item", System.StringComparison.OrdinalIgnoreCase))
                return i;
        return null;
    }

    private IEnumerator BindWhenReady()
    {
        // ждём локального игрока с PlayerInventory
        while (playerInv == null)
        {
            var all = GameObject.FindObjectsByType<PlayerInventory>(FindObjectsSortMode.None);
            foreach (var inv in all)
                if (inv.isLocalPlayer) { playerInv = inv; break; }

            if (!playerInv) { yield return null; continue; }
        }

        SetVisible(true);

        if (!subscribed)
        {
            playerInv.OnInventoryChanged.AddListener(Redraw);
            subscribed = true;
        }

        // автоселект: если selectOnlyFilled — первая занятая; иначе 0
        int first = selectOnlyFilled ? FirstFilledIndex() : 0;
        if (first >= 0) SelectSlot(first);

        Redraw(); // первичная отрисовка
    }

    // -------------------- VISUAL / REDRAW --------------------
    private void SetVisible(bool on)
    {
        if (inventoryRoot) inventoryRoot.gameObject.SetActive(on);
    }

    private void ClearAllSlotsVisuals()
    {
        foreach (var img in slotImages)
        {
            if (!img) continue;
            img.enabled = true;
            img.sprite  = null;
            img.color   = emptyColor;
            img.type    = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget  = false;
        }

        for (int i = 0; i < highlightOverlays.Count; i++)
            SetHighlight(i, false);
        selectedIndex = -1;
    }

    private void Redraw()
    {
        if (slotImages.Count == 0)
            return;

        if (!playerInv || playerInv.slots == null || playerInv.slots.Count == 0)
        {
            ClearAllSlotsVisuals();
            return;
        }

        int n = Mathf.Min(slotImages.Count, playerInv.slots.Count);

        for (int i = 0; i < n; i++)
        {
            var img = slotImages[i];
            if (!img) continue;

            string id = playerInv.slots[i];
            bool has = !string.IsNullOrEmpty(id);

            if (!has)
            {
                img.enabled = true;
                img.sprite  = null;
                img.color   = emptyColor;
                img.type    = Image.Type.Simple;
                img.preserveAspect = true;
                continue;
            }

            var sp = iconDb ? iconDb.GetIcon(id) : null;
            if (!sp)
            {
                img.enabled = true;
                img.sprite  = null;
                img.color   = emptyColor;
                img.type    = Image.Type.Simple;
                img.preserveAspect = true;
                continue;
            }

            img.enabled = true;
            img.sprite  = sp;
            img.type    = Image.Type.Simple;
            img.preserveAspect = true;
            img.color   = Color.white;
        }

        for (int i = n; i < slotImages.Count; i++)
        {
            var img = slotImages[i];
            if (!img) continue;
            img.enabled = true;
            img.sprite  = null;
            img.color   = emptyColor;
            img.type    = Image.Type.Simple;
            img.preserveAspect = true;
        }

        // поддержка подсветки после перерисовки
        if (selectedIndex >= usableSlots) selectedIndex = -1;

        if (selectOnlyFilled && selectedIndex >= 0 && !IsSlotFilled(selectedIndex))
        {
            int f = FirstFilledIndex();
            for (int i = 0; i < usableSlots; i++) SetHighlight(i, false);
            if (f >= 0) { selectedIndex = f; SetHighlight(selectedIndex, true); }
            else { selectedIndex = -1; }
        }
        else
        {
            for (int i = 0; i < usableSlots; i++) SetHighlight(i, i == selectedIndex);
        }
    }

    // -------------------- SLOT LIST / HIGHLIGHT --------------------
    private void ApplyUsableSlots()
    {
        usableSlots = Mathf.Clamp(usableSlots, 1, slotImages.Count);
        for (int i = 0; i < slotImages.Count; i++)
        {
            var img = slotImages[i];
            if (!img) continue;
            var cellRoot = img.transform.parent ? img.transform.parent.gameObject : img.gameObject;
            cellRoot.SetActive(i < usableSlots);
        }
    }

    private void BuildHighlights()
    {
        highlightOverlays.Clear();
        for (int i = 0; i < slotImages.Count; i++)
        {
            Image hlToAdd = null;

            if (i < usableSlots && slotImages[i])
            {
                Transform cell = slotImages[i].transform.parent;
                if (cell)
                {
                    var hl = cell.Find("SelectedHL")?.GetComponent<Image>();
                    if (!hl)
                    {
                        var go = new GameObject("SelectedHL", typeof(RectTransform), typeof(Image));
                        go.transform.SetParent(cell, false);
                        var r = go.GetComponent<RectTransform>();
                        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
                        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
                        hl = go.GetComponent<Image>();
                        hl.color = selectedColor;
                        hl.raycastTarget = false;
                    }
                    hl.enabled = false;
                    hlToAdd = hl;
                }
            }
            highlightOverlays.Add(hlToAdd);
        }
    }

    private void SetHighlight(int index, bool on)
    {
        if (index < 0 || index >= highlightOverlays.Count) return;
        var hl = highlightOverlays[index];
        if (hl) hl.enabled = on;
    }

    private void SelectSlot(int index)
    {
        if (usableSlots <= 0) return;
        index = Mathf.Clamp(index, 0, usableSlots - 1);

        for (int i = 0; i < usableSlots; i++) SetHighlight(i, false);

        selectedIndex = index;
        SetHighlight(selectedIndex, true);
    }

    private bool IsSlotFilled(int index)
    {
        if (!playerInv || playerInv.slots == null) return false;
        if (index < 0 || index >= playerInv.slots.Count) return false;
        return !string.IsNullOrEmpty(playerInv.slots[index]);
    }

    private int FirstFilledIndex()
    {
        if (!playerInv || playerInv.slots == null) return -1;
        int max = Mathf.Min(usableSlots, playerInv.slots.Count);
        for (int i = 0; i < max; i++)
            if (!string.IsNullOrEmpty(playerInv.slots[i])) return i;
        return -1;
    }

    private void MoveSelection(int dir)
    {
        if (usableSlots <= 0) return;

        if (selectedIndex < 0)
        {
            selectedIndex = selectOnlyFilled ? FirstFilledIndex() : 0;
            if (selectedIndex < 0) return;
            for (int i = 0; i < usableSlots; i++) SetHighlight(i, false);
            SetHighlight(selectedIndex, true);
            return;
        }

        for (int step = 0; step < usableSlots; step++)
        {
            selectedIndex = (selectedIndex + dir + usableSlots) % usableSlots;
            if (!selectOnlyFilled || IsSlotFilled(selectedIndex))
            {
                for (int i = 0; i < usableSlots; i++) SetHighlight(i, false);
                SetHighlight(selectedIndex, true);
                return;
            }
        }

        for (int i = 0; i < usableSlots; i++) SetHighlight(i, false);
        selectedIndex = -1;
    }

    // -------------------- INPUT (NUMBERS & WHEEL) --------------------
    private void HandleNumberKeys()
    {
        int pressed = -1;
        if (Input.GetKeyDown(KeyCode.Alpha1)) pressed = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) pressed = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha3)) pressed = 2;
        else if (Input.GetKeyDown(KeyCode.Alpha4)) pressed = 3;
        else if (Input.GetKeyDown(KeyCode.Alpha5)) pressed = 4;
        else if (Input.GetKeyDown(KeyCode.Alpha6)) pressed = 5;
        else if (Input.GetKeyDown(KeyCode.Alpha7)) pressed = 6;
        else if (Input.GetKeyDown(KeyCode.Alpha8)) pressed = 7;
        else if (Input.GetKeyDown(KeyCode.Alpha9)) pressed = 8;
        else if (Input.GetKeyDown(KeyCode.Alpha0)) pressed = 9;

        if (pressed < 0) return;
        if (pressed >= usableSlots) return; // учитываем реальное число слотов
        SelectSlot(pressed);

        // Если нужно — можно сразу экипировать по цифре:
        //if (playerInv && IsSlotFilled(pressed))
        //{
        //    playerInv.RequestEquipSlot(pressed);
        //}

        if (playerInv)
        {
            playerInv.RequestToggleSlot(pressed);
        }
    }
    
    private bool HandsFree()
    {
    if (!playerInv) return true;
    var all = GameObject.FindObjectsByType<PickupItem>(FindObjectsSortMode.None);
    foreach (var pi in all)
        if (pi && pi.HolderNetId == playerInv.netId)
            return false; // что-то уже в руках
    return true;
    }

    private void HandleScroll()
    {
        float dy = Input.mouseScrollDelta.y;
        if (Mathf.Abs(dy) < 0.01f) return;

        if (dy < 0f) MoveSelection(+1); // вниз — вправо
        else         MoveSelection(-1); // вверх — влево
    }

    // -------------------- ВСПОМОГАТЕЛЬНОЕ API --------------------
    public int GetSelectedIndex() => selectedIndex;
    public void SelectProgrammatically(int index) => SelectSlot(index);

    // ДОБАВИТЬ в InventoryUI

}
