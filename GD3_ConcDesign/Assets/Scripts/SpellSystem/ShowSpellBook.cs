using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShowSpellBook : MonoBehaviour
{
    private Image _spellBookImage;
    [SerializeField] private List<Sprite> _spellBookPages = new List<Sprite>();
    private int _spellBookIndex = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _spellBookImage = GetComponent<Image>();

        _spellBookIndex = 0;
        _spellBookImage.sprite = _spellBookPages[_spellBookIndex];
    }

    public void ShowNextSpell(InputAction.CallbackContext context)
    {
        if(context.canceled == false) return;

        _spellBookIndex++;

        Debug.Log(_spellBookIndex);

        if (_spellBookIndex >= _spellBookPages.Count)
        {
            _spellBookIndex = 0;
        }

        UpdateSpellBook();
    }

    public void ShowPreviousSpell(InputAction.CallbackContext context)
    {
        if (context.canceled == false) return;

        _spellBookIndex--;

        Debug.Log(_spellBookIndex);


        if (_spellBookIndex < 0)
        {
            _spellBookIndex = _spellBookPages.Count - 1;
        }

        UpdateSpellBook();
    }


    private void UpdateSpellBook()
    {
        _spellBookImage.sprite = _spellBookPages[_spellBookIndex];
    }
}
