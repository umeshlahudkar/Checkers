using UnityEngine;

public static class HapticFeedback
{
    private static readonly long[] CaptureVibrationPattern = { 0, 40 };
    private static readonly long[] KingPromotionVibrationPattern = { 0, 40, 60, 80 };
    private static readonly long[] InvalidMoveVibrationPattern = { 0, 20, 40, 20 };

    public static void TriggerCaptureVibration()
    {
        Vibrate(CaptureVibrationPattern);
    }

    public static void TriggerKingPromotionVibration()
    {
        Vibrate(KingPromotionVibrationPattern);
    }

    public static void TriggerInvalidMoveVibration()
    {
        Vibrate(InvalidMoveVibrationPattern);
    }

    private static void Vibrate(long[] pattern)
    {
        if (!ServiceLocator.Get<GameSettingsManager>().VibrationEnabled)
        {
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
            using AndroidJavaObject vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            vibrator.Call("vibrate", pattern, -1);
        }
        catch (System.Exception)
        {
            // Also what makes Unity add the VIBRATE permission to the Android manifest: it only does so
            // when the Handheld.Vibrate API is referenced, which the pattern call above is not.
            Handheld.Vibrate();
        }
#elif UNITY_IOS
        Handheld.Vibrate();
#endif
    }
}
