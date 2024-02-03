using UnityEngine;
using System.IO;

public class SavingSystem : Singleton<SavingSystem>
{
    private readonly string fileName = "SaveData.json";
    private string filePath = string.Empty;

    private void Start()
    {
        filePath = Path.Combine(Application.persistentDataPath, fileName);

        if(!File.Exists(filePath))
        {
            SaveData data = new();
            data.username = "Checkers" + Random.Range(101, 999);
            data.avtarIndex = Random.Range(1, 30);
            data.coins = 1000;

            Save(data);
        }
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
        }
    }
}

[System.Serializable]
public struct SaveData
{
    public string username;
    public int avtarIndex;
    public int coins;
}
