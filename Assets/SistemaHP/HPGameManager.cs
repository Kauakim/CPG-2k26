using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

/// Orquestrador principal do jogo Hot Potato.
/// Substitui core.cs — não cria nenhum elemento de UI em runtime.
/// Todas as referências são atribuídas via Inspector.
public class HPGameManager : MonoBehaviour
{
    // ── Configurações de jogo ─────────────────────────────────────────────────

    [Header("Configurações de jogo")]
    [SerializeField] private int   maxPlayers              = 8;
    [SerializeField] private int   minPlayers              = 2;
    [SerializeField] private float timerMax                = 30f;
    [SerializeField] private float timerPenalty            = 5f;
    [SerializeField] private float cardEarnThreshold      = 5f;
    [SerializeField] private int   lobbyCountdownSeconds   = 3;
    [SerializeField] private float lobbyWaitSeconds        = 60f;
    [SerializeField] private float botAnswerDelayMin       = 1.2f;
    [SerializeField] private float botAnswerDelayMax       = 2.4f;
    [SerializeField] private bool  autoAddLocalPlayer      = true;
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
    [SerializeField] private HPTimerSystem    timerSystem;
    [SerializeField] private HPMusicSystem    musicSystem;
    [SerializeField] private HPQuestionSystem questionSystem;

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
    private bool  botsAnswerAutomatically = true;

    private float lobbyWaitTimer;
    private float totalGameTime;

    private int botNameCounter = 1;

    private Coroutine turnRoutine;
    private Coroutine botRoutine;
    private Coroutine cardChoiceAutoRoutine;
    private Coroutine shakeRoutine;
    private Coroutine passRoutine;      // armazenado para poder ser cancelado no reset

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Start()
    {
        currentTimerMax = timerMax;

        if (suddenDeathText != null) suddenDeathText.gameObject.SetActive(false);
        if (lastAnswerText  != null) lastAnswerText.text = "";

        questionView?.SetGame(this);
        resultView?.SetRestartAction(ResetToLobby);

        if (lobbyView != null && lobbyView.AddBotButton != null)
            lobbyView.AddBotButton.onClick.AddListener(AddBot);

        SetupCardButtons();

        if (autoAddLocalPlayer) AddLocalPlayer();

        RefreshLobbyView();
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

        if (players.Count < minPlayers)
        {
            lobbyWaitTimer = 0f;
            RefreshLobbyView();
            return;
        }

        lobbyWaitTimer += Time.deltaTime;
        bool  roomFull  = players.Count >= maxPlayers;
        float remaining = Mathf.Max(0f, lobbyWaitSeconds - lobbyWaitTimer);

        if (roomFull || remaining <= lobbyCountdownSeconds)
        {
            StartGameCountdown();
        }
        else
        {
            // Espera normal: exibe tempo restante
            lobbyView?.ShowLobby(players.Count, maxPlayers,
                                 Mathf.CeilToInt(remaining),
                                 players.Count < maxPlayers);
        }
    }

    private void StartGameImmediate()
    {
        StartGameCountdown();
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

    public void AddLocalPlayer()
    {
        if (players.Count >= maxPlayers || phase != HPPhase.Lobby) return;
        AddPlayer(new HPPlayer { Name = "Jogador", IsLocal = true });
    }

    public void AddBot()
    {
        if (players.Count >= maxPlayers || phase != HPPhase.Lobby) return;
        AddPlayer(new HPPlayer { Name = "Bot " + botNameCounter++, IsLocal = false });
    }

    private void AddPlayer(HPPlayer player)
    {
        int index = players.Count;
        if (index < seatViews.Length && seatViews[index] != null)
            player.View = seatViews[index];
        players.Add(player);
        sceneBridge?.SetPlayerCount(players.Count);
        RefreshAllSeats(true);
    }

    // ── Lobby → Jogo ──────────────────────────────────────────────────────────

    private void StartGameCountdown()
    {
        if (phase != HPPhase.Lobby) return;
        phase = HPPhase.LobbyCountdown;
        StartCoroutine(CountdownRoutine());
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
        lobbyView?.HideAll();
        BeginGame();
    }

    private void BeginGame()
    {
        phase = HPPhase.Playing;
        botsAnswerAutomatically = true;
        totalGameTime = 0f;
        musicSystem?.StartMusic();
        currentPlayerIndex    = -1;
        forcedNextPlayerIndex = Random.Range(0, players.Count);
        questionView?.ShowBoard();
        RefreshAllSeats(false);
        UpdateCardButtons();
        StartNextTurn();
    }

    // ── Lógica de turno ───────────────────────────────────────────────────────

    private void StartNextTurn()
    {
        if (AliveCount() <= 1) { EndGame(); return; }
        StopBotRoutine();
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
        sceneBridge?.SetArrowTarget(currentPlayerIndex);      // seta giratória aponta para o jogador
        sceneBridge?.SetDerraunde(currentPlayerIndex);        // DERRAUNDE gira para o jogador atual
        RefreshActivePlayerView();

        // Pergunta
        turnCount++;
        CheckSuddenDeath();
        PrepareQuestion();

        // Incrementa o ID do turno antes de liberar resolvingTurn para que
        // qualquer BotRoutine de turno anterior seja descartada pelo guard.
        currentTurnId++;
        int capturedTurnId = currentTurnId;

        if (player.IsLocal)
            questionView?.Show(player.Name, currentQuestionText, true);
        else
            questionView?.ShowForBot(currentQuestionText);   // não expõe nome/input do bot

        timerSystem?.StartTimer(currentTimerMax);
        resolvingTurn = false;
        UpdateCardButtons();

        if (botsAnswerAutomatically && !player.IsLocal)
            ScheduleBotAnswer(0f, capturedTurnId);
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
        if (phase != HPPhase.Playing || resolvingTurn) return;
        if (currentPlayerIndex < 0 || currentPlayerIndex >= players.Count) return;

        HPPlayer player = players[currentPlayerIndex];

        double parsed;
        if (!TryParseAnswer(input, out parsed)) return;

        double tolerance = 0.01 + System.Math.Abs(currentAnswerValue) * 0.001;
        bool   correct   = System.Math.Abs(parsed - currentAnswerValue) <= tolerance;

        if (correct)
        {
            consecutiveWrong = 0;
            turnsWithoutLifeLoss++;
            lastAnswerCorrect = true;
            hasPreviousAnswer = true;
            questionView?.SetInteractable(false);
            StopBotRoutine();

            // Ganhou carta se respondeu em menos de cardEarnThreshold segundos
            bool earnedCard = timerSystem != null && timerSystem.Elapsed < cardEarnThreshold;

            // Pausa o timer durante a escolha de operação
            timerSystem?.StopTimer();

            UpdateLastAnswer(player.Name, input);

            if (currentExpression.HasValue && questionSystem != null)
                OfferOperationChoice(player, earnedCard);
            else
            {
                if (earnedCard) StartCardOfferForPlayer(player);
                PassTurnSoon(0.35f);
            }
        }
        else
        {
            consecutiveWrong++;
            turnsWithoutLifeLoss = 0;
            PlaySfx(vidroClip);               // som de erro imediato
            timerSystem?.Penalize(timerPenalty);

            // A cada 2 erros consecutivos reseta a expressão
            if (consecutiveWrong >= 2)
            {
                consecutiveWrong    = 0;
                resetExpressionNext = true;
                currentExpression   = new HPQuestionSystem.ExpressionState();
                PrepareQuestion();
                if (player.IsLocal)
                    questionView?.Show(player.Name, currentQuestionText, true);
                else
                    questionView?.ShowForBot(currentQuestionText);
            }
        }
    }

    // ── Vida perdida ──────────────────────────────────────────────────────────

    private void ApplyLifeLoss(HPPlayer player)
    {
        player.Lives = Mathf.Max(0, player.Lives - 1);
        if (player.Lives <= 0)
            player.Eliminated = true;
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator AfterLifeLossRoutine(HPPlayer player)
    {
        RefreshAllSeats(false);
        questionView?.SetInteractable(false);
        resetExpressionNext = true;
        yield return new WaitForSeconds(1.15f);
        FinishLifeLossFlow();
    }

    private void FinishLifeLossFlow()
    {
        if (AliveCount() <= 1)
            EndGame();
        else
            StartNextTurn();
    }

    // ── Escolha de carta ──────────────────────────────────────────────────────

    private void OfferCardChoice(HPPlayer player)
    {
        if (cardView == null || AliveCount() <= 1) { FinishLifeLossFlow(); return; }

        cardChoicePlayer = player;
        HPCardType optA = RandomCard();
        HPCardType optB = RandomCardDifferentFrom(optA);

        if (!player.IsLocal)
        {
            // Bot escolhe aleatoriamente
            HPCardType picked = Random.value < 0.5f ? optA : optB;
            GiveCard(player, picked);
            cardView.ShowFloatingCard(player.Name);
            FinishLifeLossFlow();
            return;
        }

        if (cardChoiceAutoRoutine != null) StopCoroutine(cardChoiceAutoRoutine);
        cardView.ShowChoice(player.Name, optA, optB, OnCardChosen);
        cardChoiceAutoRoutine = StartCoroutine(CardChoiceTimeout(cardChoiceTimeout, optA));
    }

    private void OnCardChosen(HPCardType cardType)
    {
        if (cardChoiceAutoRoutine != null) { StopCoroutine(cardChoiceAutoRoutine); cardChoiceAutoRoutine = null; }
        if (cardChoicePlayer != null)
        {
            GiveCard(cardChoicePlayer, cardType);
            cardView.ShowFloatingCard(cardChoicePlayer.Name);
            UpdateCardButtons();
        }
        cardChoicePlayer = null;
        FinishLifeLossFlow();
    }

    private IEnumerator CardChoiceTimeout(float timeout, HPCardType autoChoice)
    {
        yield return new WaitForSeconds(timeout);
        OnCardChosen(autoChoice);
    }

    private void GiveCard(HPPlayer player, HPCardType cardType)
    {
        if (player.HeldCard == HPCardType.None)
            player.HeldCard = cardType;
        else if (player.HeldCard2 == HPCardType.None)
            player.HeldCard2 = cardType;
        else
            return;

        int idx = players.IndexOf(player);
        if (idx >= 0 && player.View != null)
            player.View.SetPlayer(player, idx + 1);
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

    // ── Escolha de operação ───────────────────────────────────────────────────

    private void OfferOperationChoice(HPPlayer player, bool offerCardAfter = false)
    {
        if (opChoiceView == null || questionSystem == null)
        {
            if (offerCardAfter) StartCardOfferForPlayer(player);
            PassTurnSoon(0.35f);
            return;
        }

        string[] options = questionSystem.GenerateOperationOptions(turnCount, currentExpression);

        if (!player.IsLocal)
        {
            ApplyOperation(options[Random.Range(0, options.Length)]);
            if (offerCardAfter) StartCardOfferForPlayer(player);
            PassTurnSoon(0.35f);
            return;
        }

        opChoiceView.Show(options, opChoiceTimeout, op =>
        {
            ApplyOperation(op);
            if (offerCardAfter) StartCardOfferForPlayer(player);
            PassTurnSoon(0.35f);
        });
    }

    private void ApplyOperation(string op)
    {
        if (questionSystem == null) return;
        currentExpression   = questionSystem.ApplyChosenOperation(currentExpression, op);
        currentQuestionText = "Quanto é: " + currentExpression.Text + " = ?";
        currentAnswerValue  = currentExpression.Value;

        // Só atualiza o quadro em tempo real quando o jogador local escolheu a operação.
        // Para bots, o quadro é atualizado ao início do próximo turno via Show/ShowForBot.
        bool localIsChooser = currentPlayerIndex >= 0
                           && currentPlayerIndex < players.Count
                           && players[currentPlayerIndex].IsLocal;
        if (localIsChooser)
            questionView?.UpdateQuestion(currentQuestionText);
    }

    // ── Oferta de carta por resposta rápida (não bloqueia próximo turno) ──────

    private void StartCardOfferForPlayer(HPPlayer player)
    {
        if (cardView == null) return;

        HPCardType optA = RandomCard();
        HPCardType optB = RandomCardDifferentFrom(optA);

        if (!player.IsLocal)
        {
            GiveCard(player, Random.value < 0.5f ? optA : optB);
            cardView.ShowFloatingCard(player.Name);
            return;
        }

        // Mostra painel mas não bloqueia — o próximo turno já pode começar.
        cardChoicePlayer = player;
        cardView.ShowChoice(player.Name, optA, optB, OnCardChosenFast);
        if (cardChoiceAutoRoutine != null) StopCoroutine(cardChoiceAutoRoutine);
        cardChoiceAutoRoutine = StartCoroutine(CardChoiceTimeoutNoCard(cardChoiceTimeout));
        PassTurnSoon(0.35f);
    }

    private void OnCardChosenFast(HPCardType cardType)
    {
        if (cardChoiceAutoRoutine != null) { StopCoroutine(cardChoiceAutoRoutine); cardChoiceAutoRoutine = null; }
        if (cardChoicePlayer != null)
        {
            GiveCard(cardChoicePlayer, cardType);
            cardView.ShowFloatingCard(cardChoicePlayer.Name);
            UpdateCardButtons();
        }
        cardChoicePlayer = null;
        cardView?.HideChoice();
    }

    private IEnumerator CardChoiceTimeoutNoCard(float timeout)
    {
        yield return new WaitForSeconds(timeout);
        cardChoicePlayer = null;
        cardView?.HideChoice(); // tempo esgotou, não ganha carta
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
        // Cartas podem ser usadas a qualquer momento — busca o jogador local diretamente
        HPPlayer localPlayer = GetLocalPlayer();
        if (localPlayer == null || !localPlayer.Alive) return;
        HPCardType card = slot == 0 ? localPlayer.HeldCard : localPlayer.HeldCard2;
        if (card == HPCardType.None) return;
        UseCard(localPlayer, card, slot == 0);
    }

    private void UseCard(HPPlayer player, HPCardType cardType, bool isFirstSlot)
    {
        switch (cardType)
        {
            case HPCardType.NP3:      UseNP3(player, isFirstSlot);                   break;
            case HPCardType.Atestado: BeginAtestadoSelection(player, isFirstSlot);   break;
            case HPCardType.Monster:  UseMonster(player, isFirstSlot);               break;
            case HPCardType.GPT:      UseGPT(player, isFirstSlot);                   break;
        }
        UpdateCardButtons();
    }

    private void UseNP3(HPPlayer player, bool isFirstSlot)
    {
        if (player == null || player.Lives >= 3 || player.Lives <= 1) return;
        cardView?.PlayUseAnimation(HPCardType.NP3);
        PlaySfx(salivaClip);
        player.Lives = Mathf.Min(3, player.Lives + 1);
        ConsumeCard(player, isFirstSlot);
    }

    private void UseMonster(HPPlayer player, bool isFirstSlot)
    {
        if (timerSystem == null || !timerSystem.Running || timerSystem.Remaining <= 0f) return;
        cardView?.PlayUseAnimation(HPCardType.Monster);
        PlaySfx(monsterClip);
        musicSystem?.SetSpeed(0.75f);
        timerSystem.SetRemainingTime(timerSystem.Remaining * 2f);
        ConsumeCard(player, isFirstSlot);
    }

    private void UseGPT(HPPlayer player, bool isFirstSlot)
    {
        cardView?.PlayUseAnimation(HPCardType.GPT);
        PlaySfx(gepetoClip);
        ConsumeCard(player, isFirstSlot);
        // Exibe o valor correto da expressão atual (não o texto "GPT")
        UpdateLastAnswer(player.Name, HPQuestionSystem.FormatAnswer(currentAnswerValue));
        questionView?.SetInteractable(false);
        PassTurnSoon(0.35f);
    }

    private void BeginAtestadoSelection(HPPlayer player, bool isFirstSlot)
    {
        if (AliveCount() <= 1 || !player.IsLocal) return;
        selectingAtestadoTarget = true;
        StopBotRoutine();
        questionView?.SetInteractable(false);
        cardView?.PlayUseAnimation(HPCardType.Atestado);
        PlaySfx(tosseClip);
        SetTargetSelection(true);
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

        HPPlayer player = players[currentPlayerIndex];
        selectingAtestadoTarget = false;
        SetTargetSelection(false);
        ConsumeCard(player, true);
        forcedNextPlayerIndex = seatIndex;
        resolvingTurn = true;
        timerSystem?.StopTimer();
        UpdateCardButtons();
        StartCoroutine(PassTurnRoutine(0.35f));
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
        resolvingTurn = true;
        selectingAtestadoTarget = false;
        SetTargetSelection(false);
        UpdateCardButtons();
        StopBotRoutine();
        StartCoroutine(TimeoutRoutine());
    }

    private IEnumerator TimeoutRoutine()
    {
        HPPlayer player = players[currentPlayerIndex];
        timerSystem?.StopTimer();
        if (player.View != null) player.View.SetLamp(Color.clear, false);
        questionView?.SetInteractable(false);

        consecutiveWrong    = 0;
        resetExpressionNext = true;
        PlaySfx(vidroClip);               // som de vidro ao perder vida por timeout
        ApplyLifeLoss(player);
        RefreshAllSeats(false);           // atualiza sprite de vida imediatamente

        yield return new WaitForSeconds(1.15f);

        if (AliveCount() <= 1) EndGame();
        else StartNextTurn();
    }

    // ── Passagem de turno ─────────────────────────────────────────────────────

    private void PassTurnSoon(float delay)
    {
        if (resolvingTurn) return;
        resolvingTurn = true;
        selectingAtestadoTarget = false;
        SetTargetSelection(false);
        UpdateCardButtons();
        StopBotRoutine();
        if (passRoutine != null) { StopCoroutine(passRoutine); passRoutine = null; }
        passRoutine = StartCoroutine(PassTurnRoutine(delay));
    }

    private IEnumerator PassTurnRoutine(float delay)
    {
        timerSystem?.StopTimer();
        UpdateCardButtons();
        yield return new WaitForSeconds(delay);
        passRoutine = null;
        StartNextTurn();
    }

    // ── Fim de jogo / Reset ───────────────────────────────────────────────────

    private void EndGame()
    {
        phase = HPPhase.GameOver;
        StopAllGameCoroutines();
        selectingAtestadoTarget = false;
        SetTargetSelection(false);
        cardView?.HideChoice();
        timerSystem?.StopTimer();
        musicSystem?.StopMusic();
        sceneBridge?.SetActivePlayer(-1);
        RefreshAllSeats(false);
        if (useCardButton1 != null) useCardButton1.gameObject.SetActive(false);
        if (useCardButton2 != null) useCardButton2.gameObject.SetActive(false);

        HPPlayer winner     = GetWinner();
        string   winnerName = winner != null ? winner.Name : "Ninguém";
        resultView?.Show(winnerName);
    }

    public void ResetToLobby()
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
        botNameCounter          = 1;
        players.Clear();

        if (suddenDeathText != null) suddenDeathText.gameObject.SetActive(false);
        if (lastAnswerText  != null) lastAnswerText.text = "";

        timerSystem?.StopTimer();
        questionView?.Hide();
        cardView?.HideChoice();
        opChoiceView?.Hide();
        resultView?.Hide();
        musicSystem?.StopMusic();
        sceneBridge?.ResetAll();

        if (autoAddLocalPlayer) AddLocalPlayer();

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
        if (!show) { if (icon != null) icon.color = Color.clear; return; }

        // Botões visíveis para o jogador local em qualquer momento
        HPPlayer localPlayer = GetLocalPlayer();
        if (localPlayer == null || !localPlayer.Alive)
        {
            if (icon != null) icon.color = Color.clear;
            btn.interactable = false;
            return;
        }

        HPCardType cardType = slot == 0 ? localPlayer.HeldCard : localPlayer.HeldCard2;
        if (cardType == HPCardType.None)
        {
            if (icon != null) icon.color = Color.clear;
            btn.interactable = false;
            return;
        }

        Sprite spr = GetCardButtonSprite(cardType);
        if (icon != null) { icon.sprite = spr; icon.color = spr != null ? Color.white : Color.clear; }
        btn.interactable = !selectingAtestadoTarget && CanUseCard(localPlayer, cardType);
    }

    private bool CanUseCard(HPPlayer player, HPCardType cardType)
    {
        switch (cardType)
        {
            case HPCardType.NP3:
                return player.Lives < 3 && player.Lives > 1;
            case HPCardType.Atestado:
                return AliveCount() > 1 && !resolvingTurn;
            case HPCardType.Monster:
                // Monster só faz sentido quando há um timer rodando
                return timerSystem != null && timerSystem.Running && timerSystem.Remaining > 0f;
            case HPCardType.GPT:
                // GPT só funciona na vez do jogador local (ele responde a pergunta atual)
                return !resolvingTurn
                    && currentPlayerIndex >= 0
                    && currentPlayerIndex < players.Count
                    && players[currentPlayerIndex] == player;
            default:
                return false;
        }
    }

    private Sprite GetCardButtonSprite(HPCardType cardType)
    {
        return cardView != null ? cardView.GetIconSprite(cardType) : null;
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

    private void ScheduleBotAnswer(float extraDelay, int turnId)
    {
        StopBotRoutine();
        botRoutine = StartCoroutine(BotRoutine(extraDelay, turnId));
    }

    private IEnumerator BotRoutine(float extraDelay, int turnId)
    {
        float delay = Random.Range(botAnswerDelayMin, botAnswerDelayMax) + Mathf.Max(0f, extraDelay);

        // Nunca ultrapassa 60% do tempo restante para evitar timeout.
        if (timerSystem != null && timerSystem.Running)
            delay = Mathf.Min(delay, timerSystem.Remaining * 0.6f);

        // Floor elevado para garantir que resolvingTurn = false (liberado em 0.275 s)
        // já ocorreu antes de SubmitAnswer ser chamado, inclusive em morte súbita.
        delay = Mathf.Max(0.45f, delay);

        yield return new WaitForSeconds(delay);

        // Descarta a coroutine se o turno já avançou (guard principal contra múltiplas respostas).
        if (phase != HPPhase.Playing || currentPlayerIndex < 0) yield break;
        if (players[currentPlayerIndex].IsLocal) yield break;
        if (turnId != currentTurnId) yield break;

        // Não grava no input — o usuário não deve ver a resposta do bot.
        botRoutine = null;
        SubmitAnswer(HPQuestionSystem.FormatAnswer(currentAnswerValue));
    }

    private void StopBotRoutine()
    {
        if (botRoutine != null) { StopCoroutine(botRoutine); botRoutine = null; }
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

    /// Retorna o HPPlayer local, independentemente de quem está jogando no momento.
    private HPPlayer GetLocalPlayer()
    {
        for (int i = 0; i < players.Count; i++)
            if (players[i].IsLocal) return players[i];
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
        StopBotRoutine();
        if (cardChoiceAutoRoutine != null) { StopCoroutine(cardChoiceAutoRoutine); cardChoiceAutoRoutine = null; }
        if (turnRoutine           != null) { StopCoroutine(turnRoutine);           turnRoutine = null; }
        if (shakeRoutine          != null) { StopCoroutine(shakeRoutine);          shakeRoutine = null; }
        if (passRoutine           != null) { StopCoroutine(passRoutine);           passRoutine = null; }
    }
}