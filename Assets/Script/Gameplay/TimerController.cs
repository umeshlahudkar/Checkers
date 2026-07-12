using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimerController : MonoBehaviour
{
    [SerializeField] private Color normalColor;
    [SerializeField] private Color timerRunningColor;
    [SerializeField] private Color timeUpColor;

    private Image sliderImg;
    private TextMeshProUGUI timerText;
    private readonly float turnTime = 15f;
    private float currentTime = 0f;
    private bool hasTimeUpColorSet = false;
    private bool isRunning;

    public float CurrentTime { get { return currentTime; } }

    public void StartTimer()
    {
        sliderImg = ServiceLocator.Get<GameplayUIController>().GetTimerImg(ServiceLocator.Get<GameManager>().CurrentTurn);
        timerText = ServiceLocator.Get<GameplayUIController>().GetTimerText(ServiceLocator.Get<GameManager>().CurrentTurn);

        currentTime = turnTime;
        sliderImg.color = timerRunningColor;
        sliderImg.fillAmount = 1;
        hasTimeUpColorSet = false;
        isRunning = true;

        UpdateTimerText();
    }

    public void ResetTimer()
    {
        isRunning = false;
        currentTime = 0;
        hasTimeUpColorSet = false;
        ServiceLocator.Get<AudioManager>().StopTimeTickingSound();

        if(sliderImg != null)
        {
            sliderImg.color = normalColor;
            sliderImg.fillAmount = 1;
        }
    }

    private void Update()
    {
        if (isRunning && currentTime > 0 && ServiceLocator.Get<GameManager>().GameState == GameState.Playing)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0)
            {
                currentTime = 0;
                ResetTimer();
                ServiceLocator.Get<GameManager>().HandleTurnMissCount();
            }

            sliderImg.fillAmount = currentTime / turnTime;
            UpdateTimerText();

            if(isRunning && !hasTimeUpColorSet && currentTime <= ( turnTime - (turnTime * 0.75f)))
            {
                hasTimeUpColorSet = true;
                sliderImg.color = timeUpColor;
                ServiceLocator.Get<AudioManager>().PlayTimeTickingSound();
            }
        }
    }

    private void UpdateTimerText()
    {
        if (timerText == null)
        {
            return;
        }

        int totalSeconds = Mathf.CeilToInt(currentTime);
        timerText.text = string.Format("{0}:{1:00}", totalSeconds / 60, totalSeconds % 60);
    }
}
