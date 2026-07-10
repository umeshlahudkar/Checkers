using UnityEditor;

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
            SavingSystem.Delete(AudioData.FileName);

            EditorUtility.DisplayDialog("Delete Save Data", "Saved data deleted.", "OK");
        }
    }
}
