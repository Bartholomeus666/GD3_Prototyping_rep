using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpellWheelEditorPanel : MonoBehaviour
{
    [Header("References")]
    public Transform           slotContainer;    // VerticalLayoutGroup parent
    public SpellWheelRenderer  previewRenderer;  // small live-preview wheel
    public Button              closeButton;

    public GameObject rowPrefab; 

    [Header("Spell Library")]
    [Tooltip("All spells the player is allowed to assign. Populate in Inspector.")]
    public List<SpellBase> availableSpells = new();

    [Header("Row Prefab pieces (assign in Inspector)")]
    [Tooltip("Font used for runtime-created Text components in rows.")]
    public Font rowFont;
    public int rowFontSize = 16;

    // Runtime
    private SpellWheelConfig   _config;
    private SpellWheelRenderer _gameRenderer;   // the actual gameplay wheel renderer
    private readonly List<GameObject> _rows = new();

    void Awake()
    {
        gameObject.SetActive(false);
        if (closeButton) closeButton.onClick.AddListener(Close);
    }

    public void Open(SpellWheelConfig config, SpellWheelRenderer gameRenderer)
    {
        _config       = config;
        _gameRenderer = gameRenderer;
        gameObject.SetActive(true);
        BuildRows();
        RefreshPreview();
    }

    public void Close()
    {
        _config.Normalize();
        _gameRenderer?.Rebuild(); // apply changes to gameplay wheel
        gameObject.SetActive(false);
    }

    // ── Row building ─────────────────────────────────────────────────────────

    void BuildRows()
    {
        ClearRows();
        for (int i = 0; i < _config.slots.Count; i++)
            _rows.Add(BuildRow(i));
    }

    GameObject BuildRow(int index)
    {
        var slot = _config.slots[index];

        GameObject row = Instantiate(rowPrefab, slotContainer);
        row.name = $"Row_{index}";

        //// ── Colour swatch ──
        Transform firstChild = row.transform.GetChild(0);
        Image swatch = firstChild.GetComponent<Image>();
        swatch.GetComponent<Image>().color = slot.spell.wheelColor;



        // ── Percent label ──
        TMP_Text pctLabel = row.GetComponentInChildren<TMP_Text>();
        pctLabel.text = $"{Mathf.Round(_config.slots[index].percent * 100f)}%";


        Slider slider = row.GetComponentInChildren<Slider>();
        if (slider) { slider.SetValueWithoutNotify(_config.slots[index].percent * 100f); }


        slider.onValueChanged.AddListener(v =>
        {
            _config.slots[index].percent = v / 100;
            _config.Normalize();

            // Refresh all sliders to reflect normalization
            RefreshSliderValues();
            RefreshPreview();
        });

        return row;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void RefreshPreview()
    {
        if (previewRenderer) previewRenderer.Show(_config);
    }

    void RefreshSliderValues()
    {
        // Rows correspond 1:1 to config slots (last row is the Add/Remove row)
        for (int i = 0; i < _rows.Count; i++)
        {
            var slider = _rows[i].GetComponentInChildren<Slider>();
            if (slider) { slider.SetValueWithoutNotify(_config.slots[i].percent * 100f); }

            var pctLabel = _rows[i].GetComponentInChildren<TMP_Text>();
            pctLabel.text = $"{Mathf.Round(_config.slots[i].percent * 100f)}%";
        }
    }

    void ClearRows()
    {
        foreach (var r in _rows) if (r) Destroy(r);
        _rows.Clear();
    }
}
