using UnityEngine;

public class TimerController : MonoBehaviour
{
    private PlayerCardUI activeCard;
    private readonly float turnTime = 30f;
    private float currentTime = 0f;
    private bool hasPlayedTickingSound = false;
    private bool isRunning;

    public float CurrentTime { get { return currentTime; } }

    public void StartTimer()
    {
        activeCard = ServiceLocator.Get<GamePageManager>().GamePage.GetPlayerCard(ServiceLocator.Get<GameManager>().CurrentTurn);

        currentTime = turnTime;
        hasPlayedTickingSound = false;
        isRunning = true;

        activeCard.UpdateTimer(currentTime, turnTime);
    }

    public void ResetTimer()
    {
        isRunning = false;
        currentTime = 0;
        hasPlayedTickingSound = false;
        ServiceLocator.Get<AudioManager>().StopTimeTickingSound();

        if (activeCard != null)
        {
            activeCard.ResetDisplay(turnTime);
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
                return;
            }

            bool isLowTime = activeCard.UpdateTimer(currentTime, turnTime);

            if (!hasPlayedTickingSound && isLowTime)
            {
                hasPlayedTickingSound = true;
                ServiceLocator.Get<AudioManager>().PlayTimeTickingSound();
            }
        }
    }
}
