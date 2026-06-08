using UnityEngine;

/// <summary>
/// Attach to any GameObject in the scene (e.g. the SpellBook object itself,
/// or a dedicated UI manager). Listens to PlayerInteractor.OnSpellBookOpened
/// and opens/closes the spell wheel editor panel.
/// </summary>
public class SpellWheelBookListener : MonoBehaviour
{
    [Header("References")]
    public PlayerInteractor    playerInteractor;
    public SpellWheelEditorPanel editorPanel;
    public SpellWheelConfig    config;
    public SpellWheelRenderer  wheelRenderer;

    void OnEnable()
        => playerInteractor.OnSpellBookOpened += HandleSpellBookOpened;

    void OnDisable()
        => playerInteractor.OnSpellBookOpened -= HandleSpellBookOpened;

    void HandleSpellBookOpened(object sender, System.EventArgs e)
    {
        if (editorPanel.isActiveAndEnabled)
            editorPanel.Close();
        else
        {
            editorPanel.gameObject.SetActive(true);
            editorPanel.Open(config, wheelRenderer);
        }
    }
}
