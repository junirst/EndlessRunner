using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PowerUpUI : MonoBehaviour
{
    public static PowerUpUI Instance { get; private set; }

    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform panelParent;
    [SerializeField] private int maxSlots = 3;
    [SerializeField] private Sprite[] typeIcons;

    private readonly List<ActiveSlot> activeSlots = new List<ActiveSlot>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        bool changed = false;
        for (int i = activeSlots.Count - 1; i >= 0; i--)
        {
            ActiveSlot slot = activeSlots[i];
            slot.remaining -= Time.deltaTime;
            if (slot.remaining <= 0f)
            {
                Destroy(slot.slot);
                activeSlots.RemoveAt(i);
                changed = true;
            }
            else
            {
                slot.overlay.fillAmount = 1f - (slot.remaining / slot.duration);
            }
        }
        if (changed)
            Relayout();
    }

    public void ShowPowerUp(Food.FoodType type, float duration)
    {
        Sprite icon = typeIcons[(int)type];
        if (icon == null) return;

        GameObject slotObj = GetSlot();
        if (slotObj == null) return;

        Transform slotRoot = slotObj.transform.GetChild(0);
        Image iconImg = slotRoot.Find("Icon").GetComponent<Image>();
        Image overlayImg = slotRoot.Find("Overlay").GetComponent<Image>();

        iconImg.sprite = icon;
        overlayImg.sprite = icon;
        overlayImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        overlayImg.fillAmount = 0f;
        float slotSize = ((RectTransform)panelParent).rect.height * 0.8f;
        iconImg.rectTransform.sizeDelta = new Vector2(slotSize, slotSize);
        overlayImg.rectTransform.sizeDelta = new Vector2(slotSize, slotSize);
        slotObj.SetActive(true);

        activeSlots.Add(new ActiveSlot
        {
            slot = slotObj,
            overlay = overlayImg,
            duration = duration,
            remaining = duration
        });

        Relayout();
    }

    public void ClearAll()
    {
        foreach (ActiveSlot slot in activeSlots)
            Destroy(slot.slot);
        activeSlots.Clear();
    }

    private GameObject GetSlot()
    {
        if (activeSlots.Count >= maxSlots)
            return null;
        return Instantiate(slotPrefab, panelParent);
    }

    private void Relayout()
    {
        int count = activeSlots.Count;
        if (count == 0) return;
        float spacing = ((RectTransform)panelParent).rect.height * 1.1f;
        float totalWidth = (count - 1) * spacing;
        float startX = -totalWidth * 0.5f;
        for (int i = 0; i < count; i++)
            activeSlots[i].slot.transform.localPosition = new Vector3(startX + i * spacing, 0, 0);
    }

    private class ActiveSlot
    {
        public GameObject slot;
        public Image overlay;
        public float duration;
        public float remaining;
    }
}
