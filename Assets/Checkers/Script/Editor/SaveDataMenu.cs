using UnityEditor;
using UnityEngine;

public static class SaveDataMenu
{
    [MenuItem("Checkers/Delete Save Data")]
    private static void DeleteSaveData()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Delete Save Data",
            "This will permanently delete the saved profile (username, avatar, coins) and audio settings. This cannot be undone.",
            "Delete",
            "Cancel");

        if (confirmed)
        {
            SavingSystem.Delete(ProfileData.FileName);

            PlayerPrefs.DeleteKey(AudioManager.SfxVolumeKey);
            PlayerPrefs.DeleteKey(AudioManager.SfxMuteKey);
            PlayerPrefs.DeleteKey(AudioManager.MusicVolumeKey);
            PlayerPrefs.DeleteKey(AudioManager.MusicMuteKey);
            PlayerPrefs.Save();

            EditorUtility.DisplayDialog("Delete Save Data", "Saved data deleted.", "OK");
        }
    }
}
