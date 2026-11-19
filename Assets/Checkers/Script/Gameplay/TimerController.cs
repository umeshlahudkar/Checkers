using Photon.Pun;
using UnityEngine;

public class TimerController : MonoBehaviour
{
    private PlayerCardUI activeCard;
    public const float TurnTimeSeconds = 15f;

    private readonly float turnTime = TurnTimeSeconds;
    private float currentTime = 0f;
    private double turnDeadline = 0;
    private bool hasPlayedTickingSound = false;
    private bool isRunning;

    // Tracks whether PauseTimer actually froze a running countdown, as opposed to being called
    // while the timer was already stopped (enableTurnTimer off, or between turns) - isRunning alone
    // can't tell those two cases apart, and ResumeTimer must never turn a timer back on that wasn't
    // meant to be running at all.
    private bool wasPausedForCommit;

    // Set when the countdown ran out on a client that was not the master at that moment - only the
    // master may act on a timeout (see GameManager.HandleTurnMissCount), so this client dropped it.
    // If the master role is handed to this client afterwards (see FirePendingTimeout), the turn it
    // missed is still owed a timeout. Cleared by ResetTimer, i.e. when the next turn or the match end
    // comes.
    private bool timeoutPendingMaster;

    public float CurrentTime { get { return currentTime; } }

    public void StartTimer()
    {
        activeCard = ServiceLocator.Get<GamePageManager>().GamePage.GetPlayerCard(ServiceLocator.Get<GameManager>().CurrentTurn);

        currentTime = turnTime;
        turnDeadline = PhotonNetwork.Time + turnTime;
        hasPlayedTickingSound = false;
        isRunning = true;

        activeCard.UpdateTimer(currentTime, turnTime);
    }

    // Called when this client has just been made master. A turn whose deadline already passed while
    // this client was still a non-master was never timed out by anyone, so do it now - the same call
    // the countdown itself would have made. A countdown still running needs nothing: it will expire
    // normally, now as master.
    public void FirePendingTimeout()
    {
        if (!timeoutPendingMaster) { return; }

        timeoutPendingMaster = false;

        if (!PhotonNetwork.IsMasterClient || ServiceLocator.Get<GameManager>().GameState != GameState.Playing)
        {
            return;
        }

        ServiceLocator.Get<GameManager>().HandleTurnMissCount();
    }

    public void ResetTimer()
    {
        isRunning = false;
        wasPausedForCommit = false;

        // Every new turn (GameManager.StartTurn) and every match end passes through here, so a
        // pending timeout never outlives the turn it belonged to. The expiry path below sets the
        // flag only after calling this.
        timeoutPendingMaster = false;
        currentTime = 0;
        hasPlayedTickingSound = false;
        ServiceLocator.Get<AudioManager>().StopTimeTickingSound();

        if (activeCard != null)
        {
            activeCard.ResetDisplay(turnTime);
        }
    }

    // Freezes the countdown at its current remaining value rather than clearing it (unlike
    // ResetTimer) - called the instant a legal move starts committing, so the ~0.5s+ commit
    // animation (Player.HandlePieceMovementAndPieceDelete's settle waits) can never race a timeout
    // firing independently mid-animation for a turn that has, in fact, already been decided in
    // time. A no-op if the timer wasn't running to begin with.
    public void PauseTimer()
    {
        wasPausedForCommit = isRunning;
        isRunning = false;
    }

    // Resumes counting down from exactly where PauseTimer froze it - only if that call actually
    // paused a live countdown, so this can never start a timer that was never meant to run
    // (enableTurnTimer off) or that's already been fully reset for a new turn in the meantime.
    public void ResumeTimer()
    {
        if (!wasPausedForCommit) { return; }

        wasPausedForCommit = false;
        turnDeadline = PhotonNetwork.Time + currentTime;
        isRunning = true;
    }

    private void Update()
    {
        if (!isRunning || ServiceLocator.Get<GameManager>().GameState != GameState.Playing)
        {
            return;
        }

        // Counts down against a synced deadline (PhotonNetwork.Time, which also runs offline off a
        // local stopwatch) instead of accumulating Time.deltaTime, so a client resuming from being
        // backgrounded sees the real elapsed time immediately rather than a countdown that silently
        // paused while away.
        currentTime = Mathf.Max(0f, (float)(turnDeadline - PhotonNetwork.Time));

        if (currentTime <= 0)
        {
            currentTime = 0;
            ResetTimer();
            timeoutPendingMaster = !PhotonNetwork.IsMasterClient;
            ServiceLocator.Get<GameManager>().HandleTurnMissCount();
            return;
        }

        bool isLowTime = activeCard.UpdateTimer(currentTime, turnTime);

        if (!hasPlayedTickingSound && isLowTime)
        {
            hasPlayedTickingSound = true;
            ServiceLocator.Get<AudioManager>().PlayTimeTickingSound();
        }

        if (isLowTime)
        {
            float urgency = 1f - Mathf.Clamp01(currentTime / activeCard.LowTimeThreshold);
            ServiceLocator.Get<AudioManager>().SetTimeTickingUrgency(urgency);
        }
    }
}
