using UnityEngine;

// TEMPORARY: a button on the main menu that crashes the game on purpose, to check that Crashlytics
// receives crashes. Remove the button object from MainMenuPage and delete this script before release.
//
// On a device it forces a native crash, which Crashlytics reports as a fatal crash the next time the
// game is launched. In the editor it only throws an exception, so testing the button there doesn't
// take the Unity Editor down with it.
public class CrashTestButton : MonoBehaviour
{
    public void OnCrashButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

#if UNITY_EDITOR
        throw new System.InvalidOperationException("Crashlytics test crash (editor: exception only).");
#else
        UnityEngine.Diagnostics.Utils.ForceCrash(UnityEngine.Diagnostics.ForcedCrashCategory.AccessViolation);
#endif
    }
}
