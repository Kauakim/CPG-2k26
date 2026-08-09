using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// Gerencia o painel de escolha de carta e a animação de uso.
/// Nenhum elemento é criado em runtime — tudo pré-construído na hierarquia da cena.
public class HPCardView : MonoBehaviour
{
    [Header("Painel de escolha (filho da Canvas, inativo por padrão)")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Text       choiceTitleText;
    [SerializeField] private Button     option1Button;
    [SerializeField] private Button     option2Button;

    [Header("Backgrounds dos botões de escolha (um por slot)")]
    [SerializeField] private Image option1Background;
    [SerializeField] private Image option2Background;

    [Header("Sprites de background — um por tipo de carta")]
    [SerializeField] private Sprite bgNP3;
    [SerializeField] private Sprite bgAtestado;
    [SerializeField] private Sprite bgMonster;
    [SerializeField] private Sprite bgGPT;

    [Header("Overlay de animação (filho da Canvas, inativo por padrão)")]
    [SerializeField] private GameObject  animOverlay;
    [SerializeField] private CanvasGroup animGroup;
    [SerializeField] private Image       animImage;
    [SerializeField] private Image       flashImage;

    [Header("Frames de animação (sprites fatiados de sheet)")]
    [SerializeField] private Sprite[] monsterFrames;
    [SerializeField] private Sprite[] gptFrames;

    [Header("Notificação flutuante de carta (filho da Canvas, inativo por padrão)")]
    [SerializeField] private GameObject    floatingRoot;
    [SerializeField] private Text          floatingText;
    [SerializeField] private RectTransform floatingRect;
    [SerializeField] private CanvasGroup   floatingGroup;

    [Header("Posição e duração da notificação flutuante")]
    [SerializeField] private Vector2 floatingStartPos = new Vector2(0f, 118f);
    [SerializeField] private float   floatingRise     = 58f;
    [SerializeField] private float   floatingDuration = 1.45f;

    private Action<HPCardType> choiceCallback;

    private void Awake()
    {
        if (choicePanel  != null) choicePanel.SetActive(false);
        if (animOverlay  != null) animOverlay.SetActive(false);
        if (floatingRoot != null) floatingRoot.SetActive(false);
        if (flashImage   != null) flashImage.color = Color.clear;
    }

    // ── Escolha de carta ──────────────────────────────────────────────────────

    public void ShowChoice(string playerName, HPCardType first, HPCardType second,
                           Action<HPCardType> onChoose)
    {
        HideChoice();
        choiceCallback = onChoose;
        if (choicePanel == null) return;

        choicePanel.SetActive(true);
        if (choiceTitleText != null)
            choiceTitleText.text = playerName + " ganhou uma carta!";

        SetupOption(option1Button, option1Background, first);
        SetupOption(option2Button, option2Background, second);
    }

    public void HideChoice()
    {
        choiceCallback = null;
        if (choicePanel != null) choicePanel.SetActive(false);
    }

    private void SetupOption(Button btn, Image background, HPCardType cardType)
    {
        if (btn == null) return;

        if (background != null) background.sprite = GetBackgroundSprite(cardType);

        HPCardType captured = cardType;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => Choose(captured));
    }

    private void Choose(HPCardType cardType)
    {
        var cb = choiceCallback;
        HideChoice();
        cb?.Invoke(cardType);
    }

    // ── Animação de uso ───────────────────────────────────────────────────────

    public void PlayUseAnimation(HPCardType cardType)
    {
        if (cardType == HPCardType.None) return;
        StartCoroutine(UseAnimRoutine(cardType));
    }

    private IEnumerator UseAnimRoutine(HPCardType cardType)
    {
        Sprite[] frames = GetAnimFrames(cardType);
        Color    accent = HPCardInfo.FrameColor(cardType);

        if (animOverlay == null || animGroup == null || frames == null || frames.Length == 0)
        {
            yield return StartCoroutine(ColorFlashRoutine(accent));
            yield break;
        }

        animOverlay.SetActive(true);
        animGroup.alpha = 0f;

        const float fps       = 8f;
        const float holdExtra = 0.3f;
        float frameDuration   = 1f / fps;
        float totalDuration   = frames.Length * frameDuration + holdExtra;
        float timer = 0f, frameTimer = 0f;
        int fi = 0;
        if (animImage != null) animImage.sprite = frames[0];

        while (timer < totalDuration)
        {
            timer      += Time.deltaTime;
            frameTimer += Time.deltaTime;

            if (frameTimer >= frameDuration && fi < frames.Length - 1)
            {
                frameTimer -= frameDuration;
                fi++;
                if (animImage != null) animImage.sprite = frames[fi];
            }

            float p = Mathf.Clamp01(timer / totalDuration);

            if (flashImage != null)
            {
                float fa = p < 0.15f
                    ? Mathf.Lerp(0f, 0.25f, p / 0.15f)
                    : Mathf.Lerp(0.25f, 0f, (p - 0.15f) / 0.85f);
                flashImage.color = new Color(accent.r, accent.g, accent.b, fa);
            }

            animGroup.alpha = p < 0.12f
                ? Mathf.Lerp(0f, 1f, p / 0.12f)
                : p > 0.85f
                    ? Mathf.Lerp(1f, 0f, (p - 0.85f) / 0.15f)
                    : 1f;

            yield return null;
        }

        animOverlay.SetActive(false);
        if (flashImage != null) flashImage.color = Color.clear;
    }

    private IEnumerator ColorFlashRoutine(Color accent)
    {
        if (flashImage == null) yield break;

        float t = 0f;
        while (t < 0.14f)
        {
            t += Time.deltaTime;
            flashImage.color = new Color(accent.r, accent.g, accent.b,
                                          Mathf.Clamp01(t / 0.14f) * 0.28f);
            yield return null;
        }
        yield return new WaitForSeconds(0.52f);
        t = 0f;
        while (t < 0.28f)
        {
            t += Time.deltaTime;
            flashImage.color = new Color(accent.r, accent.g, accent.b,
                                          (1f - Mathf.Clamp01(t / 0.28f)) * 0.28f);
            yield return null;
        }
        flashImage.color = Color.clear;
    }

    // ── Notificação flutuante ─────────────────────────────────────────────────

    public void ShowFloatingCard(string playerName)
    {
        if (floatingRoot == null) return;
        StartCoroutine(FloatingCardRoutine(playerName));
    }

    private IEnumerator FloatingCardRoutine(string playerName)
    {
        if (floatingText  != null) floatingText.text = "\U0001f0cf Carta obtida! " + playerName;
        if (floatingRect  != null) floatingRect.anchoredPosition = floatingStartPos;
        if (floatingGroup != null) floatingGroup.alpha = 1f;
        if (floatingRoot  != null) floatingRoot.SetActive(true);

        float elapsed = 0f;
        while (elapsed < floatingDuration)
        {
            elapsed += Time.deltaTime;
            float p  = Mathf.Clamp01(elapsed / floatingDuration);
            if (floatingRect  != null)
                floatingRect.anchoredPosition = floatingStartPos + new Vector2(0f, p * floatingRise);
            if (floatingGroup != null)
                floatingGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, p);
            yield return null;
        }

        if (floatingRoot != null) floatingRoot.SetActive(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public Sprite GetBackgroundSprite(HPCardType t)
    {
        switch (t)
        {
            case HPCardType.NP3:      return bgNP3;
            case HPCardType.Atestado: return bgAtestado;
            case HPCardType.Monster:  return bgMonster;
            case HPCardType.GPT:      return bgGPT;
            default:                  return null;
        }
    }

    private Sprite[] GetAnimFrames(HPCardType t) =>
        t == HPCardType.GPT ? gptFrames : monsterFrames;
}