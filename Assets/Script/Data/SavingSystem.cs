using UnityEngine;
using System.IO;
using System;

public class SavingSystem : Singleton<SavingSystem>
{
    private readonly string fileName = "SaveData.json";
    private string filePath = string.Empty;

    private void Awake()
    {
#if UNITY_ANDROID || UNITY_STANDALONE_WIN //|| UNITY_EDITOR 
        filePath = Path.Combine(Application.persistentDataPath, fileName);
        //DeleteFile();
        if (!File.Exists(filePath))
        {
            SaveData data = new();
            data.username = string.Empty;
            data.avtarIndex = 0;
            data.coins = 0;

            data.audioData.isMusicMute = false;
            data.audioData.isSoundMute = false;
            data.audioData.musicVolume = 0.5f;
            data.audioData.soundVolume = 0.5f;

            data.sessionInfo = new SessionInfo();
            data.sessionInfo.firstOpenDate = DateTime.Now.ToBinary();
            data.sessionInfo.lastOpenDate = DateTime.Now.ToBinary();
            data.sessionInfo.currentSessionOfDay = 0;
            data.sessionInfo.currentSessionCount = 0;

            data.collectedReward = new bool[7];
            for(int i = 0; i < 7; i++)
            {
                data.collectedReward[i] = false;
            }

            Save(data);

            Debug.Log("Data Saved");
        }

#endif
    }

    public void Save(SaveData data)
    {
        string jsonData = JsonUtility.ToJson(data);
        File.WriteAllText(filePath, jsonData);
    }

    public SaveData Load()
    {
        if(File.Exists(filePath))
        {
            string jsonData = File.ReadAllText(filePath);
            return JsonUtility.FromJson<SaveData>(jsonData);
        }
        return default;
    }

    public void DeleteFile()
    {
        if(File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log("File delete");
        }
    }
}

[System.Serializable]
public struct SaveData
{
    public string username;
    public int avtarIndex;
    public int coins;

    public AudioData audioData;
    public SessionInfo sessionInfo;

    public bool[] collectedReward;
}

[System.Serializable]
public struct AudioData
{
    public bool isMusicMute;
    public bool isSoundMute;
    public float musicVolume;
    public float soundVolume;
}