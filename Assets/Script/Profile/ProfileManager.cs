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
    [SerializeField] private AvatarListSO avatarList;

    public delegate void ProfileChange(Sprite avtar, string name);
    public static event ProfileChange OnProfileChange;

    private void Start()
    {
        hasUsernameSet = false;
        hasAvtarSelect = false;
        userName = string.Empty;
        avtarIndex = -1;

#if UNITY_ANDROID || UNITY_STANDALONE_WIN || UNITY_EDITOR
        ProfileData data = SavingSystem.Load<ProfileData>(ProfileData.FileName);

        if(!string.IsNullOrEmpty(data.username))
        {
            hasUsernameSet = true;
            userName = data.username;
        }

        if(data.avtarIndex > 0 && data.avtarIndex <= avatarList.avatars.Count)
        {
            hasAvtarSelect = true;
            avtarIndex = data.avtarIndex;
            profileAvtar = avatarList.avatars[avtarIndex - 1];
        }
#endif

        if (!hasUsernameSet)
        {
            SetUserName("Random_" + Random.Range(1000, 10000));
        }

        if (!hasAvtarSelect)
        {
            SetAvtar(Random.Range(1, avatarList.avatars.Count + 1));
        }

        OnProfileChange?.Invoke(profileAvtar, userName);
    }

    public void SetUserName(string name)
    {
        hasUsernameSet = true;
        userName = name;

        OnProfileChange?.Invoke(profileAvtar, userName);

#if UNITY_ANDROID || UNITY_STANDALONE_WIN || UNITY_EDITOR
        ProfileData data = SavingSystem.Load<ProfileData>(ProfileData.FileName);
        data.username = userName;
        SavingSystem.Save(ProfileData.FileName, data);
#endif
    }

    public void SetAvtar(int index)
    {
        if(index > 0 && index <= avatarList.avatars.Count)
        {
            hasAvtarSelect = true;
            avtarIndex = index;
            profileAvtar = avatarList.avatars[index - 1];

            OnProfileChange?.Invoke(profileAvtar, userName);

#if UNITY_ANDROID || UNITY_STANDALONE_WIN || UNITY_EDITOR
            ProfileData data = SavingSystem.Load<ProfileData>(ProfileData.FileName);
            data.avtarIndex = avtarIndex;
            SavingSystem.Save(ProfileData.FileName, data);
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
        if(index > 0 && index <= avatarList.avatars.Count)
        {
            return avatarList.avatars[index - 1];
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
    public int AvtarCount { get { return avatarList.avatars.Count; } }
}
