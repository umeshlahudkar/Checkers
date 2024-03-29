using UnityEngine;

public class ProfileManager : Singleton<ProfileManager>
{
    private bool hasUsernameSet;
    private bool hasAvtarSelect;

    private string userName;
    private Sprite profileAvtar;
    private int avtarIndex;

    [SerializeField] private Sprite computerAvtar;
    [SerializeField] private Sprite[] pieceAvtar;
    [SerializeField] private Sprite[] avtars;

    public delegate void ProfileChange(Sprite avtar, string name);
    public static event ProfileChange OnProfileChange;

    private void Start()
    {
        hasUsernameSet = false;
        hasAvtarSelect = false;
        userName = string.Empty;
        avtarIndex = -1;

#if UNITY_ANDROID || UNITY_STANDALONE_WIN //|| UNITY_EDITOR 
        SaveData data = SavingSystem.Instance.Load();

        if(data.username != string.Empty)
        {
            hasUsernameSet = true;
            userName = data.username;
        }

        if(data.avtarIndex > 0)
        {
            hasAvtarSelect = true;
            avtarIndex = data.avtarIndex;
            profileAvtar = avtars[avtarIndex - 1];
        }
#endif
        OnProfileChange?.Invoke(profileAvtar, userName);
    }

    public void SetUserName(string name)
    {
        hasUsernameSet = true;
        userName = name;

        OnProfileChange?.Invoke(profileAvtar, userName);

#if UNITY_ANDROID || UNITY_STANDALONE_WIN //|| UNITY_EDITOR 
        SaveData data = SavingSystem.Instance.Load();
        data.username = userName;
        SavingSystem.Instance.Save(data);

#endif
    }

    public void SetAvtar(int index)
    {
        if(index > 0 && index <= avtars.Length)
        {
            hasAvtarSelect = true;
            avtarIndex = index;
            profileAvtar = avtars[index - 1];

            OnProfileChange?.Invoke(profileAvtar, userName);

#if UNITY_ANDROID || UNITY_STANDALONE_WIN //|| UNITY_EDITOR 
            SaveData data = SavingSystem.Instance.Load();
            data.avtarIndex = avtarIndex;
            SavingSystem.Instance.Save(data);
#endif
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

    public Sprite GetPieceAvtar(PieceType pieceType)
    {
        if(pieceType != PieceType.None)
        {
            return pieceAvtar[(int)pieceType - 1];
        }

        return null;
    }

    public Sprite GetComputerAvtar()
    {
        return computerAvtar;
    }

    public int GetProfileAvtarIndex() 
    { 
        return hasAvtarSelect == true ? avtarIndex : -1;
    }

    public bool HasAvtarSet { get { return hasAvtarSelect; } }
    public bool HasUserNameSet { get { return hasUsernameSet; } }
}
