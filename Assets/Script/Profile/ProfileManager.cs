using UnityEngine;

public class ProfileManager : Singleton<ProfileManager>
{
    private bool hasUsernameSet;
    private bool hasAvtarSelect;

    private string userName;
    private Sprite profileAvtar;
    private int avtarIndex;

    [SerializeField] private Sprite[] avtars;

    public delegate void ProfileChange(Sprite avtar, string name);
    public static event ProfileChange OnProfileChange;

    private void Start()
    {
        hasUsernameSet = false;
        hasAvtarSelect = false;
        userName = string.Empty;
        avtarIndex = -1;
    }

    public void SetUserName(string name)
    {
        hasUsernameSet = true;
        userName = name;

        OnProfileChange?.Invoke(profileAvtar, userName);
    }

    public void SetAvtar(int index)
    {
        if(index > 0 && index <= avtars.Length)
        {
            hasAvtarSelect = true;
            avtarIndex = index;
            profileAvtar = avtars[index - 1];

            OnProfileChange?.Invoke(profileAvtar, userName);
        }
    }

    public string GetUserName() 
    { 
        return hasUsernameSet == true ? userName : string.Empty; 
    }

    public Sprite GetProfileAvtar() 
    { 
        return hasAvtarSelect == true ? profileAvtar : null;
    }

    public Sprite GetAvtar(int index)
    {
        if(index <= avtars.Length)
        {
            return avtars[index - 1];
        }

        return null;
    }

    public int GetProfileAvtarIndex() 
    { 
        return hasAvtarSelect == true ? avtarIndex : -1;
    }

    public bool HasAvtarSet { get { return hasAvtarSelect; } }
    public bool HasUserNameSet { get { return hasUsernameSet; } }
}
