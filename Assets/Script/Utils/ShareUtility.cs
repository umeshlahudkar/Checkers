using UnityEngine;

public static class ShareUtility
{
    // The message sent with a room code. The game name and the store link come from the project's
    // Player Settings (Product Name and the Android package name) at runtime, so renaming the game or
    // changing its package never needs a code change here. The link is the Play Store page for that
    // package - the game ships on Android only - and starts working once the game is published.
    public static string BuildRoomInvite(string roomCode)
    {
        return BuildRoomInvite(roomCode, DeepLinkConfig.OpenLinkEnabled);
    }

    // includeOpenLink adds the tap-to-open link. It is off until the Android manifest can handle it
    // (see DeepLinkConfig), so no message goes out with a link that does nothing.
    public static string BuildRoomInvite(string roomCode, bool includeOpenLink)
    {
        string gameName = Application.productName;
        string storeLink = $"https://play.google.com/store/apps/details?id={Application.identifier}";

        string message = $"Let's play {gameName}! Join my private room with the code {roomCode}.\n";

        if (includeOpenLink)
        {
            message += $"Tap to open the game: {DeepLinkConfig.BuildJoinLink(roomCode)}\n";
        }

        message += "Open the game, tap Join on the Friends tile, and enter the code.\n"
            + $"Get the game: {storeLink}";

        return message;
    }

    // Opens the system share sheet with the text. Android only for now - there is no native share
    // plugin in the project - so everywhere else (the editor, iOS) the text is copied instead and the
    // player is told, which still gets the code to their friend.
    public static void ShareText(string text, string chooserTitle)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
            using AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent");
            intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
            intent.Call<AndroidJavaObject>("setType", "text/plain");
            intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);

            using AndroidJavaObject chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, chooserTitle);
            using AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("startActivity", chooser);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ShareUtility] Share sheet failed, copying instead: {e.Message}");
            CopyInstead(text);
        }
#else
        CopyInstead(text);
#endif
    }

    private static void CopyInstead(string text)
    {
        GUIUtility.systemCopyBuffer = text;
        ServiceLocator.Get<WarningNotifier>().Show("Invite copied. Paste it to your friend.");
    }
}
