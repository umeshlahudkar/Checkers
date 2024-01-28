using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProfileManager : Singleton<ProfileManager>
{
    private bool hasUsernameSet;
    private bool hasAvtarSelect;

    private string userName;
    private Sprite selectedAvtarSprite;
    private int avtarIndex;
    private int selectAvtarIndex;

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
        avtarIndex = -1;
        selectAvtarIndex = -1;
    }

    public void SetUserName(string name)
    {
        hasUsernameSet = true;
        userName = name;

        OnProfileChange?.Invoke(selectedAvtarSprite, userName);
    }

    public void SetAvtarIndex(int index)
    {
        if (selectAvtarIndex != -1)
        {
            avtarSelectionButtons[selectAvtarIndex - 1].SetAvtarBgColor(defaultColor);
        }

        selectAvtarIndex = index;

        avtarSelectionButtons[selectAvtarIndex - 1].SetAvtarBgColor(selectedColor);
    }

    public void SetAvtar()
    {
        avtarIndex = selectAvtarIndex;
        if(avtarIndex >= 0 && avtarIndex <= avtars.Length)
        {
            hasAvtarSelect = true;
            selectedAvtarSprite = avtars[avtarIndex - 1];

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

    public Color GetDefaultBgColor()
    {
        return defaultColor;
    }

    public Color GetSelectedBgColor()
    {
        return selectedColor;
    }

    public int GetSelectedAvtarIndex() 
    { 
        return hasAvtarSelect == true ? avtarIndex : -1;
    }

    public bool HasAvtarSet { get { return hasAvtarSelect; } }
}
