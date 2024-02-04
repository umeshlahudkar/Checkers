using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameplayTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Image sliderImg;

    [SerializeField] private Color normalColor;
    [SerializeField] private Color timeUpColor;

    private readonly float turnTime = 20f;
    private float currentTime = 0f;
    private bool hasTimeEndColorSet = false;

    public void ResetTimer()
    {
        currentTime = turnTime;
        sliderImg.color = normalColor;
        sliderImg.fillAmount = 0;
        hasTimeEndColorSet = false;
        AudioManager.Instance.StopTimeTickingSound();
    }

    private void Update()
    {
        if (currentTime > 0 && GameManager.Instance.GameState == GameState.Playing)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0)
            {
                currentTime = 0;
                AudioManager.Instance.StopTimeTickingSound();
                GameManager.Instance.UpdateTurnMissCount();
            }

            timerText.text = ((int)currentTime).ToString();
            sliderImg.fillAmount = currentTime / turnTime;

            if( !hasTimeEndColorSet && currentTime <= ( turnTime - (turnTime * 0.75f)))
            {
                hasTimeEndColorSet = true;
                sliderImg.color = timeUpColor;
                AudioManager.Instance.PlayTimeTickingSound();
            }
        }
    }
}
