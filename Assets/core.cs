using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public enum HotPotatoPhase
{
    Lobby,
    LobbyCountdown,
    Playing,
    GameOver
}

public enum HotPotatoCardType
{
    None,
    NP3,
    Atestado,
    Monster,
    GPT
}

public static class HotPotatoCardInfo
{
    public static string DisplayName(HotPotatoCardType cardType)
    {
        switch (cardType)
        {
            case HotPotatoCardType.NP3:
                return "NP3";
            case HotPotatoCardType.Atestado:
                return "ATESTADO";
            case HotPotatoCardType.Monster:
                return "MONSTER";
            case HotPotatoCardType.GPT:
                return "GPT";
            default:
                return "";
        }
    }

    public static string Description(HotPotatoCardType cardType)
    {
        switch (cardType)
        {
            case HotPotatoCardType.NP3:
                return "+1 vida se estiver com 2 vidas.";
            case HotPotatoCardType.Atestado:
                return "Redireciona a seta para outro jogador.";
            case HotPotatoCardType.Monster:
                return "Dobra o tempo restante da pergunta.";
            case HotPotatoCardType.GPT:
                return "Resolve a questao e passa a vez.";
            default:
                return "";
        }
    }
}

public class HotPotatoPlayer
{
    public string Name;
    public int Lives = 3;
    public HotPotatoCardType HeldCard = HotPotatoCardType.None;
    public HotPotatoCardType HeldCard2 = HotPotatoCardType.None;
    public bool Eliminated;
    public bool IsLocal;
    public HotPotatoSeatView View;

    public bool Alive
    {
        get { return !Eliminated && Lives > 0; }
    }
}

public class HotPotatoSeatView
{
    public RectTransform Root;
    public CanvasGroup Group;
    public Image SlotBackground;
    public Image Avatar;
    public Image Lamp;
    public Image LightOverlay;
    public Button SeatButton;
    public Text NameText;
    public Text LivesText;
    public Image LivesImage;
    public Text CardText;
    public Image CardImage;
    public Text BadgeText;
    public Image SideCardImage;

    public Sprite Life0;
    public Sprite Life1;
    public Sprite Life2;
    public Sprite LifeFull;
    public Sprite CardNP3;
    public Sprite CardAtestado;
    public Sprite CardMonster;
    public Sprite CardGPT;

    private readonly Color aliveColor = Color.clear;
    private readonly Color emptyColor = Color.clear;
    private readonly Color deadColor = Color.clear;
    private readonly Color activeColor = Color.clear;

    public void SetPlayer(HotPotatoPlayer player, int slotNumber)
    {
        if (player == null)
        {
            if (Group != null)
            {
                Group.alpha = 1f;
            }
            if (NameText != null) NameText.text = "Slot " + slotNumber;
            if (LivesText != null)
            {
                LivesText.text = "vazio";
                LivesText.gameObject.SetActive(true);
            }
            if (LivesImage != null)
            {
                LivesImage.gameObject.SetActive(false);
            }
            if (CardText != null)
            {
                CardText.text = "";
                CardText.gameObject.SetActive(true);
            }
            if (CardImage != null)
            {
                CardImage.gameObject.SetActive(false);
            }
            if (BadgeText != null) BadgeText.gameObject.SetActive(false);
            if (Lamp != null) Lamp.color = Color.clear;
            if (LightOverlay != null) LightOverlay.color = Color.clear;
            if (Avatar != null) Avatar.color = new Color(0.18f, 0.18f, 0.24f, 0.45f);
            if (SlotBackground != null) SlotBackground.color = emptyColor;
            return;
        }

        if (NameText != null) NameText.text = player.Name;
        SetLivesSprite(player.Lives);
        SetCardSprite(player.HeldCard);
        UpdateSideCard(player.HeldCard);
        if (Avatar != null) Avatar.color = player.Eliminated ? new Color(0.18f, 0.18f, 0.20f, 0.40f) : new Color(0.15f, 0.75f, 1f, 0.95f);
        if (SlotBackground != null) SlotBackground.color = player.Eliminated ? deadColor : aliveColor;
        if (Group != null && !player.Eliminated) Group.alpha = 1f;
        if (Lamp != null) Lamp.color = Color.clear;
        if (LightOverlay != null) LightOverlay.color = Color.clear;
    }

    private void SetLivesSprite(int lives)
    {
        Sprite sprite = lives >= 3 ? LifeFull : lives == 2 ? Life2 : lives == 1 ? Life1 : Life0;

        if (LivesImage != null && sprite != null)
        {
            LivesImage.sprite = sprite;
            LivesImage.gameObject.SetActive(true);
            if (LivesText != null) LivesText.gameObject.SetActive(false);
            return;
        }

        if (LivesText != null)
        {
            LivesText.text = BuildLives(lives);
            LivesText.gameObject.SetActive(true);
        }
        if (LivesImage != null) LivesImage.gameObject.SetActive(false);
    }

    private void SetCardSprite(HotPotatoCardType cardType)
    {
        Sprite sprite = null;
        switch (cardType)
        {
            case HotPotatoCardType.NP3:
                sprite = CardNP3;
                break;
            case HotPotatoCardType.Atestado:
                sprite = CardAtestado;
                break;
            case HotPotatoCardType.Monster:
                sprite = CardMonster;
                break;
            case HotPotatoCardType.GPT:
                sprite = CardGPT;
                break;
        }

        if (CardImage != null && sprite != null)
        {
            CardImage.sprite = sprite;
            CardImage.gameObject.SetActive(true);
            if (CardText != null)
            {
                CardText.gameObject.SetActive(false);
            }
            return;
        }

        if (CardText != null)
        {
            CardText.text = cardType != HotPotatoCardType.None ? HotPotatoCardInfo.DisplayName(cardType) : "";
            CardText.gameObject.SetActive(true);
        }
        if (CardImage != null)
        {
            CardImage.gameObject.SetActive(false);
        }
    }

    private void UpdateSideCard(HotPotatoCardType cardType)
    {
        if (SideCardImage == null) return;

        // Atualiza tooltip
        SeatCardTooltip tooltip = SideCardImage.gameObject.GetComponent<SeatCardTooltip>();
        if (tooltip != null) tooltip.SetCardType(cardType);

        if (cardType == HotPotatoCardType.None)
        {
            SideCardImage.gameObject.SetActive(false);
            return;
        }
        Sprite sprite = null;
        switch (cardType)
        {
            case HotPotatoCardType.NP3:      sprite = CardNP3; break;
            case HotPotatoCardType.Atestado: sprite = CardAtestado; break;
            case HotPotatoCardType.Monster:  sprite = CardMonster; break;
            case HotPotatoCardType.GPT:      sprite = CardGPT; break;
        }
        if (sprite != null) SideCardImage.sprite = sprite;
        SideCardImage.gameObject.SetActive(sprite != null);
    }

    public void SetActive(bool active)
    {
        if (SlotBackground != null)
        {
            SlotBackground.color = active ? activeColor : SlotBackground.color;
        }
    }

    public void SetLamp(Color color, bool blink)
    {
        if (Lamp == null)
        {
            return;
        }

        if (blink && color.a > 0f)
        {
            float pulse = Mathf.PingPong(Time.time * 6f, 1f);
            Lamp.color = Color.Lerp(Color.clear, color, pulse);
            if (LightOverlay != null)
            {
                LightOverlay.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.08f, 0.30f, pulse));
            }
        }
        else
        {
            Lamp.color = color;
            if (LightOverlay != null)
            {
                LightOverlay.color = color.a > 0f ? new Color(color.r, color.g, color.b, 0.18f) : Color.clear;
            }
        }
    }

    public void SetBadge(bool visible, string text)
    {
        if (BadgeText == null)
        {
            return;
        }

        BadgeText.text = text;
        BadgeText.gameObject.SetActive(visible);
    }

    public void SetTargetHint(bool visible)
    {
        if (LightOverlay == null)
        {
            return;
        }

        if (visible)
        {
            LightOverlay.color = new Color(1f, 0.86f, 0.20f, 0.26f);
            return;
        }

        LightOverlay.color = Color.clear;
    }

    private static string BuildLives(int lives)
    {
        if (lives <= 0)
        {
            return "sem vidas";
        }

        string value = "";
        for (int i = 0; i < lives; i++)
        {
            value += "\u2665";
        }

        return value;
    }
}

public class core : MonoBehaviour
{
    private const string CARD_NP3_PATH = "Images/Cards/NP3";
    private const string ARROW_PATH = "Images/Arrow/SETA";
    private const string QUADRO_PATH = "Images/Background/QUADRO";
    private const string DERRAUNDE_PATH = "Images/Player/DERRAUNDE";
    private const string MUSIC_PATH = "Songs/backgroundMusic";
    private const string CARD_ATESTADO_PATH = "Images/Cards/atestado";
    private const string CARD_MONSTER_PATH = "Images/Cards/MONSTER";
    private const string CARD_GPT_PATH = "Images/Cards/GPT";
    private const string LIFE_0_PATH = "Images/LifeBar/LIFE 0 DE 3";
    private const string LIFE_1_PATH = "Images/LifeBar/LIFE 1 DE 3";
    private const string LIFE_2_PATH = "Images/LifeBar/LIFE 2 DE 3";
    private const string LIFE_FULL_PATH = "Images/LifeBar/LIFE FULL";
    private const string BACKGROUND_PATH = "Images/Background/background";

    public const float TIMER_MAX = 30f;
    public const float TIMER_BUFF = 5f;
    public const float TIME_PENALTY = 5f;
    public const float LOBBY_WAIT = 60f;
    public const int LOBBY_COUNTDOWN = 3;
    public const int MAX_PLAYERS = 8;
    public const int MIN_PLAYERS = 2;
    public const string FIXED_QUESTION = "Quanto \u00e9 7 \u00d7 8?";
    public const string FIXED_ANSWER = "56";

    [Header("Optional")]
    [SerializeField] private bool autoAddFirstBot = false;
    [SerializeField] private bool botsAnswerAutomatically = true;
    [SerializeField] private float botAnswerDelaySeconds = 1f;
    [SerializeField] private float localCardChoiceTimeoutSeconds = 5f;

    [Header("Local Player View")]
    [SerializeField] private bool localPlayerView = false;
    [SerializeField] private bool autoAddLocalPlayer = true;
    [SerializeField] private string localPlayerName = "Voce";

    private readonly List<HotPotatoPlayer> players = new List<HotPotatoPlayer>();
    private readonly List<HotPotatoSeatView> seatViews = new List<HotPotatoSeatView>();

    private HotPotatoPhase phase;
    private question questionPanel;
    private timer turnTimer;
    private card cardSystem;
    private next turnSystem;
    private music musicSystem;
    private result resultPanel;
    private opChoice opChoiceSystem;

    private Canvas canvas;
    private RectTransform root;
    private RectTransform seatsLayer;
    private RectTransform table;
    private RectTransform arrowPivot;
    private GameObject lobbyTopObject;
    private GameObject countdownTopObject;
    private Text lobbyText;
    private Text lastAnswerText;
    private Button addBotButton;
    private Button useCardButton1;
    private Image useCardIconImage1;
    private Button useCardButton2;
    private Image useCardIconImage2;
    private Text timerText;

    private Sprite cardNp3Sprite;
    private Sprite cardAtestadoSprite;
    private Sprite cardMonsterSprite;
    private Sprite cardGptSprite;
    private Sprite life0Sprite;
    private Sprite life1Sprite;
    private Sprite life2Sprite;
    private Sprite lifeFullSprite;
    private Sprite backgroundSprite;
    private Sprite arrowSprite;
    private Sprite quadroSprite;
    private Sprite derraUndeSprite;
    private AudioClip musicClip;
    private Image derraUndeImage;
    private RectTransform derraUndePivot;

    private AudioSource sfxSource;
    private AudioClip glassBreakClip;
    private AudioClip gepetoClip;
    private AudioClip tosseClip;
    private AudioClip salivaClip;
    private AudioClip monsterClip;
    private Coroutine shakeRoutine;
    private SceneBridge sceneBridge;
    private float lobbyTimer;
    private int currentPlayerIndex = -1;
    private int forcedNextPlayerIndex = -1;
    private bool resolvingTurn;
    private bool selectingAtestadoTarget;
    private Coroutine turnRoutine;
    private Coroutine botRoutine;
    private Coroutine cardChoiceAutoRoutine;
    private HotPotatoPlayer cardChoicePlayer;
    private HotPotatoPlayer localPlayer;
    private float currentArrowRotation;

    private struct ExpressionState
    {
        public string Text;
        public double Value;
        public bool HasValue;
    }

    private ExpressionState currentExpression;
    private int turnCount;
    private int consecutiveWrong;
    private bool lastAnswerCorrect;
    private bool hasPreviousAnswer;
    private bool resetExpressionNext;
    private string currentQuestionText;
    private double currentAnswerValue;
    private int turnsWithoutLifeLoss;
    private bool suddenDeathActive;
    private float currentTimerMax = TIMER_MAX;
    private Text suddenDeathText;
    private Text startCountdownText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<core>() == null)
        {
            new GameObject("HotPotatoGame").AddComponent<core>();
        }
    }

    private void Awake()
    {
        questionPanel = GetComponent<question>();
        if (questionPanel == null)
        {
            questionPanel = gameObject.AddComponent<question>();
        }

        turnTimer = GetComponent<timer>();
        if (turnTimer == null)
        {
            turnTimer = gameObject.AddComponent<timer>();
        }

        cardSystem = GetComponent<card>();
        if (cardSystem == null)
        {
            cardSystem = gameObject.AddComponent<card>();
        }

        turnSystem = GetComponent<next>();
        if (turnSystem == null)
        {
            turnSystem = gameObject.AddComponent<next>();
        }

        musicSystem = GetComponent<music>();
        if (musicSystem == null)
        {
            musicSystem = gameObject.AddComponent<music>();
        }

        resultPanel = GetComponent<result>();
        if (resultPanel == null)
        {
            resultPanel = gameObject.AddComponent<result>();
        }

        opChoiceSystem = GetComponent<opChoice>();
        if (opChoiceSystem == null)
        {
            opChoiceSystem = gameObject.AddComponent<opChoice>();
        }
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null) return sprite;
        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void Start()
    {
        backgroundSprite   = LoadSprite(BACKGROUND_PATH);
        arrowSprite        = LoadSprite(ARROW_PATH);
        quadroSprite       = LoadSprite(QUADRO_PATH);
        derraUndeSprite    = LoadSprite(DERRAUNDE_PATH);
        musicClip          = Resources.Load<AudioClip>(MUSIC_PATH);
        if (musicClip != null) musicSystem.ChangeMusic(musicClip);
        life0Sprite        = LoadSprite(LIFE_0_PATH);
        life1Sprite        = LoadSprite(LIFE_1_PATH);
        life2Sprite        = LoadSprite(LIFE_2_PATH);
        lifeFullSprite     = LoadSprite(LIFE_FULL_PATH);
        cardNp3Sprite      = LoadSprite(CARD_NP3_PATH);
        cardAtestadoSprite = LoadSprite(CARD_ATESTADO_PATH);
        cardMonsterSprite  = LoadSprite(CARD_MONSTER_PATH);
        cardGptSprite      = LoadSprite(CARD_GPT_PATH);

        glassBreakClip = Resources.Load<AudioClip>("Songs/vidroquebrando");
        gepetoClip     = Resources.Load<AudioClip>("Songs/gepeto");
        tosseClip      = Resources.Load<AudioClip>("Songs/tosse");
        salivaClip     = Resources.Load<AudioClip>("Songs/saliva");
        monsterClip    = Resources.Load<AudioClip>("Songs/monster");
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        sceneBridge = FindObjectOfType<SceneBridge>();
        BuildInterface();
        ResetToLobby();

        if (autoAddLocalPlayer)
        {
            AddLocalPlayer();
        }

        if (autoAddFirstBot)
        {
            AddBot();
        }
    }

    private void Update()
    {
        if (phase == HotPotatoPhase.Lobby && players.Count >= MIN_PLAYERS)
        {
            lobbyTimer -= Time.deltaTime;
            UpdateLobbyText();

            if (lobbyTimer <= 0f)
            {
                StartGameCountdown();
            }
        }

        if (phase == HotPotatoPhase.Playing && turnTimer.Running && !resolvingTurn)
        {
            turnTimer.Tick(Time.deltaTime);
            UpdateActiveLamp();

            if (turnTimer.Remaining <= 0f)
            {
                ResolveTimeout();
            }
        }
    }

    public void AddBot()
    {
        if (players.Count >= MAX_PLAYERS || phase != HotPotatoPhase.Lobby)
        {
            return;
        }

        HotPotatoPlayer player = new HotPotatoPlayer();
        player.Name = "Bot " + GetNextBotNumber();
        player.IsLocal = false;
        player.View = seatViews[players.Count];
        players.Add(player);
        sceneBridge?.SetPlayerCount(players.Count);
        RefreshSeats(true);

        if (players.Count == MIN_PLAYERS)
        {
            lobbyTimer = LOBBY_WAIT;
        }

        if (players.Count >= MAX_PLAYERS)
        {
            StartGameCountdown();
        }
        else
        {
            UpdateLobbyText();
        }
    }

    public void AddLocalPlayer()
    {
        if (players.Count >= MAX_PLAYERS || phase != HotPotatoPhase.Lobby || localPlayer != null)
        {
            return;
        }

        HotPotatoPlayer player = new HotPotatoPlayer();
        player.Name = string.IsNullOrEmpty(localPlayerName) ? "Voce" : localPlayerName;
        player.IsLocal = true;
        player.View = seatViews[players.Count];
        players.Add(player);
        localPlayer = player;
        sceneBridge?.SetPlayerCount(players.Count);
        RefreshSeats(true);

        if (players.Count == MIN_PLAYERS)
        {
            lobbyTimer = LOBBY_WAIT;
        }

        if (players.Count >= MAX_PLAYERS)
        {
            StartGameCountdown();
        }
        else
        {
            UpdateLobbyText();
        }
    }

    public void SubmitAnswer(string answer)
    {
        if (phase != HotPotatoPhase.Playing || resolvingTurn || currentPlayerIndex < 0)
        {
            return;
        }

        HotPotatoPlayer player = players[currentPlayerIndex];
        string submittedAnswer = answer.Trim();
        double parsedValue;
        if (!TryParseAnswer(submittedAnswer, out parsedValue))
        {
            questionPanel.ClearInput();
            return;
        }

        double expectedValue = currentAnswerValue;
        double submittedValue = Round2(parsedValue);
        bool correct = System.Math.Abs(Round2(expectedValue) - submittedValue) < 0.05;
        if (correct)
        {
            UpdateLastAnswer(player.Name, submittedAnswer, true);
            hasPreviousAnswer = true;
        }

        if (correct)
        {
            lastAnswerCorrect = true;
            consecutiveWrong = 0;
            turnsWithoutLifeLoss += 1;
            CheckSuddenDeath();
            bool earnedCard = turnTimer.Elapsed < TIMER_BUFF && player.HeldCard == HotPotatoCardType.None;
            if (earnedCard)
            {
                questionPanel.SetInteractable(false);
                if (player.IsLocal)
                {
                    OfferOperationChoice(player, delegate {
                        OfferLocalCardChoice(player);
                        PassTurnSoon(0.35f);
                    });
                }
                else
                {
                    OfferOperationChoice(player, delegate { OfferCardChoice(player); });
                }
                return;
            }

            questionPanel.SetInteractable(false);
            OfferOperationChoice(player, delegate { PassTurnSoon(0.55f); });
            return;
        }

        lastAnswerCorrect = false;
        consecutiveWrong += 1;
        if (suddenDeathActive)
        {
            suddenDeathActive = false;
            currentTimerMax = TIMER_MAX;
            if (suddenDeathText != null) suddenDeathText.gameObject.SetActive(false);
        }
        turnsWithoutLifeLoss = 0;
        // ao errar: perde vida e reseta a expressao para uma conta simples
        resetExpressionNext = true;
        consecutiveWrong = 0;
        ApplyLifeLoss(player);
        RefreshPlayerAfterLifeLoss(player);
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
        ApplyWrongAnswerPenalty(player);
    }

    private bool IsAnswerTextValid(string answer)
    {
        if (string.IsNullOrEmpty(answer))
        {
            return false;
        }

        if (answer == "-")
        {
            return false;
        }

        int separatorCount = 0;
        for (int i = 0; i < answer.Length; i++)
        {
            if (answer[i] == ',' || answer[i] == '.')
            {
                separatorCount += 1;
                if (separatorCount > 1)
                {
                    return false;
                }
            }
        }

        int separatorIndex = answer.IndexOf(',');
        if (separatorIndex < 0)
        {
            separatorIndex = answer.IndexOf('.');
        }

        if (separatorIndex >= 0)
        {
            string decimals = answer.Substring(separatorIndex + 1);
            if (decimals.Length > 2)
            {
                return false;
            }
        }

        for (int i = 0; i < answer.Length; i++)
        {
            char c = answer[i];
            if (i == 0 && c == '-')
            {
                continue;
            }
            if ((c >= '0' && c <= '9') || c == ',' || c == '.')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private bool TryParseAnswer(string answer, out double value)
    {
        value = 0d;
        if (!IsAnswerTextValid(answer))
        {
            return false;
        }

        string normalized = answer.Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private double Round2(double value)
    {
        return System.Math.Round(value, 1, System.MidpointRounding.AwayFromZero);
    }

    private string FormatAnswerValue(double value)
    {
        double rounded = Round2(value);
        return rounded.ToString("0.0", CultureInfo.InvariantCulture);
    }

    private int GetStageForTurn(int turnNumber)
    {
        if (turnNumber <= 15)
        {
            return 1;
        }

        if (turnNumber <= 45)
        {
            return 2;
        }

        if (turnNumber <= 95)
        {
            return 3;
        }

        if (turnNumber <= 145)
        {
            return 4;
        }

        if (turnNumber <= 245)
        {
            return 5;
        }

        return 6;
    }

    private string FormatQuestionText(string text)
    {
        // Insert a newline after every 10th operator token (+ - * / ^ =)
        char[] ops = new char[] { '+', '-', '*', '/', '^', '=' };
        int opCount = 0;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            sb.Append(text[i]);
            if (System.Array.IndexOf(ops, text[i]) >= 0)
            {
                opCount++;
                if (opCount % 8 == 0)
                    sb.Append('\n');
            }
        }
        return sb.ToString();
    }

    private void PrepareQuestionForTurn()
    {
        turnCount += 1;
        int stage = GetStageForTurn(turnCount);

        if (!currentExpression.HasValue || resetExpressionNext)
        {
            currentExpression = CreateBaseExpression(stage);
            resetExpressionNext = false;
        }
        else if (hasPreviousAnswer && lastAnswerCorrect)
        {
            currentExpression = AppendTerm(currentExpression, stage);
        }

        currentQuestionText = FormatQuestionText(currentExpression.Text);
        currentAnswerValue = currentExpression.Value;
    }

    private struct Term
    {
        public string Text;
        public double Value;
    }

    private ExpressionState CreateBaseExpression(int stage)
    {
        Term first = GenerateTerm(stage);
        Term second = GenerateTerm(stage);
        string op = PickOperator(stage);

        ExpressionState state = new ExpressionState
        {
            Text = first.Text,
            Value = first.Value,
            HasValue = true
        };

        return ApplyOperator(state, op, second, stage, false);
    }

    private ExpressionState AppendTerm(ExpressionState current, int stage)
    {
        Term next = GenerateTerm(stage);
        string op = PickOperator(stage);
        return ApplyOperator(current, op, next, stage, false);
    }

    private string PickOperator(int stage)
    {
        switch (stage)
        {
            case 1:
                return Random.value < 0.5f ? "+" : "-";
            case 2:
                return Random.value < 0.5f ? "*" : "/";
            case 3:
                return Random.value < 0.5f ? "^" : "root";
            case 4:
                int pick = Random.Range(0, 4);
                if (pick == 0)
                {
                    return "+";
                }
                if (pick == 1)
                {
                    return "-";
                }
                if (pick == 2)
                {
                    return "*";
                }
                return "/";
            case 5:
            case 6:
                return Random.value < 0.5f ? "+" : "-";
            default:
                return "+";
        }
    }

    private Term GenerateTerm(int stage)
    {
        if (stage == 5)
        {
            return GenerateTrigTerm();
        }

        if (stage >= 6)
        {
            return GenerateFactorialTerm();
        }

        int value = stage == 2 ? Random.Range(2, 13) : Random.Range(1, 21);
        return new Term
        {
            Text = value.ToString(),
            Value = value
        };
    }

    private Term GenerateTrigTerm()
    {
        int func = Random.Range(0, 3);
        int[] angles = func == 2
            ? new int[] { 0, 30, 45, 60 }
            : new int[] { 0, 30, 45, 60, 90 };
        int angle = angles[Random.Range(0, angles.Length)];
        double radians = angle * System.Math.PI / 180.0;
        double value = 0d;
        string funcName = "sen";
        if (func == 0)
        {
            value = System.Math.Sin(radians);
            funcName = "sen";
        }
        else if (func == 1)
        {
            value = System.Math.Cos(radians);
            funcName = "cos";
        }
        else
        {
            value = System.Math.Tan(radians);
            funcName = "tan";
        }

        return new Term
        {
            Text = funcName + "(" + angle + ")",
            Value = value
        };
    }

    private Term GenerateFactorialTerm()
    {
        int n = Random.Range(1, 6);
        return new Term
        {
            Text = n + "!",
            Value = Factorial(n)
        };
    }

    private int Factorial(int value)
    {
        int result = 1;
        for (int i = 2; i <= value; i++)
        {
            result *= i;
        }

        return result;
    }

    private ExpressionState ApplyOperator(ExpressionState current, string op, Term term, int stage, bool allowWrap)
    {
        string left = current.Text;
        if (op == "*" || op == "/" || op == "^" || op == "root")
        {
            left = "(" + left + ")";
        }

        if (allowWrap || (stage >= 4 && Random.value < 0.05f))
        {
            left = WrapWithRandomBracket(left);
        }

        double nextValue = current.Value;
        string nextText = current.Text;

        switch (op)
        {
            case "+":
                nextValue = current.Value + term.Value;
                nextText = left + " + " + term.Text;
                break;
            case "-":
                nextValue = current.Value - term.Value;
                nextText = left + " - " + term.Text;
                break;
            case "*":
                nextValue = current.Value * term.Value;
                nextText = left + " * " + term.Text;
                break;
            case "/":
                if (System.Math.Abs(term.Value) < 0.0001)
                {
                    term.Value = 1d;
                    term.Text = "1";
                }
                nextValue = current.Value / term.Value;
                nextText = left + " / " + term.Text;
                break;
            case "^":
                int exponent = Random.Range(2, 4);
                nextValue = System.Math.Pow(current.Value, exponent);
                nextText = left + " ^ " + exponent;
                break;
            case "root":
                int index = Random.Range(2, 4);
                double baseValue = System.Math.Abs(current.Value);
                nextValue = System.Math.Pow(baseValue, 1.0 / index);
                nextText = "raiz" + index + "(" + left + ")";
                break;
        }

        return new ExpressionState
        {
            Text = nextText,
            Value = nextValue,
            HasValue = true
        };
    }

    private string WrapWithRandomBracket(string expression)
    {
        int choice = Random.Range(0, 3);
        if (choice == 0)
        {
            return "(" + expression + ")";
        }
        if (choice == 1)
        {
            return "[" + expression + "]";
        }
        return "{" + expression + "}";
    }

    private void UpdateLastAnswer(string playerName, string answer, bool correct)
    {
        if (lastAnswerText == null)
        {
            return;
        }

        string shownAnswer = string.IsNullOrEmpty(answer) ? "vazia" : answer;
        if(correct)
        {
            lastAnswerText.text = "Ultima resposta: \n" + FormatAnswerValue(currentAnswerValue);
        }
    }

    private void ClearLastAnswer()
    {
        if (lastAnswerText == null)
        {
            return;
        }

        lastAnswerText.text = "Ultima resposta: \n";
        lastAnswerText.color = scene.Hex("#d8edff");
    }

    private void ApplyWrongAnswerPenalty(HotPotatoPlayer player)
    {
        if (resolvingTurn || !turnTimer.Running)
        {
            return;
        }

        turnTimer.Penalize(TIME_PENALTY);
        questionPanel.ClearInput();

        if (turnTimer.Remaining <= 0f)
        {
            ResolveTimeout();
        }
    }

    private IEnumerator LoseLifeAndContinueRoutine(HotPotatoPlayer player, string aliveMessage, string eliminatedMessage, float delay)
    {
        ApplyLifeLoss(player);
        RefreshPlayerAfterLifeLoss(player);

        yield return new WaitForSeconds(delay);

        if (AliveCount() <= 1)
        {
            EndGame();
        }
        else
        {
            StartNextTurn();
        }
    }

    private void ApplyLifeLoss(HotPotatoPlayer player)
    {
        player.Lives = Mathf.Max(0, player.Lives - 1);
        if (player.Lives <= 0) player.Eliminated = true;
        if (sfxSource != null && glassBreakClip != null) sfxSource.PlayOneShot(glassBreakClip);
        turnsWithoutLifeLoss = 0;
        if (suddenDeathActive)
        {
            suddenDeathActive = false;
            currentTimerMax = TIMER_MAX;
            resetExpressionNext = true;  // volta para conta fácil após morte súbita
            if (suddenDeathText != null) suddenDeathText.gameObject.SetActive(false);
        }
    }

    private void RefreshPlayerAfterLifeLoss(HotPotatoPlayer player)
    {
        int index = players.IndexOf(player);
        if (index < 0)
        {
            return;
        }

        player.View.SetLamp(Color.clear, false);
        player.View.SetPlayer(player, index + 1);
        player.View.SetBadge(player.Eliminated, "NP3");

        if (player.Eliminated)
        {
            StartCoroutine(FadeOutSeat(player.View));
        }
    }

    private IEnumerator FadeOutSeat(HotPotatoSeatView view)
    {
        if (view == null || view.Root == null)
        {
            yield break;
        }

        CanvasGroup group = view.Group;
        if (group == null)
        {
            group = view.Root.gameObject.AddComponent<CanvasGroup>();
            view.Group = group;
        }

        float duration = 0.65f;
        float elapsed = 0f;
        float startAlpha = group.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }

        group.alpha = 0f;
        view.Root.gameObject.SetActive(false);
    }

    private void OfferCardChoice(HotPotatoPlayer player)
    {
        List<HotPotatoCardType> options = BuildCardOptions(player);
        if (player.HeldCard != HotPotatoCardType.None || options.Count < 2)
        {
            StartCoroutine(PassTurnRoutine(0.55f));
            return;
        }

        HotPotatoCardType firstOption = TakeRandomCardOption(options);
        HotPotatoCardType secondOption = TakeRandomCardOption(options);
        HotPotatoCardType autoChoice = Random.value < 0.5f ? firstOption : secondOption;

        resolvingTurn = true;
        StopBotRoutine();
        turnTimer.StopTimer();
        cardChoicePlayer = player;

        cardChoiceAutoRoutine = StartCoroutine(AutoChooseCardNoUiRoutine(player, autoChoice));
    }

    private void OfferLocalCardChoice(HotPotatoPlayer player)
    {
        List<HotPotatoCardType> options = BuildCardOptions(player);
        if (player.HeldCard != HotPotatoCardType.None || options.Count < 2)
        {
            return;
        }

        HotPotatoCardType firstOption = TakeRandomCardOption(options);
        HotPotatoCardType secondOption = TakeRandomCardOption(options);
        HotPotatoCardType autoChoice = Random.value < 0.5f ? firstOption : secondOption;

        cardChoicePlayer = player;
        cardSystem.ShowFloatingCard(player.Name);
        cardSystem.ShowChoice(player.Name, firstOption, secondOption, delegate (HotPotatoCardType chosenCard)
        {
            FinishLocalCardChoice(player, chosenCard);
        });

        if (cardChoiceAutoRoutine != null)
        {
            StopCoroutine(cardChoiceAutoRoutine);
        }
        cardChoiceAutoRoutine = StartCoroutine(LocalCardChoiceTimeoutRoutine(player, autoChoice));
    }

    private List<HotPotatoCardType> BuildCardOptions(HotPotatoPlayer player)
    {
        List<HotPotatoCardType> options = new List<HotPotatoCardType>();

        if (CanUseNP3(player))
        {
            options.Add(HotPotatoCardType.NP3);
        }

        options.Add(HotPotatoCardType.Atestado);
        options.Add(HotPotatoCardType.Monster);
        options.Add(HotPotatoCardType.GPT);
        return options;
    }

    private HotPotatoCardType TakeRandomCardOption(List<HotPotatoCardType> options)
    {
        int index = Random.Range(0, options.Count);
        HotPotatoCardType cardType = options[index];
        options.RemoveAt(index);
        return cardType;
    }

    private IEnumerator AutoChooseCardRoutine(HotPotatoPlayer player, HotPotatoCardType cardType)
    {
        yield return new WaitForSeconds(1.5f);

        if (cardChoicePlayer == player)
        {
            cardSystem.ChooseCard(cardType);
        }
    }

    private IEnumerator AutoChooseCardNoUiRoutine(HotPotatoPlayer player, HotPotatoCardType cardType)
    {
        yield return new WaitForSeconds(1.2f);

        if (cardChoicePlayer == player)
        {
            FinishCardChoice(player, cardType);
        }
    }

    private IEnumerator LocalCardChoiceTimeoutRoutine(HotPotatoPlayer player, HotPotatoCardType cardType)
    {
        float wait = Mathf.Max(0f, localCardChoiceTimeoutSeconds);
        yield return new WaitForSeconds(wait);

        if (cardChoicePlayer == player)
        {
            FinishLocalCardChoice(player, cardType);
        }
    }

    private void FinishCardChoice(HotPotatoPlayer player, HotPotatoCardType chosenCard)
    {
        if (cardChoiceAutoRoutine != null)
        {
            StopCoroutine(cardChoiceAutoRoutine);
            cardChoiceAutoRoutine = null;
        }

        if (cardChoicePlayer != player || player.HeldCard != HotPotatoCardType.None)
        {
            return;
        }

        cardChoicePlayer = null;
        ApplyCardChoice(player, chosenCard, true);
    }

    private void FinishLocalCardChoice(HotPotatoPlayer player, HotPotatoCardType chosenCard)
    {
        if (cardChoiceAutoRoutine != null)
        {
            StopCoroutine(cardChoiceAutoRoutine);
            cardChoiceAutoRoutine = null;
        }

        if (cardChoicePlayer != player || player.HeldCard != HotPotatoCardType.None)
        {
            return;
        }

        cardChoicePlayer = null;
        ApplyCardChoice(player, chosenCard, false);
        cardSystem.HideChoice();
    }

    private void ApplyCardChoice(HotPotatoPlayer player, HotPotatoCardType chosenCard, bool passTurn)
    {
        if (player.HeldCard == HotPotatoCardType.None)
        {
            player.HeldCard = chosenCard;
        }
        else
        {
            player.HeldCard2 = chosenCard;
        }
        player.View.SetPlayer(player, players.IndexOf(player) + 1);
        UpdateUseCardButtons();
        if (passTurn)
        {
            StartCoroutine(PassTurnRoutine(0.55f));
        }
    }

    private void OfferOperationChoice(HotPotatoPlayer player, Action afterChoice)
    {
        if (!player.IsLocal)
        {
            // bots escolhem automaticamente sem UI
            string[] opts = GenerateOperationOptions();
            ApplyChosenOperation(opts[Random.Range(0, opts.Length)]);
            afterChoice();
            return;
        }

        string[] options = GenerateOperationOptions();
        opChoiceSystem.Show(options, 5f, delegate(string chosen)
        {
            ApplyChosenOperation(chosen);
            afterChoice();
        });
    }

    private string[] GenerateOperationOptions()
    {
        List<string> pool = new List<string>();

        // Pesos: operacoes simples tem peso maior, complexas menor
        // +/- sempre disponiveis (peso 3 cada)
        int addVal  = Random.Range(1, 101);
        int subVal  = Random.Range(1, 101);
        pool.Add("+" + addVal); pool.Add("+" + addVal); pool.Add("+" + addVal);
        pool.Add("-" + subVal); pool.Add("-" + subVal); pool.Add("-" + subVal);

        if (turnCount >= 15)
        {
            // */  peso 2
            int mulN = Random.Range(2, 16);
            int divN = Random.Range(2, 16);
            pool.Add("*" + mulN); pool.Add("*" + mulN);
            pool.Add("/" + divN); pool.Add("/" + divN);
        }

        if (turnCount >= 30)
        {
            // ^ peso 1
            int expN = Random.Range(2, 4);
            pool.Add("^" + expN);
        }

        if (turnCount >= 45 && !currentExpression.HasValue)
        {
            // ! so no inicio, peso 1
            pool.Add("!");
        }

        // embaralha e pega 3 distintos
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            string tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
        }

        // garante 3 opcoes distintas
        List<string> chosen = new List<string>();
        for (int i = 0; i < pool.Count && chosen.Count < 3; i++)
        {
            string op = pool[i];
            // evita duplicatas de mesmo tipo+valor
            bool dup = false;
            for (int k = 0; k < chosen.Count; k++)
                if (chosen[k] == op) { dup = true; break; }
            if (!dup) chosen.Add(op);
        }
        while (chosen.Count < 3)
        {
            int addFill = Random.Range(1, 101);
            chosen.Add("+" + addFill);
        }

        return chosen.ToArray();
    }

    private void ApplyChosenOperation(string op)
    {
        string left = currentExpression.Text;
        bool needsParens = op.StartsWith("*") || op.StartsWith("/") || op.StartsWith("^") || op == "!";
        if (needsParens) left = "(" + left + ")";

        if (op.StartsWith("+"))
        {
            double n = double.Parse(op.Substring(1), CultureInfo.InvariantCulture);
            currentExpression = new ExpressionState { Text = left + " + " + op.Substring(1), Value = currentExpression.Value + n, HasValue = true };
        }
        else if (op.StartsWith("-"))
        {
            double n = double.Parse(op.Substring(1), CultureInfo.InvariantCulture);
            currentExpression = new ExpressionState { Text = left + " - " + op.Substring(1), Value = currentExpression.Value - n, HasValue = true };
        }
        else if (op.StartsWith("*"))
        {
            double n = double.Parse(op.Substring(1), CultureInfo.InvariantCulture);
            currentExpression = new ExpressionState { Text = left + " * " + op.Substring(1), Value = currentExpression.Value * n, HasValue = true };
        }
        else if (op.StartsWith("/"))
        {
            double n = double.Parse(op.Substring(1), CultureInfo.InvariantCulture);
            if (System.Math.Abs(n) < 0.0001) n = 1;
            currentExpression = new ExpressionState { Text = left + " / " + op.Substring(1), Value = currentExpression.Value / n, HasValue = true };
        }
        else if (op.StartsWith("^"))
        {
            int exp = int.Parse(op.Substring(1));
            currentExpression = new ExpressionState { Text = left + " ^ " + exp, Value = System.Math.Pow(currentExpression.Value, exp), HasValue = true };
        }
        else if (op == "!")
        {
            int n = Mathf.Clamp(Mathf.RoundToInt((float)currentExpression.Value), 0, 7);
            currentExpression = new ExpressionState { Text = left + "!", Value = Factorial(n), HasValue = true };
        }
        currentQuestionText = FormatQuestionText(currentExpression.Text);
        currentAnswerValue  = currentExpression.Value;
        lastAnswerCorrect = false;
        hasPreviousAnswer = false;
    }

    private IEnumerator ShakeRoutine()
    {
        Vector2 origin = root.anchoredPosition;
        float duration = 0.35f;
        float magnitude = 12f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - elapsed / duration;
            root.anchoredPosition = origin + new Vector2((Random.value * 2f - 1f) * magnitude * t, (Random.value * 2f - 1f) * magnitude * t);
            yield return null;
        }
        root.anchoredPosition = origin;
        shakeRoutine = null;
    }

    private void CheckSuddenDeath()
    {
        if (turnCount < 55) return;
        if (!suddenDeathActive && turnsWithoutLifeLoss >= 10)
        {
            suddenDeathActive = true;
            if (suddenDeathText != null) suddenDeathText.gameObject.SetActive(true);
        }
        if (suddenDeathActive)
            currentTimerMax = Mathf.Max(1f, currentTimerMax - 1f);
    }

    private bool CanUseNP3(HotPotatoPlayer player)
    {
        return player != null && player.Lives < 3 && player.Lives > 1;
    }



    private void UseNP3(HotPotatoPlayer player, bool isFirstSlot)
    {
        if (!CanUseNP3(player))
        {
            return;
        }

        cardSystem.PlayUseAnimation(HotPotatoCardType.NP3);
        if (sfxSource != null && salivaClip != null) sfxSource.PlayOneShot(salivaClip);
        player.Lives = Mathf.Min(3, player.Lives + 1);
        ConsumeCard(player, isFirstSlot);
        player.View.SetPlayer(player, currentPlayerIndex + 1);
    }

    private void UseMonster(HotPotatoPlayer player, bool isFirstSlot)
    {
        if (!turnTimer.Running || turnTimer.Remaining <= 0f)
        {
            return;
        }

        cardSystem.PlayUseAnimation(HotPotatoCardType.Monster);
        if (sfxSource != null && monsterClip != null) sfxSource.PlayOneShot(monsterClip);
        musicSystem.SetSpeed(0.75f);
        float boostedTime = turnTimer.Remaining * 2f;
        turnTimer.SetRemainingTime(boostedTime);
        ConsumeCard(player, isFirstSlot);
    }

    private void UseGPT(HotPotatoPlayer player, bool isFirstSlot)
    {
        cardSystem.PlayUseAnimation(HotPotatoCardType.GPT);
        if (sfxSource != null && gepetoClip != null) sfxSource.PlayOneShot(gepetoClip);
        ConsumeCard(player, isFirstSlot);
        UpdateLastAnswer(player.Name, "GPT", true);
        questionPanel.SetInteractable(false);
        PassTurnSoon(0.35f);
    }

    private void BeginAtestadoTargetSelection(HotPotatoPlayer player, bool isFirstSlot)
    {
        if (AliveCount() <= 1)
        {
            return;
        }

        if (!player.IsLocal)
        {
            return;
        }

        selectingAtestadoTarget = true;
        StopBotRoutine();
        questionPanel.SetInteractable(false);
        cardSystem.PlayUseAnimation(HotPotatoCardType.Atestado);
        if (sfxSource != null && tosseClip != null) sfxSource.PlayOneShot(tosseClip);
        SetTargetSelection(true);
    }

    private void ConsumeCard(HotPotatoPlayer player, bool isFirstSlot)
    {
        if (isFirstSlot)
        {
            player.HeldCard = HotPotatoCardType.None;
        }
        else
        {
            player.HeldCard2 = HotPotatoCardType.None;
        }
        int index = players.IndexOf(player);
        if (index >= 0)
        {
            player.View.SetPlayer(player, index + 1);
            player.View.SetActive(index == currentPlayerIndex && player.Alive);
        }
    }

    private Sprite GetCardSprite(HotPotatoCardType cardType)
    {
        switch (cardType)
        {
            case HotPotatoCardType.NP3:      return cardNp3Sprite;
            case HotPotatoCardType.Atestado: return cardAtestadoSprite;
            case HotPotatoCardType.Monster:  return cardMonsterSprite;
            case HotPotatoCardType.GPT:      return cardGptSprite;
            default: return null;
        }
    }

    private void CreateCardSlot(RectTransform parent, int slotIndex, Vector2 position, ref Button outButton, ref Image outIcon)
    {
        GameObject slotObject = scene.CreateUIObject("CardSlot" + slotIndex, parent);
        RectTransform slotRect = slotObject.GetComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(1f, 0f);
        slotRect.anchorMax = new Vector2(1f, 0f);
        slotRect.pivot = new Vector2(1f, 0f);
        slotRect.anchoredPosition = position;
        slotRect.sizeDelta = new Vector2(140f, 140f);

        Image slotBg = slotObject.AddComponent<Image>();
        slotBg.sprite = scene.CreateRoundedRectSprite(140, 140, 14, scene.Hex("#ffffff"), scene.Hex("#ffffff"), 0);
        slotBg.type = Image.Type.Sliced;
        slotBg.color = new Color(1f, 1f, 1f, 0.10f);

        Button slotButton = slotObject.AddComponent<Button>();
        slotButton.targetGraphic = slotBg;
        int capturedIndex = slotIndex;
        slotButton.onClick.AddListener(delegate { UseCardFromSlot(capturedIndex); });
        ColorBlock cb = slotButton.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        cb.disabledColor = new Color(1f, 1f, 1f, 0.25f);
        slotButton.colors = cb;

        GameObject iconObject = scene.CreateUIObject("CardIcon", slotObject.transform);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        scene.Stretch(iconRect);
        Image icon = iconObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.color = Color.clear;

        slotButton.gameObject.SetActive(false);

        outButton = slotButton;
        outIcon = icon;
    }

    private void UseCardFromSlot(int slotIndex)
    {
        if (phase != HotPotatoPhase.Playing || resolvingTurn || currentPlayerIndex < 0 || currentPlayerIndex >= players.Count)
        {
            return;
        }

        HotPotatoPlayer player = players[currentPlayerIndex];
        if (!player.IsLocal)
        {
            return;
        }

        HotPotatoCardType cardToUse = slotIndex == 0 ? player.HeldCard : player.HeldCard2;
        if (cardToUse == HotPotatoCardType.None)
        {
            return;
        }

        UseCard(player, cardToUse, slotIndex == 0);
    }

    private void UseCard(HotPotatoPlayer player, HotPotatoCardType cardType, bool isFirstSlot)
    {
        switch (cardType)
        {
            case HotPotatoCardType.NP3:
                UseNP3(player, isFirstSlot);
                break;
            case HotPotatoCardType.Atestado:
                BeginAtestadoTargetSelection(player, isFirstSlot);
                break;
            case HotPotatoCardType.Monster:
                UseMonster(player, isFirstSlot);
                break;
            case HotPotatoCardType.GPT:
                UseGPT(player, isFirstSlot);
                break;
        }

        UpdateUseCardButtons();
    }

    private void UpdateUseCardButtons()
    {
        UpdateCardButton(ref useCardButton1, ref useCardIconImage1, 0);
        UpdateCardButton(ref useCardButton2, ref useCardIconImage2, 1);
    }

    private void UpdateCardButton(ref Button button, ref Image icon, int slotIndex)
    {
        if (button == null || icon == null)
        {
            return;
        }

        if (localPlayerView)
        {
            button.gameObject.SetActive(false);
            return;
        }

        bool canShow = phase == HotPotatoPhase.Playing;
        button.gameObject.SetActive(canShow);

        if (!canShow || currentPlayerIndex < 0 || currentPlayerIndex >= players.Count)
        {
            icon.color = Color.clear;
            return;
        }

        HotPotatoPlayer player = players[currentPlayerIndex];
        if (!player.IsLocal)
        {
            button.gameObject.SetActive(false);
            return;
        }

        HotPotatoCardType cardType = slotIndex == 0 ? player.HeldCard : player.HeldCard2;
        if (cardType == HotPotatoCardType.None)
        {
            icon.color = Color.clear;
            button.interactable = false;
            return;
        }

        Sprite cardSprite = GetCardSprite(cardType);
        icon.sprite = cardSprite;
        icon.color = cardSprite != null ? Color.white : Color.clear;
        button.interactable = !resolvingTurn && !selectingAtestadoTarget && CanUseHeldCard(player, cardType);
    }

    private bool CanUseHeldCard(HotPotatoPlayer player, HotPotatoCardType cardType)
    {
        switch (cardType)
        {
            case HotPotatoCardType.NP3:
                return CanUseNP3(player);
            case HotPotatoCardType.Atestado:
                return AliveCount() > 1;
            case HotPotatoCardType.Monster:
                return turnTimer.Running && turnTimer.Remaining > 0f;
            case HotPotatoCardType.GPT:
                return true;
            default:
                return false;
        }
    }

    private void OnSeatClicked(int seatIndex)
    {
        if (!selectingAtestadoTarget || currentPlayerIndex < 0 || seatIndex < 0 || seatIndex >= players.Count)
        {
            return;
        }

        if (seatIndex == currentPlayerIndex || !players[seatIndex].Alive)
        {
            return;
        }

        HotPotatoPlayer player = players[currentPlayerIndex];
        HotPotatoPlayer target = players[seatIndex];
        selectingAtestadoTarget = false;
        SetTargetSelection(false);
        ConsumeCard(player, true);
        forcedNextPlayerIndex = seatIndex;
        resolvingTurn = true;
        turnTimer.StopTimer();
        UpdateUseCardButtons();
        StartCoroutine(PassTurnRoutine(0.35f));
    }

    private void SetTargetSelection(bool enabled)
    {
        for (int i = 0; i < players.Count; i++)
        {
            HotPotatoPlayer player = players[i];
            bool validTarget = enabled && i != currentPlayerIndex && player.Alive;
            player.View.SetTargetHint(validTarget);
        }
    }

    public void ResetToLobby()
    {
        StopRunningCoroutines();
        phase = HotPotatoPhase.Lobby;
        resolvingTurn = false;
        selectingAtestadoTarget = false;
        currentPlayerIndex = -1;
        forcedNextPlayerIndex = -1;
        cardChoicePlayer = null;
        localPlayer = null;
        currentExpression = new ExpressionState();
        turnCount = 0;
        consecutiveWrong = 0;
        lastAnswerCorrect = false;
        hasPreviousAnswer = false;
        resetExpressionNext = false;
        currentQuestionText = "";
        currentAnswerValue = 0d;
        turnsWithoutLifeLoss = 0;
        suddenDeathActive = false;
        currentTimerMax = TIMER_MAX;
        if (suddenDeathText != null) suddenDeathText.gameObject.SetActive(false);
        if (startCountdownText != null) startCountdownText.gameObject.SetActive(false);
        if (lobbyTopObject != null) lobbyTopObject.SetActive(true);
        lobbyTimer = 0f;
        players.Clear();
        sceneBridge?.ResetAll();
        if (autoAddLocalPlayer)
        {
            AddLocalPlayer();
        }
        turnTimer.StopTimer();
        questionPanel.Hide();
        cardSystem.HideChoice();
        opChoiceSystem.Hide();
        resultPanel.Hide();
        if (addBotButton != null)
        {
            addBotButton.gameObject.SetActive(true);
        }
        if (useCardButton1 != null)
        {
            useCardButton1.gameObject.SetActive(false);
        }
        if (useCardButton2 != null)
        {
            useCardButton2.gameObject.SetActive(false);
        }
        musicSystem.StopMusic();
        RefreshSeats(true);
        SetTargetSelection(false);
        UpdateLobbyText();
        ClearLastAnswer();
        if (arrowPivot != null) arrowPivot.localRotation = Quaternion.identity;
        currentArrowRotation = 0f;
        ApplyLocalPlayerView();
    }

    private int GetNextBotNumber()
    {
        int count = 0;
        for (int i = 0; i < players.Count; i++)
        {
            if (!players[i].IsLocal)
            {
                count += 1;
            }
        }

        return count + 1;
    }

    private void BuildInterface()
    {
        CreateCanvas();

        Font font = scene.BuiltInFont();
        Sprite circleSprite = scene.CreateCircleSprite(128, Color.white, Color.white, 0);

        GameObject background = scene.CreateUIObject("Background", root);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        scene.Stretch(backgroundRect);
        Image backgroundImage = background.AddComponent<Image>();
        if (backgroundSprite != null)
        {
            backgroundImage.sprite = backgroundSprite;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.color = Color.white;
            backgroundImage.preserveAspect = false;
        }
        else
        {
            backgroundImage.color = scene.Hex("#0a0a1a");
        }

        // Quando a SceneBridge existe, o cenário Unity já provê o fundo visual.
        if (sceneBridge != null)
        {
            backgroundImage.color = Color.clear;
            backgroundImage.sprite = null;
        }

        GameObject tableObject = scene.CreateUIObject("Table", root);
        table = tableObject.GetComponent<RectTransform>();
        table.sizeDelta = new Vector2(520f, 520f);
        table.anchoredPosition = new Vector2(0f, -80f);

        GameObject centerTextObject = scene.CreateUIObject("CenterStatus", table);
        RectTransform centerTextRect = centerTextObject.GetComponent<RectTransform>();
        centerTextRect.sizeDelta = new Vector2(420f, 120f);
        centerTextRect.anchoredPosition = Vector2.zero;

        if (sceneBridge == null)
            CreateArrowView();

        // Seats procedurais ficam num container oculto; quando a cena
        // já tem PlayerUI nos Personagens, servem apenas de fallback.
        GameObject seatsObject = scene.CreateUIObject("Seats", root);
        seatsLayer = seatsObject.GetComponent<RectTransform>();
        scene.Stretch(seatsLayer);
        if (sceneBridge != null)
            seatsLayer.gameObject.SetActive(false);

        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            HotPotatoSeatView view = sceneBridge != null ? FindSeatViewFromScene(i, font) : null;
            if (view == null)
                view = CreateSeatView("Seat " + (i + 1), seatsLayer, circleSprite, font, i);
            seatViews.Add(view);
        }

        questionPanel.Initialize(this, root, font, quadroSprite);

        GameObject timerTextObject = scene.CreateUIObject("TimerText", table);
        RectTransform timerTextRect = timerTextObject.GetComponent<RectTransform>();
        timerTextRect.anchoredPosition = new Vector2(-120f, 80f);
        timerTextRect.sizeDelta = new Vector2(120f, 60f);
        timerText = scene.CreateText(timerTextObject, "30", 48, font, TextAnchor.MiddleCenter, Color.white);
        timerText.fontStyle = FontStyle.Bold;
        turnTimer.Initialize(null, timerText);

        GameObject buttonObject = scene.CreateButton("AddBotButton", root, "Adicionar Bot", font, AddBot);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 100f);
        buttonRect.sizeDelta = new Vector2(230f, 52f);
        addBotButton = buttonObject.GetComponent<Button>();

        // Dois slots de carta no canto inferior direito, um em cima do outro
        CreateCardSlot(root, 0, new Vector2(-100f, 70f), ref useCardButton1, ref useCardIconImage1);
        CreateCardSlot(root, 1, new Vector2(-100f, -50f), ref useCardButton2, ref useCardIconImage2);

        GameObject statusObject = scene.CreateUIObject("Status", root);
        RectTransform statusRect = statusObject.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0f);
        statusRect.anchorMax = new Vector2(0.5f, 0f);
        statusRect.pivot = new Vector2(0.5f, 0f);
        statusRect.anchoredPosition = new Vector2(0f, 162f);
        statusRect.sizeDelta = new Vector2(760f, 38f);
        
        GameObject lastAnswerObject = scene.CreateUIObject("LastAnswer", root);
        RectTransform lastAnswerRect = lastAnswerObject.GetComponent<RectTransform>();
        lastAnswerRect.anchorMin = new Vector2(0f, 0f);
        lastAnswerRect.anchorMax = new Vector2(0f, 0f);
        lastAnswerRect.pivot = new Vector2(0f, 0f);
        lastAnswerRect.anchoredPosition = new Vector2(28f, 24f);
        lastAnswerRect.sizeDelta = new Vector2(320f, 80f);

        GameObject lastAnswerTextObject = scene.CreateUIObject("Text", lastAnswerObject.transform);
        RectTransform lastAnswerTextRect = lastAnswerTextObject.GetComponent<RectTransform>();
        scene.Stretch(lastAnswerTextRect);
        lastAnswerText = scene.CreateText(lastAnswerTextObject, "", 22, font, TextAnchor.MiddleLeft, scene.Hex("#ffffff"));
        lastAnswerText.fontStyle = FontStyle.Bold;
        lastAnswerText.verticalOverflow = VerticalWrapMode.Overflow;

        // Banner morte subita
        GameObject sdObject = scene.CreateUIObject("SuddenDeathBanner", root);
        RectTransform sdRect = sdObject.GetComponent<RectTransform>();
        sdRect.anchorMin = new Vector2(0.5f, 0.5f);
        sdRect.anchorMax = new Vector2(0.5f, 0.5f);
        sdRect.pivot = new Vector2(0.5f, 0.5f);
        sdRect.anchoredPosition = new Vector2(0f, 180f);
        sdRect.sizeDelta = new Vector2(620f, 80f);
        suddenDeathText = scene.CreateText(sdObject, "MORTE S\u00daBITA", 54, font, TextAnchor.MiddleCenter, scene.Hex("#ff274f"));
        suddenDeathText.fontStyle = FontStyle.Bold;
        sdObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.85f);
        sdObject.SetActive(false);

        // Container para o lobby text (visível na fase Lobby)
        GameObject lobbyTopObject = scene.CreateUIObject("LobbyTop", root);
        RectTransform lobbyTopRect = lobbyTopObject.GetComponent<RectTransform>();
        lobbyTopRect.anchorMin = new Vector2(0.5f, 1f);
        lobbyTopRect.anchorMax = new Vector2(0.5f, 1f);
        lobbyTopRect.pivot = new Vector2(0.5f, 1f);
        lobbyTopRect.anchoredPosition = new Vector2(0f, -80f);
        lobbyTopRect.sizeDelta = new Vector2(800f, 80f);
        
        lobbyText = scene.CreateText(lobbyTopObject, "", 28, font, TextAnchor.MiddleCenter, Color.white);
        lobbyText.fontStyle = FontStyle.Bold;
        this.lobbyTopObject = lobbyTopObject;  // Salva referência para controlar visibilidade

        // Countdown de início no topo (visível durante countdown)
        GameObject countdownTopObject = scene.CreateUIObject("StartCountdown", root);
        RectTransform countdownTopRect = countdownTopObject.GetComponent<RectTransform>();
        countdownTopRect.anchorMin = new Vector2(0.5f, 1f);
        countdownTopRect.anchorMax = new Vector2(0.5f, 1f);
        countdownTopRect.pivot = new Vector2(0.5f, 1f);
        countdownTopRect.anchoredPosition = new Vector2(0f, -80f);
        countdownTopRect.sizeDelta = new Vector2(800f, 140f);
        
        startCountdownText = scene.CreateText(countdownTopObject, "", 82, font, TextAnchor.MiddleCenter, scene.Hex("#ffdf4d"));
        startCountdownText.fontStyle = FontStyle.Bold;
        
        Shadow countdownShadow = countdownTopObject.AddComponent<Shadow>();
        countdownShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        countdownShadow.effectDistance = new Vector2(2f, -2f);
        countdownTopObject.SetActive(false);
        this.countdownTopObject = countdownTopObject;  // Salva referência para controlar visibilidade

        cardSystem.Initialize(root, font);
        opChoiceSystem.Initialize(root, font);
        resultPanel.Initialize(root, font, ResetToLobby);
        ApplyLocalPlayerView();
    }

    private void CreateArrowView()
    {
        GameObject arrowObject = scene.CreateUIObject("Arrow", table);
        arrowPivot = arrowObject.GetComponent<RectTransform>();
        arrowPivot.sizeDelta = new Vector2(120f, 120f);
        arrowPivot.anchoredPosition = new Vector2(-15f, 10f);

        Image arrowImage = arrowObject.AddComponent<Image>();
        if (arrowSprite != null)
        {
            arrowImage.sprite = arrowSprite;
            arrowImage.color = Color.white;
            arrowImage.preserveAspect = true;
        }
        else
        {
            // fallback: desenha seta simples
            Sprite headSprite = scene.CreateTriangleSprite(96, Color.white);
            arrowImage.sprite = headSprite;
            arrowImage.color = scene.Hex("#ffdf4d");
        }

        // DERRAUNDE — batata sobre a mesa, movida para o jogador da vez
        GameObject derraUndeObject = scene.CreateUIObject("DerraUnde", table);
        derraUndePivot = derraUndeObject.GetComponent<RectTransform>();
        derraUndePivot.sizeDelta = new Vector2(90f, 90f);
        derraUndePivot.anchoredPosition = new Vector2(-460f, -280f);
        derraUndeImage = derraUndeObject.AddComponent<Image>();
        derraUndeImage.preserveAspect = true;
        if (derraUndeSprite != null)
            derraUndeImage.sprite = derraUndeSprite;
        derraUndeImage.color = derraUndeSprite != null ? Color.white : Color.clear;
    }

    private void ApplyLocalPlayerView()
    {
        if (!localPlayerView)
        {
            return;
        }

        if (seatsLayer != null)
        {
            seatsLayer.gameObject.SetActive(false);
        }

        if (arrowPivot != null)
        {
            arrowPivot.gameObject.SetActive(false);
        }

        if (lobbyText != null)
        {
            lobbyText.gameObject.SetActive(false);
        }

        if (lastAnswerText != null)
        {
            lastAnswerText.gameObject.SetActive(false);
        }

        if (addBotButton != null)
        {
            addBotButton.gameObject.SetActive(false);
        }

        if (useCardButton1 != null)
        {
            useCardButton1.gameObject.SetActive(false);
        }

        if (useCardButton2 != null)
        {
            useCardButton2.gameObject.SetActive(false);
        }
    }

    private HotPotatoSeatView CreateSeatView(string name, RectTransform parent, Sprite circleSprite, Font font, int seatIndex)
    {
        GameObject seatObject = scene.CreateUIObject(name, parent);
        RectTransform seatRect = seatObject.GetComponent<RectTransform>();
        seatRect.sizeDelta = new Vector2(150f, 130f);

        CanvasGroup group = seatObject.AddComponent<CanvasGroup>();

        Image background = seatObject.AddComponent<Image>();
        background.sprite = scene.CreateRoundedRectSprite(160, 110, 14, Color.white, Color.white, 0);
        background.type = Image.Type.Sliced;
        background.color = Color.clear;

        Button seatButton = seatObject.AddComponent<Button>();
        seatButton.targetGraphic = background;
        seatButton.transition = Selectable.Transition.None;
        int capturedSeatIndex = seatIndex;
        seatButton.onClick.AddListener(delegate { OnSeatClicked(capturedSeatIndex); });

        // Lamp (indicador ativo) — topo do seat
        GameObject lampObject = scene.CreateUIObject("Lamp", seatRect);
        RectTransform lampRect = lampObject.GetComponent<RectTransform>();
        lampRect.sizeDelta = new Vector2(20f, 20f);
        lampRect.anchoredPosition = new Vector2(0f, 72f);
        Image lampImage = lampObject.AddComponent<Image>();
        lampImage.sprite = circleSprite;
        lampImage.color = Color.clear;
        Shadow lampGlow = lampObject.AddComponent<Shadow>();
        lampGlow.effectColor = Color.white;
        lampGlow.effectDistance = new Vector2(0f, 0f);

        // Vidas — acima do avatar
        GameObject livesImageObject = scene.CreateUIObject("LivesImage", seatRect);
        RectTransform livesImageRect = livesImageObject.GetComponent<RectTransform>();
        livesImageRect.sizeDelta = new Vector2(120f, 38f);
        livesImageRect.anchoredPosition = new Vector2(0f, 40f);
        Image livesImage = livesImageObject.AddComponent<Image>();
        livesImage.preserveAspect = true;

        GameObject livesObject = scene.CreateUIObject("Lives", seatRect);
        RectTransform livesRect = livesObject.GetComponent<RectTransform>();
        livesRect.sizeDelta = new Vector2(120f, 24f);
        livesRect.anchoredPosition = new Vector2(0f, 40f);
        Text livesText = scene.CreateText(livesObject, "", 14, font, TextAnchor.MiddleCenter, scene.Hex("#ff5d7a"));
        livesText.gameObject.SetActive(false);

        // Avatar — centro
        GameObject avatarObject = scene.CreateUIObject("Avatar", seatRect);
        RectTransform avatarRect = avatarObject.GetComponent<RectTransform>();
        avatarRect.sizeDelta = new Vector2(70f, 70f);
        avatarRect.anchoredPosition = new Vector2(0f, -4f);
        Image avatar = avatarObject.AddComponent<Image>();
        avatar.sprite = circleSprite;
        avatar.color = new Color(0.15f, 0.75f, 1f, 0.95f);

        // Nome — abaixo do avatar
        GameObject nameObject = scene.CreateUIObject("Name", seatRect);
        RectTransform nameRect = nameObject.GetComponent<RectTransform>();
        nameRect.sizeDelta = new Vector2(140f, 26f);
        nameRect.anchoredPosition = new Vector2(0f, -48f);
        Text nameText = scene.CreateText(nameObject, "", 16, font, TextAnchor.MiddleCenter, Color.white);
        nameText.fontStyle = FontStyle.Bold;

        // Carta — abaixo do nome
        GameObject cardObject = scene.CreateUIObject("Cards", seatRect);
        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(110f, 20f);
        cardRect.anchoredPosition = new Vector2(0f, -68f);
        Text cardText = scene.CreateText(cardObject, "", 12, font, TextAnchor.MiddleCenter, scene.Hex("#ffdf4d"));

        GameObject cardImageObject = scene.CreateUIObject("CardImage", cardObject.transform);
        RectTransform cardImageRect = cardImageObject.GetComponent<RectTransform>();
        scene.Stretch(cardImageRect);
        Image cardImage = cardImageObject.AddComponent<Image>();
        cardImage.preserveAspect = true;

        // Mini-card lateral — direita para seats 0-3, esquerda para 4-7
        // tamanho ~metade do avatar (35x35), posicionado ao lado do centro
        float sideX = seatIndex < 4 ? 62f : -62f;
        GameObject sideCardObject = scene.CreateUIObject("SideCard", seatRect);
        RectTransform sideCardRect = sideCardObject.GetComponent<RectTransform>();
        sideCardRect.sizeDelta = new Vector2(38f, 38f);
        sideCardRect.anchoredPosition = new Vector2(sideX, -4f);
        Image sideCardImage = sideCardObject.AddComponent<Image>();
        sideCardImage.preserveAspect = true;
        sideCardImage.raycastTarget = true;  // habilitado para hover
        sideCardObject.SetActive(false);

        // Tooltip ao passar o mouse sobre a carta de outro jogador
        SeatCardTooltip sideCardTooltip = sideCardObject.AddComponent<SeatCardTooltip>();
        sideCardTooltip.SetRoot(root);
        sideCardTooltip.SetFont(font);

        // Light overlay
        GameObject overlayObject = scene.CreateUIObject("LightOverlay", seatRect);
        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        scene.Stretch(overlayRect);
        Image lightOverlay = overlayObject.AddComponent<Image>();
        lightOverlay.sprite = scene.CreateRoundedRectSprite(160, 110, 14, Color.white, Color.white, 0);
        lightOverlay.type = Image.Type.Sliced;
        lightOverlay.color = Color.clear;
        lightOverlay.raycastTarget = false;

        // Badge NP3
        GameObject badgeObject = scene.CreateUIObject("NP3", seatRect);
        RectTransform badgeRect = badgeObject.GetComponent<RectTransform>();
        badgeRect.sizeDelta = new Vector2(96f, 40f);
        badgeRect.anchoredPosition = new Vector2(0f, 2f);
        Text badgeText = scene.CreateText(badgeObject, "NP3", 30, font, TextAnchor.MiddleCenter, scene.Hex("#ff1744"));
        badgeText.fontStyle = FontStyle.Bold;
        badgeText.gameObject.SetActive(false);

        return new HotPotatoSeatView
        {
            Root = seatRect,
            Group = group,
            SlotBackground = background,
            Avatar = avatar,
            Lamp = lampImage,
            LightOverlay = lightOverlay,
            SeatButton = seatButton,
            NameText = nameText,
            LivesText = livesText,
            LivesImage = livesImage,
            CardText = cardText,
            CardImage = cardImage,
            BadgeText = badgeText
            ,
            SideCardImage = sideCardImage,
            Life0 = life0Sprite,
            Life1 = life1Sprite,
            Life2 = life2Sprite,
            LifeFull = lifeFullSprite,
            CardNP3 = cardNp3Sprite,
            CardAtestado = cardAtestadoSprite,
            CardMonster = cardMonsterSprite,
            CardGPT = cardGptSprite
        };
    }

    /// Localiza o PlayerUI que é filho do Personagem indicado e monta um
    /// HotPotatoSeatView conectado a ele. Autoatribui os sprites de vida e carta
    /// que já foram carregados via Resources no Start(). Retorna null se o
    /// Personagem não tiver um Canvas filho (PlayerUI não instanciado).
    private HotPotatoSeatView FindSeatViewFromScene(int index, Font font)
    {
        Transform character = sceneBridge?.GetCharacterTransform(index);
        if (character == null) return null;

        // O PlayerUI é o único Canvas filho do Personagem
        Canvas uiCanvas = character.GetComponentInChildren<Canvas>(true);
        if (uiCanvas == null) return null;
        Transform ui = uiCanvas.transform;

        HotPotatoSeatView view = new HotPotatoSeatView
        {
            Root          = ui.GetComponent<RectTransform>(),
            Group         = ui.GetComponent<CanvasGroup>(),
            // Avatar, Lamp, SlotBackground e LightOverlay são nulos
            // intencionalmente: o sprite do Personagem e a Light2D cobrem essas funções.
            NameText      = FindChildText(ui, "Name"),
            LivesImage    = FindChildImage(ui, "LivesImage"),
            LivesText     = FindChildText(ui, "Lives"),
            CardText      = FindChildText(ui, "Cards"),
            CardImage     = FindChildImage(ui, "CardImage"),
            BadgeText     = FindChildText(ui, "Badge"),
            // Auto-preenchimento dos sprites carregados de Resources
            Life0         = life0Sprite,
            Life1         = life1Sprite,
            Life2         = life2Sprite,
            LifeFull      = lifeFullSprite,
            CardNP3       = cardNp3Sprite,
            CardAtestado  = cardAtestadoSprite,
            CardMonster   = cardMonsterSprite,
            CardGPT       = cardGptSprite
        };

        return view;
    }

    private static Text FindChildText(Transform parent, string childName)
    {
        Transform t = parent.Find(childName);
        return t != null ? t.GetComponent<Text>() : null;
    }

    private static Image FindChildImage(Transform parent, string childName)
    {
        Transform t = parent.Find(childName);
        return t != null ? t.GetComponent<Image>() : null;
    }

    private void CreateCanvas()
    {
        Canvas existingCanvas = FindObjectOfType<Canvas>();
        if (existingCanvas != null)
        {
            canvas = existingCanvas;
        }
        else
        {
            GameObject canvasObject = new GameObject("Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<GraphicRaycaster>();
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        root = canvas.GetComponent<RectTransform>();

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }

    private void RefreshSeats(bool lobbyLayout)
    {
        int circleCount = lobbyLayout ? MAX_PLAYERS : Mathf.Max(players.Count, 1);
        float radiusX = lobbyLayout ? 285f : Mathf.Lerp(180f, 275f, Mathf.InverseLerp(2f, MAX_PLAYERS, players.Count));
        float radiusY = lobbyLayout ? 255f : Mathf.Lerp(160f, 245f, Mathf.InverseLerp(2f, MAX_PLAYERS, players.Count));
        Vector2 center = new Vector2(-20f, -80f);

        bool useSceneUI = sceneBridge != null;

        for (int i = 0; i < seatViews.Count; i++)
        {
            HotPotatoSeatView view = seatViews[i];
            HotPotatoPlayer player = i < players.Count ? players[i] : null;

            if (useSceneUI)
            {
                // Visibilidade do Personagem já é gerida por SceneBridge.SetPlayerCount.
                // Aqui só mostramos/ocultamos o PlayerUI overlay.
                bool hasPlayer = i < players.Count;
                if (view.Root != null)
                    view.Root.gameObject.SetActive(hasPlayer);
                if (!hasPlayer) continue;
            }
            else
            {
                bool showEliminated = lobbyLayout || player == null || !player.Eliminated;
                bool visible = lobbyLayout || (i < players.Count && showEliminated);
                if (view.Root != null) view.Root.gameObject.SetActive(visible);
                if (!visible) continue;

                int layoutIndex = lobbyLayout ? i : Mathf.Min(i, circleCount - 1);
                float angle = 90f - (360f / circleCount) * layoutIndex;
                float radians = angle * Mathf.Deg2Rad;
                if (view.Root != null)
                    view.Root.anchoredPosition = center + new Vector2(Mathf.Cos(radians) * radiusX, Mathf.Sin(radians) * radiusY);
            }

            view.SetPlayer(player, i + 1);
            view.SetActive(false);
            view.SetBadge(player != null && player.Eliminated, "DP");
        }
    }

    private void UpdateLobbyText()
    {
        if (lobbyText == null || addBotButton == null)
        {
            return;
        }

        addBotButton.interactable = players.Count < MAX_PLAYERS;

        if (players.Count < MIN_PLAYERS)
        {
            lobbyText.text = "Aguardando jogadores...\n" + players.Count + "/" + MAX_PLAYERS;
            return;
        }

        int seconds = Mathf.CeilToInt(Mathf.Max(0f, lobbyTimer));
        lobbyText.text = "Iniciando em " + seconds + "s\n" + players.Count + "/" + MAX_PLAYERS;
    }

    private void StartGameCountdown()
    {
        if (phase != HotPotatoPhase.Lobby)
        {
            return;
        }

        phase = HotPotatoPhase.LobbyCountdown;
        if (addBotButton != null)
        {
            addBotButton.gameObject.SetActive(false);
        }
        if (lobbyTopObject != null)
        {
            lobbyTopObject.SetActive(false);
        }
        if (countdownTopObject != null)
        {
            countdownTopObject.SetActive(true);
        }
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        RefreshSeats(true);

        // Mostra countdown no topo da tela (onde o quadro verde ainda não está visível)
        if (lobbyText != null)
        {
            lobbyText.text = "";
        }
        if (startCountdownText != null)
        {
            startCountdownText.gameObject.SetActive(true);
        }

        for (int i = LOBBY_COUNTDOWN; i > 0; i--)
        {
            if (startCountdownText != null) startCountdownText.text = i + "...";
            yield return new WaitForSeconds(1f);
        }

        if (startCountdownText != null) startCountdownText.text = "Come\u00e7ar!";
        yield return new WaitForSeconds(0.45f);

        if (countdownTopObject != null) countdownTopObject.SetActive(false);
        if (startCountdownText != null) startCountdownText.gameObject.SetActive(false);
        BeginGame();
    }

    private void BeginGame()
    {
        phase = HotPotatoPhase.Playing;
        botsAnswerAutomatically = true;
        musicSystem.StartMusic();
        if (addBotButton != null)
        {
            addBotButton.gameObject.SetActive(false);
        }
        if (lobbyText != null)
        {
            lobbyText.text = "";
        }
        currentPlayerIndex = -1;
        forcedNextPlayerIndex = Random.Range(0, players.Count);
        RefreshSeats(false);
        UpdateUseCardButtons();
        StartNextTurn();
    }

    private void StartNextTurn()
    {
        if (AliveCount() <= 1)
        {
            EndGame();
            return;
        }

        StopBotRoutine();

        if (turnRoutine != null)
        {
            StopCoroutine(turnRoutine);
        }

        turnRoutine = StartCoroutine(StartTurnRoutine());
    }

    private IEnumerator StartTurnRoutine()
    {
        resolvingTurn = true;
        selectingAtestadoTarget = false;
        SetTargetSelection(false);
        UpdateUseCardButtons();
        questionPanel.Hide();
        turnTimer.StopTimer();
        musicSystem.SetSpeed(1f);  // restaura velocidade da música (pode ter sido afetada pelo Monster)
        RefreshSeats(false);

        if (forcedNextPlayerIndex >= 0 && forcedNextPlayerIndex < players.Count && players[forcedNextPlayerIndex].Alive)
        {
            currentPlayerIndex = forcedNextPlayerIndex;
            forcedNextPlayerIndex = -1;
        }
        else
        {
            forcedNextPlayerIndex = -1;
            currentPlayerIndex = turnSystem.GetNextAliveIndex(players, currentPlayerIndex);
        }

        if (currentPlayerIndex < 0)
        {
            EndGame();
            yield break;
        }

        HotPotatoPlayer player = players[currentPlayerIndex];

        yield return AnimateArrowToPlayer(currentPlayerIndex, 0.55f);
        RefreshActivePlayerView();

        PrepareQuestionForTurn();
        questionPanel.ShowQuestion(player.Name, currentQuestionText, player.IsLocal);
        turnTimer.StartTimer(currentTimerMax);
        resolvingTurn = false;
        UpdateUseCardButtons();

        if (botsAnswerAutomatically && !player.IsLocal)
        {
            ScheduleBotAnswer(0f);
        }
    }

    private IEnumerator AnimateArrowToPlayer(int playerIndex, float duration)
    {
        // Modo scene-driven: sem seta UI — aguarda metade do tempo e aciona a luz.
        if (arrowPivot == null)
        {
            yield return new WaitForSeconds(duration * 0.5f);
            sceneBridge?.SetActivePlayer(playerIndex);
            yield break;
        }

        float target = PlayerRotation(playerIndex);
        float start = currentArrowRotation;
        float extraSpin = 360f;
        float elapsed = 0f;

        Vector2 targetPos = PlayerPositionOnTable(playerIndex);
        Vector2 startPos = derraUndePivot != null ? derraUndePivot.anchoredPosition : Vector2.zero;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            currentArrowRotation = Mathf.Lerp(start, target + extraSpin, eased);
            arrowPivot.localRotation = Quaternion.Euler(0f, 0f, currentArrowRotation);
            if (derraUndePivot != null)
                derraUndePivot.anchoredPosition = Vector2.Lerp(startPos, targetPos, eased);
            yield return null;
        }

        currentArrowRotation = Mathf.Repeat(target, 360f);
        arrowPivot.localRotation = Quaternion.Euler(0f, 0f, currentArrowRotation);
        if (derraUndePivot != null)
            derraUndePivot.anchoredPosition = targetPos;
        sceneBridge?.SetActivePlayer(playerIndex);
    }

    private Vector2 PlayerPositionOnTable(int playerIndex)
    {
        int circleCount = Mathf.Max(players.Count, 1);
        float radius = Mathf.Lerp(250f, 385f, Mathf.InverseLerp(2f, MAX_PLAYERS, players.Count)) * 0.45f;
        float angle = 90f - (360f / circleCount) * playerIndex;
        float rad = angle * Mathf.Deg2Rad;
        Vector2 center = new Vector2(0f, -80f) - table.anchoredPosition;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
    }

    private float PlayerRotation(int playerIndex)
    {
        int circleCount = Mathf.Max(players.Count, 1);
        float angle = 90f - (360f / circleCount) * playerIndex;
        return angle - 90f;
    }

    private void ScheduleBotAnswer(float extraDelay)
    {
        StopBotRoutine();
        botRoutine = StartCoroutine(BotAnswerRoutine(extraDelay));
    }

    private IEnumerator BotAnswerRoutine(float extraDelay)
    {
        float wait = Mathf.Max(0f, botAnswerDelaySeconds) + Mathf.Max(0f, extraDelay);
        yield return new WaitForSeconds(wait);

        if (phase != HotPotatoPhase.Playing || resolvingTurn || currentPlayerIndex < 0)
        {
            yield break;
        }

        if (players[currentPlayerIndex].IsLocal)
        {
            yield break;
        }

        string answer = FormatAnswerValue(currentAnswerValue);
        questionPanel.SetInputText(answer);
        yield return new WaitForSeconds(0.25f);
        botRoutine = null;
        SubmitAnswer(answer);
    }

    private void RefreshActivePlayerView()
    {
        for (int i = 0; i < players.Count; i++)
        {
            HotPotatoPlayer player = players[i];
            player.View.SetPlayer(player, i + 1);
            player.View.SetActive(i == currentPlayerIndex && player.Alive);
            player.View.SetLamp(Color.clear, false);
            player.View.SetBadge(player.Eliminated, "DP");
        }

        UpdateActiveLamp();
        UpdateUseCardButtons();
    }

    private void UpdateActiveLamp()
    {
        if (currentPlayerIndex < 0 || currentPlayerIndex >= players.Count)
        {
            return;
        }

        for (int i = 0; i < players.Count; i++)
        {
            if (i != currentPlayerIndex)
            {
                players[i].View.SetLamp(Color.clear, false);
            }
        }

        HotPotatoPlayer active = players[currentPlayerIndex];
        if (!active.Alive)
        {
            active.View.SetLamp(Color.clear, false);
            return;
        }

        active.View.SetLamp(turnTimer.CurrentLightColor(), turnTimer.ShouldBlink);
    }

    private void PassTurnSoon(float delay)
    {
        if (resolvingTurn)
        {
            return;
        }

        resolvingTurn = true;
        selectingAtestadoTarget = false;
        SetTargetSelection(false);
        UpdateUseCardButtons();
        StopBotRoutine();
        StartCoroutine(PassTurnRoutine(delay));
    }

    private IEnumerator PassTurnRoutine(float delay)
    {
        turnTimer.StopTimer();
        UpdateUseCardButtons();
        yield return new WaitForSeconds(delay);
        StartNextTurn();
    }

    private void ResolveTimeout()
    {
        if (resolvingTurn || currentPlayerIndex < 0)
        {
            return;
        }

        resolvingTurn = true;
        selectingAtestadoTarget = false;
        SetTargetSelection(false);
        UpdateUseCardButtons();
        StopBotRoutine();
        StartCoroutine(TimeoutRoutine());
    }

    private IEnumerator TimeoutRoutine()
    {
        HotPotatoPlayer player = players[currentPlayerIndex];
        turnTimer.StopTimer();
        player.View.SetLamp(Color.clear, false);
        questionPanel.SetInteractable(false);

        lastAnswerCorrect = false;
        consecutiveWrong = 0;

        // Perde vida ao acabar o tempo sem responder
        bool wasSuddenDeath = suddenDeathActive;
        ApplyLifeLoss(player);
        RefreshPlayerAfterLifeLoss(player);

        // Se estava em morte súbita, resetar a expressão para começar fácil de novo
        if (wasSuddenDeath)
        {
            resetExpressionNext = true;
        }

        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());

        yield return new WaitForSeconds(1.15f);

        if (AliveCount() <= 1)
            EndGame();
        else
            StartNextTurn();
    }

    private int AliveCount()
    {
        int count = 0;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].Alive)
            {
                count++;
            }
        }

        return count;
    }

    private HotPotatoPlayer Winner()
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].Alive)
            {
                return players[i];
            }
        }

        return null;
    }

    private void EndGame()
    {
        phase = HotPotatoPhase.GameOver;
        StopRunningCoroutines();
        selectingAtestadoTarget = false;
        SetTargetSelection(false);
        cardSystem.HideChoice();
        turnTimer.StopTimer();
        // Keep question visible for all players.
        useCardButton1.gameObject.SetActive(false);
        useCardButton2.gameObject.SetActive(false);
        musicSystem.StopMusic();
        sceneBridge?.SetActivePlayer(-1);
        RefreshSeats(false);

        HotPotatoPlayer winner = Winner();
        string winnerName = winner != null ? winner.Name : "Ningu\u00e9m";
        resultPanel.Show(winnerName);
    }

    private void StopRunningCoroutines()
    {
        StopBotRoutine();

        if (cardChoiceAutoRoutine != null)
        {
            StopCoroutine(cardChoiceAutoRoutine);
            cardChoiceAutoRoutine = null;
        }

        if (turnRoutine != null)
        {
            StopCoroutine(turnRoutine);
            turnRoutine = null;
        }
    }

    private void StopBotRoutine()
    {
        if (botRoutine != null)
        {
            StopCoroutine(botRoutine);
            botRoutine = null;
        }
    }
}
