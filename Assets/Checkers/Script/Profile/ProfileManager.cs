using System.Collections;
using UnityEngine;

public class ProfileManager : Service<ProfileManager>, IInitializable
{
    private string userName = string.Empty;
    private Sprite profileAvtar;
    private int avtarID = -1;
    private int pieceID = -1;

    [SerializeField] private Sprite computerAvtar;
    [SerializeField] private Sprite[] pieceAvtar;
    [SerializeField] private AvatarListSO avatarList;

    public delegate void ProfileChange(Sprite avtar, string name);
    public static event ProfileChange OnProfileChange;

    public IEnumerator Initialize()
    {
        userName = string.Empty;
        avtarID = -1;

#if UNITY_ANDROID || UNITY_STANDALONE_WIN || UNITY_EDITOR
        ProfileData data = SavingSystem.Load<ProfileData>(ProfileData.FileName);

        if(!string.IsNullOrEmpty(data.username))
        {
            userName = data.username;
        }

        if(data.avtarIndex > 0 && data.avtarIndex <= avatarList.avatars.Count)
        {
            avtarID = data.avtarIndex;
            profileAvtar = avatarList.avatars[avtarID - 1];
        }

        if(data.pieceIndex == (int)PieceType.White || data.pieceIndex == (int)PieceType.Black)
        {
            pieceID = data.pieceIndex;
        }
#endif

        if (string.IsNullOrEmpty(userName))
        {
            SetUserName(GameConstants.Profile.GuestNamePrefix + Random.Range(1000, 10000));
        }

        if (avtarID <= 0)
        {
            SetAvtar(Random.Range(1, avatarList.avatars.Count + 1));
        }

        if (pieceID <= 0)
        {
            SetPiece((int)PieceType.White);
        }

        OnProfileChange?.Invoke(profileAvtar, userName);

        yield break;
    }

    public void SetUserName(string name)
    {
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
            avtarID = index;
            profileAvtar = avatarList.avatars[index - 1];

            OnProfileChange?.Invoke(profileAvtar, userName);

#if UNITY_ANDROID || UNITY_STANDALONE_WIN || UNITY_EDITOR
            ProfileData data = SavingSystem.Load<ProfileData>(ProfileData.FileName);
            data.avtarIndex = avtarID;
            SavingSystem.Save(ProfileData.FileName, data);
#endif
        }
    }

    public string UserName { get { return userName; } }
    public int AvatarID { get { return avtarID; } }

    public string GetUserName()
    {
        return userName;
    }

    public Sprite GetProfileAvtar()
    {
        return profileAvtar;
    }

    public Sprite GetAvtar(int index)
    {
        if(index > 0 && index <= avatarList.avatars.Count)
        {
            return avatarList.avatars[index - 1];
        }

        return avatarList.avatars.Count > 0 ? avatarList.avatars[0] : null;
    }

    public Sprite GetPieceAvtar(PieceType pieceType)
    {
        int index = (int)pieceType - 1;

        if(pieceType != PieceType.None && index >= 0 && index < pieceAvtar.Length)
        {
            return pieceAvtar[index];
        }

        return pieceAvtar.Length > 0 ? pieceAvtar[0] : null;
    }

    public Sprite GetComputerAvtar()
    {
        return computerAvtar;
    }

    public int GetProfileAvtarID()
    {
        return avtarID;
    }

    public void SetPiece(int index)
    {
        if(index == (int)PieceType.White || index == (int)PieceType.Black)
        {
            pieceID = index;

#if UNITY_ANDROID || UNITY_STANDALONE_WIN || UNITY_EDITOR
            ProfileData data = SavingSystem.Load<ProfileData>(ProfileData.FileName);
            data.pieceIndex = pieceID;
            SavingSystem.Save(ProfileData.FileName, data);
#endif
        }
    }

    public int GetProfilePieceID()
    {
        return pieceID;
    }

    public int AvtarCount { get { return avatarList.avatars.Count; } }
}
