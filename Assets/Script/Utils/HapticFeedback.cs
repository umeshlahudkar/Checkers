using UnityEngine;

public static class HapticFeedback
{
    // Every effect is a short on/off/on pattern rather than a single pulse: some Android devices
    // silently ignore a one-shot vibration (no exception, no buzz) but do play a multi-pulse
    // waveform. Those devices also ignore an explicit amplitude, so the pulse lengths are the only
    // strength control. Kept short and light so a capture chain is felt without becoming a buzz.
    private static readonly long[] CaptureVibrationPattern = { 0, 20, 45, 30 };
    private static readonly long[] KingPromotionVibrationPattern = { 0, 25, 50, 35, 50, 50 };
    private static readonly long[] InvalidMoveVibrationPattern = { 0, 18, 40, 18 };

    // Two effects closer together than this are played as one, so a quick run of captures is a single
    // tap instead of a drawn-out buzz. A king promotion skips this - it replaces the capture buzz
    // that is still playing, since it is the one effect worth always feeling.
    private const float MinSecondsBetweenEffects = 0.2f;

    private static float lastEffectTime = float.NegativeInfinity;

    public static void TriggerCaptureVibration()
    {
        Vibrate(CaptureVibrationPattern);
    }

    public static void TriggerKingPromotionVibration()
    {
        Vibrate(KingPromotionVibrationPattern, ignoreCooldown: true);
    }

    public static void TriggerInvalidMoveVibration()
    {
        Vibrate(InvalidMoveVibrationPattern);
    }

    private static void Vibrate(long[] pattern, bool ignoreCooldown = false)
    {
        if (!ServiceLocator.Get<GameSettingsManager>().VibrationEnabled)
        {
            return;
        }

        if (!ignoreCooldown && Time.unscaledTime - lastEffectTime < MinSecondsBetweenEffects)
        {
            return;
        }

        lastEffectTime = Time.unscaledTime;

#if UNITY_ANDROID && !UNITY_EDITOR
        PlayOnAndroid(pattern);
#elif UNITY_IOS
        // iOS has no pattern API in Unity - this is its one fixed buzz.
        Handheld.Vibrate();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private const int WaveformEffectMinApiLevel = 26;

    private static AndroidJavaObject vibrator;
    private static int apiLevel;

    private static void PlayOnAndroid(long[] pattern)
    {
        try
        {
            if (vibrator == null)
            {
                using AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
                vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                apiLevel = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
            }

            if (vibrator == null || !vibrator.Call<bool>("hasVibrator"))
            {
                return;
            }

            if (apiLevel >= WaveformEffectMinApiLevel)
            {
                // The waveform (no amplitude) form, which is the one the affected devices honour.
                using AndroidJavaClass effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                using AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>("createWaveform", pattern, -1);
                vibrator.Call("vibrate", effect);
            }
            else
            {
                vibrator.Call("vibrate", pattern, -1);
            }
        }
        catch (System.Exception exception)
        {
            // Not worth interrupting play for - drop the cached service so the next effect retries.
            Debug.LogWarning("[HapticFeedback] Vibration failed: " + exception.Message);
            vibrator = null;
        }
    }
#endif
}
