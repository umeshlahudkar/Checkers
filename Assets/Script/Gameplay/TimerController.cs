using UnityEngine;
using UnityEngine.UI;

public class TimerController : MonoBehaviour
{
    [SerializeField] private Color normalColor;
    [SerializeField] private Color timerRunningColor;
    [SerializeField] private Color timeUpColor;

    private Image sliderImg;
    private readonly float turnTime = 5f;
    private float currentTime = 0f;
    private bool hasTimeUpColorSet = false;
    private bool isRunning;

    public void StartTimer()
    {
        sliderImg = GameplayUIController.Instance.GetTimerImg(GameManager.Instance.CurrentTurn);

        currentTime = turnTime;
        sliderImg.color = timerRunningColor;
        sliderImg.fillAmount = 1;
        hasTimeUpColorSet = false;
        isRunning = true;
    }

    public void ResetTimer()
    {
        isRunning = false;
        currentTime = 0;
        hasTimeUpColorSet = false;
        AudioManager.Instance.StopTimeTickingSound();

        if(sliderImg != null)
        {
            sliderImg.color = normalColor;
            sliderImg.fillAmount = 1;
        }
    }

    private void Update()
    {
        if (isRunning && currentTime > 0 && GameManager.Instance.GameState == GameState.Playing)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0)
            {
                currentTime = 0;
                ResetTimer();
                GameManager.Instance.HandleTurnMissCount();
            }

            sliderImg.fillAmount = currentTime / turnTime;

            if(isRunning && !hasTimeUpColorSet && currentTime <= ( turnTime - (turnTime * 0.75f)))
            {
                hasTimeUpColorSet = true;
                sliderImg.color = timeUpColor;
                AudioManager.Instance.PlayTimeTickingSound();
            }
        }
    }
}
