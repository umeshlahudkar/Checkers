using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProfileManager : Singleton<ProfileManager>
{
    private bool hasUsernameSet;
    private bool hasAvtarSelect;

    private string userName;
    private Sprite selectedAvtarSprite;
    private int selectedAvtarIndex;

    [SerializeField] private Color defaultColor;
    [SerializeField] private Color selectedColor;

    [SerializeField] private Sprite[] avtars;
    [SerializeField] private AvtarSelectionButton[] avtarSelectionButtons;

    public delegate void ProfileChange(Sprite avtar, string name);
    public static event ProfileChange OnProfileChange;

    private void Start()
    {
        hasUsernameSet = false;
        hasAvtarSelect = false;
        userName = string.Empty;
        selectedAvtarIndex = -1;
    }

    public void SetUserName(string name)
    {
        hasUsernameSet = true;
        userName = name;

        OnProfileChange?.Invoke(selectedAvtarSprite, userName);
    }

    public void SetAvtarIndex(int index)
    {
        if (selectedAvtarIndex != -1)
        {
            avtarSelectionButtons[selectedAvtarIndex - 1].SetAvtarBgColor(defaultColor);
        }

        selectedAvtarIndex = index;

        avtarSelectionButtons[selectedAvtarIndex - 1].SetAvtarBgColor(selectedColor);
    }

    public void SetAvtar()
    {
        if(selectedAvtarIndex >= 0 && selectedAvtarIndex <= avtars.Length)
        {
            hasAvtarSelect = true;
            selectedAvtarSprite = avtars[selectedAvtarIndex - 1];

            OnProfileChange?.Invoke(selectedAvtarSprite, userName);
        }
    }

    public string GetUserName() 
    { 
        return hasUsernameSet == true ? userName : string.Empty; 
    }

    public Sprite GetSelectedAvtarSprite() 
    { 
        return hasAvtarSelect == true ? selectedAvtarSprite : null;
    }

    public Sprite GetSprite(int index)
    {
        if(index <= avtars.Length)
        {
            return avtars[index - 1];
        }

        return null;
    }

    public int GetSelectedAvtarIndex() 
    { 
        return hasAvtarSelect == true ? selectedAvtarIndex : -1;
    }
}
