using UnityEngine;
using System.IO;

public static class SavingSystem
{
    public static void Save<T>(string fileName, T data)
    {
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(GetPath(fileName), json);
    }

    public static T Load<T>(string fileName)
    {
        string path = GetPath(fileName);
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<T>(json);
        }
        return default;
    }

    public static bool Exists(string fileName)
    {
        return File.Exists(GetPath(fileName));
    }

    public static void Delete(string fileName)
    {
        string path = GetPath(fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static string GetPath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, fileName);
    }
}
