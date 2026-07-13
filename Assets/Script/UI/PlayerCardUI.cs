using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerCardUI : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject border;
    [SerializeField] private Image bg;
    [SerializeField] private Transform turnIndicatorParent;
    [SerializeField] private GameObject turnIndicatorTemplate;

    [Space(10)]
    [SerializeField] private float lowTimeThreshold = 5f;
    [SerializeField] private float blinkInterval = 0.25f;
    [SerializeField] private Color blinkColor = new(1f, 0.3f, 0.3f, 1f);

    private readonly List<GameObject> turnIndicators = new();
    private Color bgDefaultColor;
    private Coroutine blinkCoroutine;

    private void Awake()
    {
        bgDefaultColor = bg.color;
    }

    public void SetPlayerInfo(string playerName, Sprite avatar)
    {
        nameText.text = playerName;
        avatarImage.sprite = avatar;
    }

    public void SetTurnActive(bool isActive)
    {
        border.SetActive(isActive);
    }

    public void InitTurnIndicators(int maxMissCount)
    {
        for (int i = 0; i < turnIndicators.Count; i++)
        {
            Destroy(turnIndicators[i]);
        }
        turnIndicators.Clear();

        for (int i = 0; i < maxMissCount; i++)
        {
            GameObject indicator = Instantiate(turnIndicatorTemplate, turnIndicatorParent);
            indicator.SetActive(true);
            turnIndicators.Add(indicator);
        }
    }

    public void SetMissCount(int missCount)
    {
        for (int i = 0; i < turnIndicators.Count; i++)
        {
            turnIndicators[i].SetActive(i >= missCount);
        }
    }

    public bool UpdateTimer(float currentTime, float turnTime)
    {
        SetTimerText(currentTime);

        bool shouldBlink = currentTime > 0 && currentTime <= lowTimeThreshold;
        if (shouldBlink && blinkCoroutine == null)
        {
            blinkCoroutine = StartCoroutine(BlinkBg());
        }
        else if (!shouldBlink && blinkCoroutine != null)
        {
            StopBlinking();
        }

        return shouldBlink;
    }

    public void ResetDisplay(float turnTime)
    {
        StopBlinking();
        SetTimerText(turnTime);
    }

    private void StopBlinking()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        bg.color = bgDefaultColor;
    }

    private IEnumerator BlinkBg()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime / blinkInterval;
            bg.color = Color.Lerp(bgDefaultColor, blinkColor, Mathf.PingPong(t, 1f));
            yield return null;
        }
    }

    private void SetTimerText(float time)
    {
        int totalSeconds = Mathf.CeilToInt(time);
        timerText.text = string.Format("{0}:{1:00}", totalSeconds / 60, totalSeconds % 60);
    }
}
