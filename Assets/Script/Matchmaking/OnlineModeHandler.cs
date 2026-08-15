using UnityEngine;
using Photon.Realtime;

public class OnlineModeHandler : MatchModeHandler
{
    private const float MatchmakingTimeoutSeconds = 15f;
    private const float BotFallbackLeadTimeSeconds = 3f;
    private const float PreGameCountdownSeconds = 3f;

    private bool isCancelled;
    private bool isTimerRunning;
    private float matchmakingElapsed;
    private int lastDisplayedSeconds;

    private bool isPreGameCountdownRunning;
    private float preGameCountdownElapsed;
    private int lastDisplayedCountdown;

    private bool isBotFallbackPending;
    private string pendingDisguisedName;
    private Sprite pendingDisguisedAvatar;

    public OnlineModeHandler(MatchmakingConnectionManager connectionManager, GameDataSO gameDataSO)
        : base(connectionManager, gameDataSO)
    {
    }

    public override GameModeType Mode => GameModeType.Multiplayer;

    public override void StartMatch()
    {
        gameDataSO.gameMode = Mode;

        // This handler instance is reused across every online match attempt, so a previous
        // cycle's leftover state (e.g. a cancel that landed mid pre-game-countdown) must not
        // bleed into this new one.
        ResetMatchState();

        ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.Matchmaking);

        if(connectionManager.IsConnectedAndReady)
        {
            if(!connectionManager.JoinRandomRoom())
            {
                connectionManager.CreateRoom();
            }
        }
        else
        {
            connectionManager.Connect();
        }
    }

    public override void CancelMatch()
    {
        isCancelled = true;
        StopMatchmakingTimer();
        StopPreGameCountdown();
        // Disconnect entirely (not just leave the room) - canceling means the player isn't
        // matchmaking anymore, so there's no reason to keep an idle Photon connection open.
        connectionManager.Disconnect();
        ServiceLocator.Get<MenuPageManager>().GoBack();
    }

    public override void Update()
    {
        if(isTimerRunning)
        {
            UpdateMatchmakingTimer();
        }

        if(isPreGameCountdownRunning)
        {
            UpdatePreGameCountdown();
        }
    }

    private void UpdateMatchmakingTimer()
    {
        matchmakingElapsed += Time.deltaTime;
        float remaining = MatchmakingTimeoutSeconds - matchmakingElapsed;

        if(remaining <= BotFallbackLeadTimeSeconds)
        {
            StopMatchmakingTimer();
            OnMatchmakingTimeout();
            return;
        }

        int displaySeconds = Mathf.CeilToInt(remaining);
        if(displaySeconds != lastDisplayedSeconds)
        {
            lastDisplayedSeconds = displaySeconds;
            matchmakingPage.UpdateRemainingTime(displaySeconds);
        }
    }

    private void UpdatePreGameCountdown()
    {
        preGameCountdownElapsed += Time.deltaTime;
        float remaining = PreGameCountdownSeconds - preGameCountdownElapsed;

        if(remaining <= 0f)
        {
            Debug.Log($"[OnlineModeHandler] Pre-game countdown finished, loading gameplay scene - time: {Time.realtimeSinceStartup}, isMasterClient: {connectionManager.IsMasterClient}");

            isPreGameCountdownRunning = false;
            matchmakingPage.ShowPreGameCountdown("GO!");

            if(isBotFallbackPending)
            {
                isBotFallbackPending = false;

                // Starting a VsBot match sets gameDataSO.opponentPlayer to "Computer" (with the correct
                // piece type) via PvcModeHandler; overwrite the name/avatar afterwards so the player
                // believes they matched with a real opponent, but keep the piece type it assigned.
                connectionManager.StartMatch(GameModeType.VsBot);

                gameDataSO.opponentPlayer = new PlayerInfo
                {
                    userName = pendingDisguisedName,
                    avatar = pendingDisguisedAvatar,
                    pieceType = gameDataSO.opponentPlayer.pieceType
                };

                // PvcModeHandler.StartMatch just set gameMode to VsBot (and opponentIsBot to true,
                // which we deliberately keep) - restore Multiplayer so every gameMode-driven UI/rule
                // difference (draw offer button shown, no Undo, PhotonView-based win resolution,
                // etc.) behaves exactly like a real multiplayer match. GameManager tells this match
                // apart from a real one via PhotonNetwork.OfflineMode/gameDataSO.opponentIsBot, never
                // via gameMode - see GameManager.InitializeGame/PlayGameOverSequence/ReceiveDrawOffer.
                gameDataSO.gameMode = GameModeType.Multiplayer;
            }
            else
            {
                connectionManager.CloseRoomAndLoadOnlineScene(GameConstants.Scenes.GameplayScene);
            }

            return;
        }

        int displaySeconds = Mathf.CeilToInt(remaining);
        if(displaySeconds != lastDisplayedCountdown)
        {
            lastDisplayedCountdown = displaySeconds;
            matchmakingPage.ShowPreGameCountdown(displaySeconds.ToString());
        }
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();

        if(isCancelled)
        {
            return;
        }

        matchmakingPage.ShowConnected();
    }

    // Random matchmaking has to wait until we're actually inside the rule-set SQL lobby -
    // joining a room before then would ignore the rule-set filter entirely.
    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();

        if(isCancelled)
        {
            return;
        }

        if(!connectionManager.JoinRandomRoom())
        {
            Debug.LogError("[OnlineModeHandler] JoinRandomRoom could not be processed");
            StopMatchmakingTimer();
            matchmakingPage.ShowFailed("Could not start matchmaking. Please try again.");
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        base.OnDisconnected(cause);

        if(isCancelled)
        {
            return;
        }

        ServiceLocator.Get<WarningNotifier>().Show($"Connection lost ({cause}). Please try again.");
        CancelMatch();
    }

    public override void OnCustomAuthenticationFailed(string debugMessage)
    {
        base.OnCustomAuthenticationFailed(debugMessage);

        if(isCancelled)
        {
            return;
        }

        StopMatchmakingTimer();
        matchmakingPage.ShowFailed("Could not authenticate. Please try again.");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        base.OnJoinRandomFailed(returnCode, message);

        if(isCancelled)
        {
            return;
        }

        connectionManager.CreateRoom();
    }

    public override void OnCreatedRoom()
    {
        base.OnCreatedRoom();
    }

    public override void OnCreateRoomFailed()
    {
        base.OnCreateRoomFailed();

        if(isCancelled)
        {
            return;
        }

        ServiceLocator.Get<WarningNotifier>().Show("Could not create a match. Please try again.");
        CancelMatch();
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        matchmakingPage.ShowJoinedRoom();
        StartMatchmakingTimer();

        if(!connectionManager.IsRoomFull)
        {
            matchmakingPage.ShowSearchingOpponent();
        }

        TryStartGameplay();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        TryStartGameplay();
    }

    private void TryStartGameplay()
    {
        Debug.Log($"[OnlineModeHandler] TryStartGameplay - isCancelled: {isCancelled}, isPreGameCountdownRunning: {isPreGameCountdownRunning}, IsRoomFull: {connectionManager.IsRoomFull}, time: {Time.realtimeSinceStartup}");

        if(isCancelled || isPreGameCountdownRunning || !connectionManager.IsRoomFull)
        {
            return;
        }

        StopMatchmakingTimer();

        gameDataSO.ownPlayer = connectionManager.GetOwnPlayerInfo();
        gameDataSO.opponentPlayer = connectionManager.GetOpponentPlayerInfo();

        matchmakingPage.ShowOpponentFound(gameDataSO.opponentPlayer.userName, gameDataSO.opponentPlayer.avatar);

        StartPreGameCountdown();
    }

    private void StartPreGameCountdown()
    {
        Debug.Log($"[OnlineModeHandler] StartPreGameCountdown - time: {Time.realtimeSinceStartup}");

        preGameCountdownElapsed = 0f;
        lastDisplayedCountdown = -1;
        isPreGameCountdownRunning = true;
    }

    private void OnMatchmakingTimeout()
    {
        if(isCancelled || connectionManager.IsRoomFull)
        {
            return;
        }

        isCancelled = true;
        isBotFallbackPending = true;

        ProfileManager profileManager = ServiceLocator.Get<ProfileManager>();
        int avtarIndex = Random.Range(1, profileManager.AvtarCount + 1);
        pendingDisguisedName = GameConstants.Profile.GuestNamePrefix + Random.Range(1000, 10000);
        pendingDisguisedAvatar = profileManager.GetAvtar(avtarIndex);

        matchmakingPage.ShowOpponentFound(pendingDisguisedName, pendingDisguisedAvatar);

        StartPreGameCountdown();
    }

    private void StartMatchmakingTimer()
    {
        matchmakingElapsed = 0f;
        lastDisplayedSeconds = -1;
        isTimerRunning = true;
    }

    private void StopMatchmakingTimer()
    {
        isTimerRunning = false;
    }

    private void StopPreGameCountdown()
    {
        isPreGameCountdownRunning = false;
    }

    private void ResetMatchState()
    {
        isCancelled = false;
        isBotFallbackPending = false;
        pendingDisguisedName = null;
        pendingDisguisedAvatar = null;

        // A previous match's bot fallback (see OnMatchmakingTimeout below) may have left this true -
        // a genuine, successfully-matched online match must not inherit it.
        gameDataSO.opponentIsBot = false;

        StopMatchmakingTimer();
        StopPreGameCountdown();
    }
}
