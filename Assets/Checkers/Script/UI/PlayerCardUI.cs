using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerCardUI : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI piecesLeftText;
    [SerializeField] private Image bg;
    [SerializeField] private Image pieceIcon;
    [SerializeField] private Image iconBg;


    [SerializeField] private Sprite selectedBgSprite;
    [SerializeField] private Sprite unSelectedBgSprite;
    [SerializeField] private Sprite selectedIconBgSprite;
    [SerializeField] private Sprite unSelectedIconBgSprite;

    [Header("Turn Indicator")]
    [SerializeField] private Sprite activeIndicatorSprite;
    [SerializeField] private Sprite disableIndicatorSprite;
    [SerializeField] private GameObject turnIndicatorMainParent;
    [SerializeField] private Transform turnIndicatorParent;
    [SerializeField] private GameObject turnIndicatorTemplate;

    [Space(10)]
    [SerializeField] private float lowTimeThreshold = 5f;
    [SerializeField] private float blinkInterval = 0.25f;
    [SerializeField] private Color blinkColor = new(1f, 0.3f, 0.3f, 1f);

    [Header("Turn Activation Juice")]
    [SerializeField] private float activationTweenDuration = 0.2f;

    private static readonly Color SelectedContentColor = Color.white;
    private static readonly Color UnselectedContentColor = new Color32(0x84, 0x94, 0xAC, 0xFF);

    private readonly List<Image> turnIndicators = new();
    private Color bgDefaultColor;
    private Coroutine blinkCoroutine;
    private int totalPieces;
    private float blinkRemainingTime;

    public RectTransform RectTransform { get { return rectTransform; } }

    public float LowTimeThreshold { get { return lowTimeThreshold; } }

    private void Awake()
    {
        bgDefaultColor = bg.color;
    }

    public void SetPlayerInfo(string playerName, Sprite avatar, Sprite pieceSprite)
    {
        nameText.text = playerName;
        avatarImage.sprite = avatar;
        pieceIcon.sprite = pieceSprite;
    }

    public void SetTurnActive(bool isActive)
    {
        bg.sprite = isActive ? selectedBgSprite : unSelectedBgSprite;
        iconBg.sprite = isActive ? selectedIconBgSprite : unSelectedIconBgSprite;

        Color contentColor = isActive ? SelectedContentColor : UnselectedContentColor;
        avatarImage.DOKill();
        avatarImage.DOColor(contentColor, activationTweenDuration);
        nameText.DOKill();
        nameText.DOColor(contentColor, activationTweenDuration);
        timerText.DOKill();
        timerText.DOColor(contentColor, activationTweenDuration);
        piecesLeftText.DOKill();
        piecesLeftText.DOColor(contentColor, activationTweenDuration);
        pieceIcon.DOKill();
        pieceIcon.DOColor(contentColor, activationTweenDuration);

        for (int i = 0; i < turnIndicators.Count; i++)
        {
            Image indicator = turnIndicators[i];
            indicator.DOKill();
            indicator.DOColor(contentColor, activationTweenDuration);
        }
    }

    public void InitPiecesLeft(int total)
    {
        totalPieces = total;
        SetPiecesLeft(total);
    }

    public void SetPiecesLeft(int count)
    {
        piecesLeftText.text = $": {count}/{totalPieces}";
    }

    // A punch-scale pop on the piece icon, played whenever this player loses a piece to a capture
    // (see Piece.Destroy) - draws the eye to the card whose count just dropped.
    public void PlayPieceCapturedAnimation()
    {
        pieceIcon.rectTransform.DOKill();
        pieceIcon.rectTransform.localScale = Vector3.one;
        pieceIcon.rectTransform.DOPunchScale(Vector3.one * 0.3f, 0.4f, vibrato: 8, elasticity: 0.6f);
    }

    public void InitTurnIndicators(int maxMissCount)
    {
        // maxMissCount is 0 for modes that don't enforce a turn timer (VsBot/VsPlayer offline - see
        // GameManager.SetupLocalMatch) - hide the whole row rather than leave an empty parent.
        turnIndicatorMainParent.SetActive(maxMissCount > 0);

        for (int i = 0; i < turnIndicators.Count; i++)
        {
            Destroy(turnIndicators[i].gameObject);
        }
        turnIndicators.Clear();

        for (int i = 0; i < maxMissCount; i++)
        {
            GameObject indicator = Instantiate(turnIndicatorTemplate, turnIndicatorParent);
            indicator.SetActive(true);

            Image indicatorImage = indicator.GetComponent<Image>();
            indicatorImage.sprite = activeIndicatorSprite;
            turnIndicators.Add(indicatorImage);
        }
    }

    // Indicators stay active/visible for the whole match - a miss swaps its sprite to the disabled
    // look instead of hiding the GameObject, so the row of dots never shifts/reflows as misses come
    // in. Disables from the last indicator backward, so the first ones stay lit longest.
    public void SetMissCount(int missCount)
    {
        int disableFromIndex = turnIndicators.Count - missCount;
        for (int i = 0; i < turnIndicators.Count; i++)
        {
            turnIndicators[i].sprite = i >= disableFromIndex ? disableIndicatorSprite : activeIndicatorSprite;
        }
    }

    public void SetTimerVisible(bool visible)
    {
        timerText.gameObject.SetActive(visible);
    }

    public bool UpdateTimer(float currentTime, float turnTime)
    {
        SetTimerText(currentTime);
        blinkRemainingTime = currentTime;

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
        timerText.rectTransform.localScale = Vector3.one;
    }

    // Blink speed and the timer text's pulse both ramp up as remainingTime approaches zero,
    // so the last couple seconds feel more urgent than the moment the blink first kicks in.
    private IEnumerator BlinkBg()
    {
        float t = 0f;
        while (true)
        {
            float urgency = 1f - Mathf.Clamp01(blinkRemainingTime / lowTimeThreshold);
            float currentInterval = Mathf.Lerp(blinkInterval, blinkInterval * 0.4f, urgency);

            t += Time.deltaTime / currentInterval;
            float pingPong = Mathf.PingPong(t, 1f);

            bg.color = Color.Lerp(bgDefaultColor, blinkColor, pingPong);
            float pulseScale = Mathf.Lerp(0.08f, 0.18f, urgency);
            timerText.rectTransform.localScale = Vector3.one * (1f + pulseScale * pingPong);

            yield return null;
        }
    }

    private void SetTimerText(float time)
    {
        int totalSeconds = Mathf.CeilToInt(time);
        timerText.text = string.Format("{0}:{1:00}", totalSeconds / 60, totalSeconds % 60);
    }
}
