using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.UI;

/// Orquestrador principal do jogo Hot Potato — versão multiplayer Photon PUN2.
/// Master Client = autoridade do jogo. Toda mudança de estado é enviada via RPC a todos.
[RequireComponent(typeof(PhotonView))]
public class HPGameManager : MonoBehaviourPun
{
    // ── Configurações de jogo ─────────────────────────────────────────────────

    [Header("Configurações de jogo")]
    [SerializeField] private int   maxPlayers              = 8;
    [SerializeField] private int   minPlayers              = 2;
    [SerializeField] private float timerMax                = 30f;
    [SerializeField] private float timerPenalty            = 5f;
    [SerializeField] private float cardEarnThreshold      = 5f;
    [SerializeField] private int   lobbyCountdownSeconds   = 3;
    [SerializeField] private float lobbyWaitSeconds        = 3f;
    [SerializeField] private float opChoiceTimeout         = 5f;
    [SerializeField] private float cardChoiceTimeout       = 10f;
    [SerializeField] private int   suddenDeathAfterTurn    = 55;
    [SerializeField] private int   turnsForSuddenDeath     = 10;

    // ── Referências de cena ───────────────────────────────────────────────────

    [Header("Cena")]
    [SerializeField] private SceneBridge sceneBridge;

    // ── Seats (PlayerUI MonoBehaviour em cada Personagem) ─────────────────────

    [Header("Assentos — HPSeatView de cada Personagem (1-8)")]
    [SerializeField] private HPSeatView[] seatViews = new HPSeatView[8];

    // ── Sub-sistemas ──────────────────────────────────────────────────────────

    [Header("Sub-sistemas")]
    [SerializeField] private HPTimerSystem       timerSystem;
    [SerializeField] private HPMusicSystem       musicSystem;
    [SerializeField] private HPQuestionSystem    questionSystem;
    [SerializeField] private HPVignetteController vignetteController;

    // ── Views de UI ───────────────────────────────────────────────────────────

    [Header("Views de UI")]
    [SerializeField] private HPLobbyView    lobbyView;
    [SerializeField] private HPQuestionView questionView;
    [SerializeField] private HPResultView   resultView;
    [SerializeField] private HPCardView     cardView;
    [SerializeField] private HPOpChoiceView opChoiceView;

    // ── UI Avulsa ─────────────────────────────────────────────────────────────

    [Header("UI avulsa (atribuir via Inspector)")]
    [SerializeField] private Text          lastAnswerText;
    [SerializeField] private Text          totalGameTimerText;
    [SerializeField] private Text          suddenDeathText;
    [SerializeField] private RectTransform screenRoot;

    [Header("Botões de uso de carta (1 e 2)")]
    [SerializeField] private Button useCardButton1;
    [SerializeField] private Button useCardButton2;
    [SerializeField] private Image  useCardIcon1;
    [SerializeField] private Image  useCardIcon2;

    // ── Áudio ─────────────────────────────────────────────────────────────────

    [Header("Áudio — SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip   salivaClip;
    [SerializeField] private AudioClip   monsterClip;
    [SerializeField] private AudioClip   gepetoClip;
    [SerializeField] private AudioClip   tosseClip;
    [SerializeField] private AudioClip   vidroClip;     // toca em erro e ao perder vida

    // ── Estado interno ────────────────────────────────────────────────────────

    private HPPhase  phase = HPPhase.Lobby;
    private List<HPPlayer> players = new List<HPPlayer>();

    private int      currentPlayerIndex    = -1;
    private int      forcedNextPlayerIndex = -1;
    private HPPlayer cardChoicePlayer;

    private HPQuestionSystem.ExpressionState currentExpression;
    private string currentQuestionText = "";
    private double currentAnswerValue;

    private int  turnCount;
    private int  consecutiveWrong;
    private bool lastAnswerCorrect;
    private bool hasPreviousAnswer;
    private bool resetExpressionNext;
    private int  turnsWithoutLifeLoss;
    private bool suddenDeathActive;
    private float currentTimerMax;

    private int   currentTurnId;
    private bool  selectingAtestadoTarget;
    private bool  resolvingTurn;

    // ── Mapeamento Photon → HPPlayer ──────────────────────────────────────────
    // Chave: actor number do Photon. Valor: índice na lista players.
    private readonly Dictionary<int, int> actorToPlayerIndex = new Dictionary<int, int>();

    private float lobbyWaitTimer;
    private float totalGameTime;

    private Coroutine turnRoutine;
    private Coroutine cardChoiceAutoRoutine;
    private Coroutine shakeRoutine;
    private Coroutine passRoutine;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Start()
    {
        currentTimerMax = timerMax;

        if (suddenDeathText != null) suddenDeathText.gameObject.SetActive(false);
        if (lastAnswerText  != null) lastAnswerText.text = "";

        questionView?.SetGame(this);
        resultView?.SetRestartAction(ResetToLobby);
        SetupCardButtons();

        // Lobby permanece até o Photon confirmar entrada na sala (OnNetworkJoinedRoom)
        RefreshAllSeats(true);
    }

    private void Update()
    {
        HandleLobbyTimer();
        HandleGameTimer();
        HandleTotalGameTimer();
    }

    private void HandleTotalGameTimer()
    {
        if (phase != HPPhase.Playing) return;
        totalGameTime += Time.deltaTime;
        if (totalGameTimerText != null)
        {
            int s = Mathf.FloorToInt(totalGameTime);
            totalGameTimerText.text = string.Format("{0:00}:{1:00}", s / 60, s % 60);
        }
    }

    // ── Lobby timer ───────────────────────────────────────────────────────────

    private void HandleLobbyTimer()
    {
        if (phase != HPPhase.Lobby) return;
        // Apenas o Master Client controla o timer do lobby
        if (!PhotonNetwork.IsMasterClient) return;

        int seated = HPNetworkManager.Instance != null
            ? HPNetworkManager.Instance.OccupiedSeatCount()
            : players.Count;

        if (seated < minPlayers)
        {
            lobbyWaitTimer = 0f;
            photonView.RPC(nameof(RPC_SyncLobbyUI), RpcTarget.All,
                           seated, maxPlayers, 0, false);
            return;
        }

        // Sala cheia → 3s de countdown imediato
        if (seated >= maxPlayers)
        {
            photonView.RPC(nameof(RPC_StartCountdown), RpcTarget.All);
            return;
        }

        // 2–7 jogadores → 60s
        lobbyWaitTimer += Time.deltaTime;
        float remaining = Mathf.Max(0f, lobbyWaitSeconds - lobbyWaitTimer);

        if (remaining <= 0f)
        {
            photonView.RPC(nameof(RPC_StartCountdown), RpcTarget.All);
        }
        else if (remaining <= lobbyCountdownSeconds)
        {
            photonView.RPC(nameof(RPC_SyncLobbyCountdown), RpcTarget.All,
                           Mathf.CeilToInt(remaining));
        }
        else
        {
            photonView.RPC(nameof(RPC_SyncLobbyUI), RpcTarget.All,
                           seated, maxPlayers, Mathf.CeilToInt(remaining), true);
        }
    }

    private void RefreshLobbyView()
    {
        lobbyView?.ShowLobby(players.Count, maxPlayers, 0, players.Count < maxPlayers);
    }

    // ── Game timer ────────────────────────────────────────────────────────────

    private void HandleGameTimer()
    {
        if (phase != HPPhase.Playing || timerSystem == null) return;

        timerSystem.Tick(Time.deltaTime);

        if (timerSystem.Running && timerSystem.Remaining <= 0f)
        {
            timerSystem.StopTimer();
            ResolveTimeout();
        }

        if (currentPlayerIndex >= 0 && currentPlayerIndex < players.Count)
        {
            HPPlayer active = players[currentPlayerIndex];
            if (active.View != null)
                active.View.SetLamp(timerSystem.CurrentLightColor(), timerSystem.ShouldBlink);
        }
    }

    // ── Gerenciamento de jogadores ────────────────────────────────────────────

    // ── Callbacks do HPNetworkManager ─────────────────────────────────────────

    /// Chamado pelo HPNetworkManager quando o jogador entra na sala.
    public void OnNetworkJoinedRoom()
    {
        RefreshLobbyView();
        RefreshCharacterVisuals();
    }

    /// Atualiza a cor dos sprites dos personagens:
    /// cinza = assento livre, branco = assento ocupado.
    public void RefreshCharacterVisuals()
    {
        for (int i = 0; i < seatViews.Length; i++)
        {
            Transform slot = sceneBridge?.GetCharacterTransform(i);
            if (slot == null) continue;

            SpriteRenderer sr = slot.GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            // Não sobrescreve a cor vermelha de eliminado
            if (i < players.Count && players[i].Eliminated) continue;

            bool taken = HPNetworkManager.Instance != null
                      && HPNetworkManager.Instance.GetSeatOwner(i) != 0;

            sr.color = taken
                ? Color.white                            // escolhido → cor normal
                : new Color(0.4f, 0.4f, 0.4f, 1f);     // livre → cinza
        }
    }

    /// Chamado pelo Master quando a contagem de jogadores muda.
    public void OnNetworkPlayerCountChanged(int count)
    {
        RefreshLobbyView();
        RefreshCharacterVisuals();
    }

    /// Chamado quando este cliente se torna o novo Master (migração).
    public void OnBecameMaster()
    {
        Debug.Log("[HP] Este cliente se tornou o novo Master Client.");
        // Se o jogo estava rodando, o novo master assume o estado atual
    }

    // ── Construção da lista de jogadores a partir do Photon ───────────────────

    private void BuildPlayersFromPhoton()
    {
        players.Clear();
        actorToPlayerIndex.Clear();

        if (HPNetworkManager.Instance == null) return;

        for (int seat = 0; seat < maxPlayers; seat++)
        {
            Photon.Realtime.Player photonPlayer =
                HPNetworkManager.Instance.GetPlayerAtSeat(seat);
            if (photonPlayer == null) continue;

            var hp = new HPPlayer
            {
                Name    = photonPlayer.NickName,
                IsLocal = photonPlayer.IsLocal,
                Lives   = 3
            };
            if (seat < seatViews.Length && seatViews[seat] != null)
                hp.View = seatViews[seat];

            actorToPlayerIndex[photonPlayer.ActorNumber] = players.Count;
            players.Add(hp);
        }
    }

    // Mantidos para compatibilidade de Inspector (não fazem mais nada útil)
    public void AddLocalPlayer() { }
    public void AddBot()         { }

    // ── Lobby → Jogo ──────────────────────────────────────────────────────────

    private void StartGameCountdown()
    {
        if (phase != HPPhase.Lobby) return;
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC(nameof(RPC_StartCountdown), RpcTarget.All);
    }

    private IEnumerator CountdownRoutine()
    {
        for (int i = lobbyCountdownSeconds; i > 0; i--)
        {
            lobbyView?.ShowCountdown(i);
            yield return new WaitForSeconds(1f);
        }
        lobbyView?.ShowCountdown(0);
        yield return new WaitForSeconds(0.45f);

        if (PhotonNetwork.IsMasterClient)
            photonView.RPC(nameof(RPC_BeginGame), RpcTarget.All);
    }

    private void BeginGame()
    {
        phase         = HPPhase.Playing;
        totalGameTime = 0f;

        BuildPlayersFromPhoton();
        HPNetworkManager.Instance?.MarkGameStarted();

        lobbyView?.HideAll();
        musicSystem?.StartMusic();
        vignetteController?.StartGame();
        currentPlayerIndex    = -1;
        forcedNextPlayerIndex = Random.Range(0, players.Count);
        questionView?.ShowBoard();
        RefreshAllSeats(false);
        UpdateCardButtons();

        if (PhotonNetwork.IsMasterClient)
            StartNextTurn();
    }

    // ── Lógica de turno ───────────────────────────────────────────────────────

    private void StartNextTurn()
    {
        if (AliveCount() <= 1) { EndGame(); return; }
        if (turnRoutine != null) StopCoroutine(turnRoutine);
        turnRoutine = StartCoroutine(TurnRoutine());
    }

    private IEnumerator TurnRoutine()
    {
        resolvingTurn         = true;
        selectingAtestadoTarget = false;
        SetTargetSelection(false);
        UpdateCardButtons();
        questionView?.Hide();
        timerSystem?.StopTimer();
        musicSystem?.SetSpeed(1f);
        RefreshAllSeats(false);

        // Seleciona próximo jogador
        if (forcedNextPlayerIndex >= 0
            && forcedNextPlayerIndex < players.Count
            && players[forcedNextPlayerIndex].Alive)
        {
            currentPlayerIndex    = forcedNextPlayerIndex;
            forcedNextPlayerIndex = -1;
        }
        else
        {
            forcedNextPlayerIndex = -1;
            currentPlayerIndex    = GetNextAliveIndex(currentPlayerIndex);
        }

        if (currentPlayerIndex < 0) { EndGame(); yield break; }

        HPPlayer player = players[currentPlayerIndex];

        // Acende luz no jogador ativo
        yield return new WaitForSeconds(0.275f);
        sceneBridge?.SetActivePlayer(currentPlayerIndex);
        RefreshActivePlayerView();

        // Pergunta
        turnCount++;
        CheckSuddenDeath();
        PrepareQuestion();

        // Incrementa o ID do turno antes de liberar resolvingTurn para que
        // qualquer BotRoutine de turno anterior seja descartada pelo guard.
        currentTurnId++;

        // Master envia o turno para todos via RPC
        // (answerValue como string pois Photon não serializa double nativamente)
        photonView.RPC(nameof(RPC_BeginTurn), RpcTarget.All,
            currentPlayerIndex,
            currentQuestionText,
            HPQuestionSystem.FormatAnswer(currentAnswerValue),
            currentExpression.Text ?? "",
            currentExpression.HasValue);

        timerSystem?.StartTimer(currentTimerMax);
        resolvingTurn = false;
        UpdateCardButtons();
    }

    private void PrepareQuestion()
    {
        if (questionSystem == null)
        {
            currentQuestionText = "HPQuestionSystem não atribuído no Inspector.";
            currentAnswerValue  = 0;
            return;
        }
        var result = questionSystem.PrepareQuestion(
            turnCount, currentExpression, consecutiveWrong, resetExpressionNext);
        currentQuestionText  = result.questionText;
        currentAnswerValue   = result.answerValue;
        currentExpression    = result.newExpression;
        resetExpressionNext  = false;
        lastAnswerCorrect    = false;
        hasPreviousAnswer    = false;
    }

    // ── Submissão de resposta ─────────────────────────────────────────────────

    public void SubmitAnswer(string input)
    {
        if (phase != HPPhase.Playing) return;

        // Jogador local envia resposta ao Master para validação
        photonView.RPC(nameof(RPC_RequestAnswer), RpcTarget.MasterClient, input);
    }

    [PunRPC]
    private void RPC_RequestAnswer(string input, PhotonMessageInfo info)
    {
        // Só o Master valida
        if (!PhotonNetwork.IsMasterClient) return;
        if (resolvingTurn) return;
        if (currentPlayerIndex < 0 || currentPlayerIndex >= players.Count) return;

        // Verifica se quem enviou é realmente o jogador da vez
        if (actorToPlayerIndex.TryGetValue(info.Sender.ActorNumber, out int senderIdx))
            if (senderIdx != currentPlayerIndex) return;

        HPPlayer player = players[currentPlayerIndex];

        double parsed;
        if (!TryParseAnswer(input, out parsed)) return;

        double tolerance = 0.01 + System.Math.Abs(currentAnswerValue) * 0.001;
        bool   correct   = System.Math.Abs(parsed - currentAnswerValue) <= tolerance;

        if (correct)
        {
            consecutiveWrong = 0;
            turnsWithoutLifeLoss++;
            bool earnedCard = timerSystem != null && timerSystem.Elapsed < cardEarnThreshold;
            timerSystem?.StopTimer();

            // Broadcast resultado correto para todos
            photonView.RPC(nameof(RPC_OnAnswerCorrect), RpcTarget.All,
                           currentPlayerIndex, input, earnedCard);

            if (currentExpression.HasValue && questionSystem != null)
            {
                string[] ops = questionSystem.GenerateOperationOptions(turnCount, currentExpression);
                // Envia opções de operação para o jogador que acertou
                photonView.RPC(nameof(RPC_ShowOperationChoice), RpcTarget.All,
                               currentPlayerIndex, ops, earnedCard);
            }
            else
            {
                if (earnedCard) MasterGiveCard(player, currentPlayerIndex);
                photonView.RPC(nameof(RPC_PassTurn), RpcTarget.All, 0.35f);
            }
        }
        else
        {
            consecutiveWrong++;
            turnsWithoutLifeLoss = 0;
            if (consecutiveWrong >= 2)
            {
                consecutiveWrong    = 0;
                resetExpressionNext = true;
            }
            photonView.RPC(nameof(RPC_OnAnswerWrong), RpcTarget.All,
                           currentPlayerIndex, timerPenalty);
        }
    }

    // ── Vida perdida ──────────────────────────────────────────────────────────

    // ApplyLifeLoss local — apenas para uso interno no RPC_ApplyLifeLoss
    private void ApplyLifeLoss(HPPlayer player)
    {
        player.Lives = Mathf.Max(0, player.Lives - 1);
        if (player.Lives <= 0) player.Eliminated = true;
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private static HPCardType RandomCard()
    {
        HPCardType[] cards = { HPCardType.NP3, HPCardType.Atestado, HPCardType.Monster, HPCardType.GPT };
        return cards[Random.Range(0, cards.Length)];
    }

    private static HPCardType RandomCardDifferentFrom(HPCardType other)
    {
        HPCardType pick = RandomCard();
        for (int i = 0; i < 8 && pick == other; i++) pick = RandomCard();
        return pick;
    }

    private IEnumerator CardChoiceTimeoutNoCard(float timeout)
    {
        yield return new WaitForSeconds(timeout);
        cardChoicePlayer = null;
        cardView?.HideChoice();
        cardChoiceAutoRoutine = null;
    }

    // ── Uso de cartas ─────────────────────────────────────────────────────────

    private void SetupCardButtons()
    {
        if (useCardButton1 != null)
        {
            useCardButton1.onClick.RemoveAllListeners();
            useCardButton1.onClick.AddListener(() => UseCardFromSlot(0));
        }
        if (useCardButton2 != null)
        {
            useCardButton2.onClick.RemoveAllListeners();
            useCardButton2.onClick.AddListener(() => UseCardFromSlot(1));
        }
    }

    private void UseCardFromSlot(int slot)
    {
        if (phase != HPPhase.Playing) return;
        // Jogador local solicita uso de carta ao master
        photonView.RPC(nameof(RPC_RequestUseCard), RpcTarget.MasterClient, slot);
    }

    [PunRPC]
    private void RPC_RequestUseCard(int slot, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (currentPlayerIndex < 0) return;

        // Apenas o jogador da vez pode usar carta agora
        if (!actorToPlayerIndex.TryGetValue(info.Sender.ActorNumber, out int senderIdx)) return;
        if (senderIdx != currentPlayerIndex) return;

        HPPlayer player = players[currentPlayerIndex];
        HPCardType card = slot == 0 ? player.HeldCard : player.HeldCard2;
        if (card == HPCardType.None) return;

        // Broadcast efeito da carta para todos
        photonView.RPC(nameof(RPC_ApplyCardEffect), RpcTarget.All,
                       currentPlayerIndex, (int)card, slot);
    }

    private void ConsumeCard(HPPlayer player, bool isFirstSlot)
    {
        if (isFirstSlot) player.HeldCard  = HPCardType.None;
        else             player.HeldCard2 = HPCardType.None;

        int idx = players.IndexOf(player);
        if (idx >= 0 && player.View != null)
        {
            player.View.SetPlayer(player, idx + 1);
            player.View.SetActive(idx == currentPlayerIndex && player.Alive);
        }
    }

    // ── Seleção de alvo (Atestado) ────────────────────────────────────────────

    public void OnSeatClicked(int seatIndex)
    {
        if (!selectingAtestadoTarget) return;
        if (seatIndex < 0 || seatIndex >= players.Count) return;
        if (seatIndex == currentPlayerIndex || !players[seatIndex].Alive) return;

        selectingAtestadoTarget = false;
        SetTargetSelection(false);
        photonView.RPC(nameof(RPC_ForceNextPlayer), RpcTarget.All, seatIndex, 0.35f);
    }

    private void SetTargetSelection(bool enabled)
    {
        for (int i = 0; i < players.Count; i++)
        {
            bool validTarget = enabled && i != currentPlayerIndex && players[i].Alive;
            if (players[i].View != null) players[i].View.SetTargetHint(validTarget);
        }
    }

    // ── Morte súbita ──────────────────────────────────────────────────────────

    private void CheckSuddenDeath()
    {
        if (turnCount < suddenDeathAfterTurn) return;

        if (!suddenDeathActive && turnsWithoutLifeLoss >= turnsForSuddenDeath)
        {
            suddenDeathActive = true;
            if (suddenDeathText != null) suddenDeathText.gameObject.SetActive(true);
        }

        if (suddenDeathActive)
            currentTimerMax = Mathf.Max(1f, currentTimerMax - 1f);
    }

    // ── Timeout de turno ──────────────────────────────────────────────────────

    private void ResolveTimeout()
    {
        if (resolvingTurn || currentPlayerIndex < 0) return;
        if (!PhotonNetwork.IsMasterClient) return;
        resolvingTurn = true;
        photonView.RPC(nameof(RPC_ApplyLifeLoss), RpcTarget.All, currentPlayerIndex);
        StartCoroutine(AfterTimeoutRoutine());
    }

    private IEnumerator AfterTimeoutRoutine()
    {
        yield return new WaitForSeconds(1.15f);
        if (AliveCount() <= 1)
            EndGame();
        else
            StartNextTurn();
    }

    // ── Passagem de turno ─────────────────────────────────────────────────────

    private void PassTurnSoon(float delay)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC(nameof(RPC_PassTurn), RpcTarget.All, delay);
    }

    private IEnumerator PassTurnRoutine(float delay)
    {
        timerSystem?.StopTimer();
        UpdateCardButtons();
        yield return new WaitForSeconds(delay);
        StartNextTurn();
    }

    // ── Fim de jogo / Reset ───────────────────────────────────────────────────

    private void EndGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            HPPlayer winner = GetWinner();
            int winnerIdx   = winner != null ? players.IndexOf(winner) : -1;
            photonView.RPC(nameof(RPC_EndGame), RpcTarget.All, winnerIdx);
        }
    }

    private void ApplyEndGame(int winnerIndex)
    {
        phase = HPPhase.GameOver;
        StopAllGameCoroutines();
        selectingAtestadoTarget = false;
        SetTargetSelection(false);
        cardView?.HideChoice();
        timerSystem?.StopTimer();
        musicSystem?.StopMusic();
        vignetteController?.ResetVignette();
        sceneBridge?.SetActivePlayer(-1);
        RefreshAllSeats(false);
        if (useCardButton1 != null) useCardButton1.gameObject.SetActive(false);
        if (useCardButton2 != null) useCardButton2.gameObject.SetActive(false);

        string winnerName = (winnerIndex >= 0 && winnerIndex < players.Count)
            ? players[winnerIndex].Name : "Ninguém";
        resultView?.Show(winnerName);
    }

    public void ResetToLobby()
    {
        if (PhotonNetwork.IsMasterClient)
            photonView.RPC(nameof(RPC_ResetToLobby), RpcTarget.All);
    }

    private void ApplyResetToLobby()
    {
        StopAllGameCoroutines();
        phase                   = HPPhase.Lobby;
        resolvingTurn           = false;
        selectingAtestadoTarget = false;
        currentPlayerIndex      = -1;
        forcedNextPlayerIndex   = -1;
        cardChoicePlayer        = null;
        currentExpression       = new HPQuestionSystem.ExpressionState();
        turnCount               = 0;
        consecutiveWrong        = 0;
        currentTurnId           = 0;
        lastAnswerCorrect       = false;
        hasPreviousAnswer       = false;
        resetExpressionNext     = false;
        currentQuestionText     = "";
        currentAnswerValue      = 0;
        turnsWithoutLifeLoss    = 0;
        suddenDeathActive       = false;
        currentTimerMax         = timerMax;
        lobbyWaitTimer          = 0f;
        totalGameTime           = 0f;
        players.Clear();
        actorToPlayerIndex.Clear();

        if (suddenDeathText != null) suddenDeathText.gameObject.SetActive(false);
        if (lastAnswerText  != null) lastAnswerText.text = "";

        timerSystem?.StopTimer();
        questionView?.Hide();
        cardView?.HideChoice();
        opChoiceView?.Hide();
        resultView?.Hide();
        musicSystem?.StopMusic();
        vignetteController?.ResetVignette();
        sceneBridge?.ResetAll();

        for (int i = 0; i < 8; i++)
            sceneBridge?.SetPlayerEliminated(i, false);

        RefreshLobbyView();
        RefreshAllSeats(true);
        UpdateCardButtons();
    }

    // ── Atualização de views ──────────────────────────────────────────────────

    private void RefreshAllSeats(bool lobbyLayout)
    {
        bool sceneMode = sceneBridge != null;
        if (sceneMode)
            sceneBridge.SetPlayerCount(players.Count);

        for (int i = 0; i < seatViews.Length; i++)
        {
            HPSeatView view = seatViews[i];
            if (view == null) continue;

            HPPlayer player   = i < players.Count ? players[i] : null;
            bool     hasPlayer = player != null;

            view.gameObject.SetActive(lobbyLayout ? true : hasPlayer);
            if (!hasPlayer) continue;

            bool isActive = !lobbyLayout && i == currentPlayerIndex && player.Alive;
            view.SetPlayer(player, i + 1);
            view.SetActive(isActive);
            view.SetBadge(player.Eliminated, "DP");
        }
    }

    private void RefreshActivePlayerView()
    {
        for (int i = 0; i < players.Count; i++)
        {
            HPPlayer player = players[i];
            if (player.View == null) continue;
            player.View.SetPlayer(player, i + 1);
            player.View.SetActive(i == currentPlayerIndex && player.Alive);
            player.View.SetLamp(Color.clear, false);
            player.View.SetBadge(player.Eliminated, "DP");
        }
        UpdateCardButtons();
    }

    private void UpdateCardButtons()
    {
        UpdateCardButton(useCardButton1, useCardIcon1, 0);
        UpdateCardButton(useCardButton2, useCardIcon2, 1);
    }

    private void UpdateCardButton(Button btn, Image icon, int slot)
    {
        if (btn == null) return;

        bool show = phase == HPPhase.Playing;
        btn.gameObject.SetActive(show);
        if (!show || currentPlayerIndex < 0 || currentPlayerIndex >= players.Count)
        {
            if (icon != null) icon.color = Color.clear;
            return;
        }

        HPPlayer player = players[currentPlayerIndex];
        if (!player.IsLocal) { btn.gameObject.SetActive(false); return; }

        HPCardType cardType = slot == 0 ? player.HeldCard : player.HeldCard2;
        if (cardType == HPCardType.None)
        {
            if (icon != null) icon.color = Color.clear;
            btn.interactable = false;
            return;
        }

        Sprite spr = GetCardButtonSprite(cardType);
        if (icon != null) { icon.sprite = spr; icon.color = spr != null ? Color.white : Color.clear; }
        btn.interactable = !resolvingTurn && !selectingAtestadoTarget && CanUseCard(player, cardType);
    }

    private bool CanUseCard(HPPlayer player, HPCardType cardType)
    {
        switch (cardType)
        {
            case HPCardType.NP3:      return player.Lives < 3 && player.Lives > 1;
            case HPCardType.Atestado: return AliveCount() > 1;
            case HPCardType.Monster:  return timerSystem != null && timerSystem.Running && timerSystem.Remaining > 0f;
            case HPCardType.GPT:      return true;
            default:                  return false;
        }
    }

    private Sprite GetCardButtonSprite(HPCardType cardType)
    {
        // Os ícones de cartas vêm do HPCardView — reutiliza se disponível
        // Como alternativa, pode-se adicionar [SerializeField] próprio aqui.
        return null;
    }

    // ── UI de última resposta ─────────────────────────────────────────────────

    private void UpdateLastAnswer(string playerName, string answer)
    {
        if (lastAnswerText == null) return;
        // Exibe só o valor correto — nunca as tentativas erradas.
        lastAnswerText.text  = "✓  " + answer;
        lastAnswerText.color = HPCardInfo.Hex("#27e36f");
    }

    // ── Bot ───────────────────────────────────────────────────────────────────

    private void StopBotRoutine() { /* removido — sem bots */ }

    // ── RPCs Photon ───────────────────────────────────────────────────────────

    [PunRPC]
    private void RPC_SyncLobbyUI(int seated, int max, int remaining, bool canStart)
    {
        if (canStart)
            lobbyView?.ShowLobby(seated, max, remaining, seated < max);
        else
            lobbyView?.ShowLobby(seated, max, 0, false);
    }

    [PunRPC]
    private void RPC_SyncLobbyCountdown(int seconds)
    {
        lobbyView?.ShowCountdown(seconds);
    }

    [PunRPC]
    private void RPC_StartCountdown()
    {
        if (phase != HPPhase.Lobby) return;
        phase = HPPhase.LobbyCountdown;
        StartCoroutine(CountdownRoutine());
    }

    [PunRPC]
    private void RPC_BeginGame()
    {
        BeginGame();
    }

    [PunRPC]
    private void RPC_BeginTurn(int playerIndex, string questionText,
                                string answerStr, string exprText, bool exprHasValue)
    {
        currentPlayerIndex  = playerIndex;
        currentQuestionText = questionText;
        double.TryParse(answerStr, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out currentAnswerValue);
        currentExpression = new HPQuestionSystem.ExpressionState
        {
            Text     = exprText,
            Value    = currentAnswerValue,
            HasValue = exprHasValue
        };

        if (playerIndex < 0 || playerIndex >= players.Count) return;
        HPPlayer player = players[playerIndex];

        sceneBridge?.SetActivePlayer(playerIndex);
        sceneBridge?.SetArrowTarget(playerIndex);
        sceneBridge?.SetDerraunde(playerIndex);
        RefreshActivePlayerView();

        if (player.IsLocal)
            questionView?.Show(player.Name, questionText, true);
        else
            questionView?.ShowForBot(questionText);

        timerSystem?.StartTimer(currentTimerMax);
        resolvingTurn = false;
        UpdateCardButtons();
    }

    [PunRPC]
    private void RPC_OnAnswerCorrect(int playerIndex, string answer, bool earnedCard)
    {
        if (playerIndex >= 0 && playerIndex < players.Count)
            UpdateLastAnswer(players[playerIndex].Name, answer);
        questionView?.SetInteractable(false);
        PlaySfx(null); // som de acerto opcional
    }

    [PunRPC]
    private void RPC_OnAnswerWrong(int playerIndex, float penalty)
    {
        PlaySfx(vidroClip);
        timerSystem?.Penalize(penalty);
        if (playerIndex >= 0 && playerIndex < players.Count
            && players[playerIndex].IsLocal)
            questionView?.SetInteractable(true);
    }

    [PunRPC]
    private void RPC_ShowOperationChoice(int playerIndex, string[] ops, bool earnedCard)
    {
        if (playerIndex < 0 || playerIndex >= players.Count) return;
        HPPlayer player = players[playerIndex];

        if (player.IsLocal)
        {
            // Só o jogador da vez vê o painel de operação
            opChoiceView?.Show(ops, opChoiceTimeout, op =>
            {
                photonView.RPC(nameof(RPC_RequestOperation), RpcTarget.MasterClient,
                               op, earnedCard);
            });
        }
    }

    [PunRPC]
    private void RPC_RequestOperation(string op, bool earnedCard, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (questionSystem == null) return;

        currentExpression   = questionSystem.ApplyChosenOperation(currentExpression, op);
        currentQuestionText = "Quanto é: " + currentExpression.Text + " = ?";
        currentAnswerValue  = currentExpression.Value;

        photonView.RPC(nameof(RPC_ApplyOperation), RpcTarget.All,
                       currentQuestionText, currentExpression.Text,
                       HPQuestionSystem.FormatAnswer(currentAnswerValue));

        if (earnedCard)
        {
            HPPlayer player = players[currentPlayerIndex];
            MasterGiveCard(player, currentPlayerIndex);
        }
        photonView.RPC(nameof(RPC_PassTurn), RpcTarget.All, 0.35f);
    }

    [PunRPC]
    private void RPC_ApplyOperation(string questionText, string exprText, string answerStr)
    {
        currentQuestionText = questionText;
        currentExpression.Text = exprText;
        double.TryParse(answerStr, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out currentAnswerValue);
        currentExpression.Value    = currentAnswerValue;
        currentExpression.HasValue = true;
        questionView?.UpdateQuestion(questionText);
        opChoiceView?.Hide();
    }

    [PunRPC]
    private void RPC_GiveCard(int playerIndex, int slot, int cardType)
    {
        if (playerIndex < 0 || playerIndex >= players.Count) return;
        HPPlayer player = players[playerIndex];
        HPCardType type = (HPCardType)cardType;

        if (slot == 0) player.HeldCard  = type;
        else           player.HeldCard2 = type;

        player.View?.SetPlayer(player, playerIndex + 1);
        cardView?.ShowFloatingCard(player.Name);
        UpdateCardButtons();
    }

    [PunRPC]
    private void RPC_ApplyCardEffect(int playerIndex, int cardTypeInt, int slot)
    {
        if (playerIndex < 0 || playerIndex >= players.Count) return;
        HPPlayer   player   = players[playerIndex];
        HPCardType cardType = (HPCardType)cardTypeInt;
        bool       isFirst  = slot == 0;

        switch (cardType)
        {
            case HPCardType.NP3:
                cardView?.PlayUseAnimation(HPCardType.NP3);
                PlaySfx(salivaClip);
                player.Lives = Mathf.Min(3, player.Lives + 1);
                ConsumeCard(player, isFirst);
                break;

            case HPCardType.Monster:
                cardView?.PlayUseAnimation(HPCardType.Monster);
                PlaySfx(monsterClip);
                musicSystem?.SetSpeed(0.75f);
                timerSystem?.SetRemainingTime(
                    timerSystem != null ? timerSystem.Remaining * 2f : timerMax);
                ConsumeCard(player, isFirst);
                break;

            case HPCardType.GPT:
                cardView?.PlayUseAnimation(HPCardType.GPT);
                PlaySfx(gepetoClip);
                ConsumeCard(player, isFirst);
                UpdateLastAnswer(player.Name, HPQuestionSystem.FormatAnswer(currentAnswerValue));
                questionView?.SetInteractable(false);
                if (PhotonNetwork.IsMasterClient)
                    photonView.RPC(nameof(RPC_PassTurn), RpcTarget.All, 0.35f);
                break;

            case HPCardType.Atestado:
                cardView?.PlayUseAnimation(HPCardType.Atestado);
                PlaySfx(tosseClip);
                if (player.IsLocal)
                {
                    selectingAtestadoTarget = true;
                    SetTargetSelection(true);
                }
                ConsumeCard(player, isFirst);
                break;
        }
        UpdateCardButtons();
    }

    [PunRPC]
    private void RPC_ApplyLifeLoss(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= players.Count) return;
        HPPlayer player = players[playerIndex];
        player.Lives = Mathf.Max(0, player.Lives - 1);
        if (player.Lives <= 0)
        {
            player.Eliminated = true;
            sceneBridge?.SetPlayerEliminated(playerIndex, true);
        }
        PlaySfx(vidroClip);
        RefreshAllSeats(false);
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    [PunRPC]
    private void RPC_PassTurn(float delay)
    {
        resolvingTurn = true;
        selectingAtestadoTarget = false;
        SetTargetSelection(false);
        UpdateCardButtons();
        if (passRoutine != null) StopCoroutine(passRoutine);
        passRoutine = StartCoroutine(PassTurnRoutine(delay));
    }

    [PunRPC]
    private void RPC_ForceNextPlayer(int playerIndex, float delay)
    {
        forcedNextPlayerIndex = playerIndex;
        resolvingTurn = true;
        selectingAtestadoTarget = false;
        SetTargetSelection(false);
        UpdateCardButtons();
        if (passRoutine != null) StopCoroutine(passRoutine);
        passRoutine = StartCoroutine(PassTurnRoutine(delay));
    }

    [PunRPC]
    private void RPC_EndGame(int winnerIndex)
    {
        ApplyEndGame(winnerIndex);
    }

    [PunRPC]
    private void RPC_ResetToLobby()
    {
        ApplyResetToLobby();
    }

    // ── Helper master — dá carta a um jogador e faz broadcast ─────────────────

    private void MasterGiveCard(HPPlayer player, int playerIndex)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        HPCardType optA = RandomCard();
        HPCardType optB = RandomCardDifferentFrom(optA);

        int slot = player.HeldCard == HPCardType.None ? 0 : 1;

        if (!player.IsLocal)
        {
            // Bot/jogador remoto: escolha aleatória imediata
            HPCardType picked = Random.value < 0.5f ? optA : optB;
            photonView.RPC(nameof(RPC_GiveCard), RpcTarget.All,
                           playerIndex, slot, (int)picked);
            return;
        }

        // Para o jogador local: mostra painel de escolha via RPC para ele
        photonView.RPC(nameof(RPC_ShowCardChoice), RpcTarget.All,
                       playerIndex, (int)optA, (int)optB, slot);
    }

    [PunRPC]
    private void RPC_ShowCardChoice(int playerIndex, int cardA, int cardB, int slot)
    {
        if (playerIndex < 0 || playerIndex >= players.Count) return;
        HPPlayer player = players[playerIndex];
        if (!player.IsLocal) return;

        cardChoicePlayer = player;
        cardView?.ShowChoice(player.Name, (HPCardType)cardA, (HPCardType)cardB, chosen =>
        {
            photonView.RPC(nameof(RPC_GiveCard), RpcTarget.MasterClient,
                           playerIndex, slot, (int)chosen);
            photonView.RPC(nameof(RPC_GiveCard), RpcTarget.All,
                           playerIndex, slot, (int)chosen);
            cardView?.HideChoice();
        });

        if (cardChoiceAutoRoutine != null) StopCoroutine(cardChoiceAutoRoutine);
        cardChoiceAutoRoutine = StartCoroutine(CardChoiceTimeoutNoCard(cardChoiceTimeout));
    }

    // ── Animação de shake ─────────────────────────────────────────────────────

    private IEnumerator ShakeRoutine()
    {
        if (screenRoot == null) yield break;
        Vector2 origin    = screenRoot.anchoredPosition;
        float   duration  = 0.35f;
        float   magnitude = 12f;
        float   elapsed   = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - elapsed / duration;
            screenRoot.anchoredPosition = origin + new Vector2(
                (Random.value * 2f - 1f) * magnitude * t,
                (Random.value * 2f - 1f) * magnitude * t);
            yield return null;
        }
        screenRoot.anchoredPosition = origin;
        shakeRoutine = null;
    }

    // ── Utilitários ───────────────────────────────────────────────────────────

    private int AliveCount()
    {
        int c = 0;
        for (int i = 0; i < players.Count; i++)
            if (players[i].Alive) c++;
        return c;
    }

    private HPPlayer GetWinner()
    {
        for (int i = 0; i < players.Count; i++)
            if (players[i].Alive) return players[i];
        return null;
    }

    private int GetNextAliveIndex(int from)
    {
        if (players.Count == 0) return -1;
        for (int step = 1; step <= players.Count; step++)
        {
            int idx = (from + step + players.Count) % players.Count;
            if (players[idx].Alive) return idx;
        }
        return -1;
    }

    private static bool TryParseAnswer(string input, out double result)
    {
        input = input.Trim().Replace(',', '.');
        return double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);
    }

    private void StopAllGameCoroutines()
    {
        if (cardChoiceAutoRoutine != null) { StopCoroutine(cardChoiceAutoRoutine); cardChoiceAutoRoutine = null; }
        if (turnRoutine           != null) { StopCoroutine(turnRoutine);           turnRoutine = null; }
        if (shakeRoutine          != null) { StopCoroutine(shakeRoutine);          shakeRoutine = null; }
        if (passRoutine           != null) { StopCoroutine(passRoutine);           passRoutine = null; }
    }
}