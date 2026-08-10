using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;

public class card : MonoBehaviour
{
    // ── Caminhos dos card-arts (imagem interna da carta) ─────────────────────
    private const string CARD_NP3_PATH      = "Images/Cards/NP3";
    private const string CARD_ATESTADO_PATH = "Images/Cards/atestado";
    private const string CARD_MONSTER_PATH  = "Images/Cards/MONSTER";
    private const string CARD_GPT_PATH      = "Images/Cards/GPT";

    // ── Caminhos das molduras físicas (assets CARTA_COR.png) ─────────────────
    // NP3 = VERMELHA  |  Atestado = AMARELA  |  Monster = VERDE  |  GPT = AZUL
    private const string CARTA_VERMELHA_PATH = "Images/Cards/CARTA_VERMELHA";
    private const string CARTA_AMARELA_PATH  = "Images/Cards/CARTA_AMARELA";
    private const string CARTA_VERDE_PATH    = "Images/Cards/CARTA_VERDE";
    private const string CARTA_AZUL_PATH     = "Images/Cards/CARTA_AZUL"; // pendente

    // ── Caminhos dos sprite-sheets de animação ────────────────────────────────
    // Coloque em: Resources/Images/Cards/Animations/
    // Cada arquivo deve estar fatiado (Sprite Mode = Multiple) no Unity.
    private const string ANIM_MONSTER_PATH = "Images/Cards/Animations/monsterAnimation";
    private const string ANIM_GPT_PATH     = "Images/Cards/Animations/GPTAnimation";

    private RectTransform root;
    private Font font;
    private GameObject choicePanel;
    private Action<HotPotatoCardType> choiceCallback;

    // Molduras carregadas
    private Sprite cartaVermelha;
    private Sprite cartaAmarela;
    private Sprite cartaVerde;
    private Sprite cartaAzul;

    // Frames dos sprite-sheets
    private Sprite[] monsterFrames;
    private Sprite[] gptFrames;

    // ── Inicialização ─────────────────────────────────────────────────────────

    public void Initialize(RectTransform canvasRoot, Font uiFont)
    {
        root = canvasRoot;
        font = uiFont;

        cartaVermelha = LoadSprite(CARTA_VERMELHA_PATH);
        cartaAmarela  = LoadSprite(CARTA_AMARELA_PATH);
        cartaVerde    = LoadSprite(CARTA_VERDE_PATH);
        cartaAzul     = LoadSprite(CARTA_AZUL_PATH);   // null até o asset existir

        // Resources.LoadAll retorna todos os sprites fatiados do sheet, em ordem
        monsterFrames = Resources.LoadAll<Sprite>(ANIM_MONSTER_PATH);
        gptFrames     = Resources.LoadAll<Sprite>(ANIM_GPT_PATH);
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite s = Resources.Load<Sprite>(path);
        if (s != null) return s;
        Texture2D t = Resources.Load<Texture2D>(path);
        if (t == null) return null;
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
    }

    // ── Cor de destaque por tipo ──────────────────────────────────────────────

    public static Color FrameColor(HotPotatoCardType cardType)
    {
        switch (cardType)
        {
            case HotPotatoCardType.NP3:      return scene.Hex("#FF274F"); // vermelho
            case HotPotatoCardType.Atestado: return scene.Hex("#FFD700"); // amarelo
            case HotPotatoCardType.GPT:      return scene.Hex("#26A7FF"); // azul
            case HotPotatoCardType.Monster:  return scene.Hex("#27E36F"); // verde
            default:                          return Color.white;
        }
    }

    private Sprite GetCartaSprite(HotPotatoCardType cardType)
    {
        switch (cardType)
        {
            case HotPotatoCardType.NP3:      return cartaVermelha;
            case HotPotatoCardType.Atestado: return cartaAmarela;
            case HotPotatoCardType.Monster:  return cartaVerde;
            case HotPotatoCardType.GPT:      return cartaAzul;
            default:                          return null;
        }
    }

    // NP3 e Atestado usam frames do Monster como placeholder
    private Sprite[] GetAnimFrames(HotPotatoCardType cardType)
    {
        switch (cardType)
        {
            case HotPotatoCardType.GPT: return gptFrames;
            default:                    return monsterFrames;
        }
    }

    // ── Painel de escolha de carta ────────────────────────────────────────────

    public void ShowChoice(string playerName,
                           HotPotatoCardType firstOption,
                           HotPotatoCardType secondOption,
                           Action<HotPotatoCardType> onChoose)
    {
        if (root == null || font == null) return;
        HideChoice();
        choiceCallback = onChoose;

        choicePanel = scene.CreateUIObject("CardChoicePanel", root);
        RectTransform panelRect = choicePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot     = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, -10f);
        panelRect.sizeDelta = new Vector2(620f, 260f);
        choicePanel.AddComponent<Image>().color = Color.clear;

        GameObject titleObject = scene.CreateUIObject("Title", choicePanel.transform);
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot     = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -18f);
        titleRect.sizeDelta = new Vector2(-36f, 46f);
        Text title = scene.CreateText(titleObject, playerName + " ganhou uma carta",
                                       24, font, TextAnchor.MiddleCenter, Color.white);
        title.fontStyle = FontStyle.Bold;

        CreateOptionButton(firstOption,  new Vector2(-148f, -38f));
        CreateOptionButton(secondOption, new Vector2( 148f, -38f));
    }

    public void ChooseCard(HotPotatoCardType cardType)
    {
        Action<HotPotatoCardType> cb = choiceCallback;
        HideChoice();
        if (cb != null) cb(cardType);
    }

    public void HideChoice()
    {
        choiceCallback = null;
        if (choicePanel != null) { Destroy(choicePanel); choicePanel = null; }
    }

    // ── Botão de opção individual ──────────────────────────────────────────────

    private void CreateOptionButton(HotPotatoCardType cardType, Vector2 position)
    {
        Color  accent      = FrameColor(cardType);

        GameObject buttonObject = scene.CreateButton(
            "Option" + HotPotatoCardInfo.DisplayName(cardType),
            choicePanel.transform, "", font,
            delegate { ChooseCard(cardType); });

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot     = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = new Vector2(300f, 200f);

        // Fundo simples com borda colorida, sem a moldura carta
        Image buttonImage = buttonObject.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.sprite = scene.CreateRoundedRectSprite(300, 200, 14,
                new Color(0.08f, 0.10f, 0.18f, 1f), accent, 4);
            buttonImage.type  = Image.Type.Sliced;
            buttonImage.color = Color.white;
        }

        // Arte interna da carta (NP3.png, atestado.png, etc.) - agora visível sem moldura
        GameObject cardImageObject = scene.CreateUIObject("CardImage", buttonObject.transform);
        RectTransform cardImageRect = cardImageObject.GetComponent<RectTransform>();
        cardImageRect.anchorMin = Vector2.zero;
        cardImageRect.anchorMax = Vector2.one;
        cardImageRect.offsetMin = new Vector2(12f, 12f);
        cardImageRect.offsetMax = new Vector2(-12f, -36f);
        Image cardImage = cardImageObject.AddComponent<Image>();
        cardImage.preserveAspect = true;
        cardImage.raycastTarget  = false;
        Sprite artSprite = LoadCardArtSprite(cardType);
        cardImage.sprite = artSprite;
        cardImage.color  = artSprite != null ? Color.white : Color.clear;

        // Nome da carta no topo do botão
        GameObject labelObject = scene.CreateUIObject("Label", buttonObject.transform);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot     = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -6f);
        labelRect.sizeDelta = new Vector2(0f, 30f);
        Text label = scene.CreateText(labelObject, HotPotatoCardInfo.DisplayName(cardType),
                                       18, font, TextAnchor.MiddleCenter, Color.white);
        label.fontStyle = FontStyle.Bold;

        // Remove o texto vazio gerado pelo CreateButton
        Text buttonText = buttonObject.GetComponentInChildren<Text>();
        if (buttonText != null) buttonText.text = "";
    }

    private Sprite LoadCardArtSprite(HotPotatoCardType cardType)
    {
        string path = null;
        switch (cardType)
        {
            case HotPotatoCardType.NP3:      path = CARD_NP3_PATH;      break;
            case HotPotatoCardType.Atestado: path = CARD_ATESTADO_PATH; break;
            case HotPotatoCardType.Monster:  path = CARD_MONSTER_PATH;  break;
            case HotPotatoCardType.GPT:      path = CARD_GPT_PATH;      break;
        }
        return string.IsNullOrEmpty(path) ? null : LoadSprite(path);
    }

    // ── Floating card (carta obtida) ──────────────────────────────────────────

    public void ShowFloatingCard(string playerName)
    {
        if (root == null || font == null) return;
        StartCoroutine(FloatingCardRoutine(playerName));
    }

    private IEnumerator FloatingCardRoutine(string playerName)
    {
        GameObject cardObject = scene.CreateUIObject("FloatingCard", root);
        RectTransform rect = cardObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 118f);
        rect.sizeDelta = new Vector2(420f, 56f);
        Text text = scene.CreateText(cardObject, "\ud83c\udccf Carta obtida! " + playerName,
                                      28, font, TextAnchor.MiddleCenter, scene.Hex("#ffdf4d"));
        text.fontStyle = FontStyle.Bold;

        CanvasGroup group = cardObject.AddComponent<CanvasGroup>();
        float elapsed = 0f;
        const float duration = 1.45f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rect.anchoredPosition = new Vector2(0f, 118f + t * 58f);
            group.alpha = 1f - Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }
        Destroy(cardObject);
    }

    // ── Animação de uso de carta com sprite-sheet ─────────────────────────────
    //
    // Monster → frames do MONSTER sheet + audio monster.mp3
    // GPT     → frames do GPT sheet + audio gepeto.mp3
    // NP3     → frames do MONSTER como placeholder + audio saliva.mp3
    // Atestado→ frames do MONSTER como placeholder + audio tosse.mp3
    //
    // Quando os sheets de NP3/Atestado ficarem prontos, coloque em:
    //   Resources/Images/Cards/Animations/NP3    (e ajuste ANIM_NP3_PATH)
    //   Resources/Images/Cards/Animations/ATESTADO
    // e carregue em Initialize() — sem alterar mais nada.

    public void PlayUseAnimation(HotPotatoCardType cardType)
    {
        if (root == null || cardType == HotPotatoCardType.None) return;
        StartCoroutine(UseAnimationRoutine(cardType));
    }

    private IEnumerator UseAnimationRoutine(HotPotatoCardType cardType)
    {
        Sprite[] frames = GetAnimFrames(cardType);
        Color    accent = FrameColor(cardType);

        // Sem frames: usa o flash de texto como fallback
        if (frames == null || frames.Length == 0)
        {
            yield return StartCoroutine(ColorFlashRoutine(cardType));
            yield break;
        }

        // Flash de fundo colorido
        GameObject flashObj = scene.CreateUIObject("CardUseFlash", root);
        scene.Stretch(flashObj.GetComponent<RectTransform>());
        Image flashImg = flashObj.AddComponent<Image>();
        flashImg.color = Color.clear;
        flashObj.transform.SetAsLastSibling();

        // Sprite animado centralizado
        GameObject animObj = scene.CreateUIObject("CardUseAnim", root);
        RectTransform animRect = animObj.GetComponent<RectTransform>();
        animRect.anchorMin = new Vector2(0.5f, 0.5f);
        animRect.anchorMax = new Vector2(0.5f, 0.5f);
        animRect.pivot     = new Vector2(0.5f, 0.5f);
        animRect.anchoredPosition = Vector2.zero;
        animRect.sizeDelta = new Vector2(340f, 340f);
        Image animImg = animObj.AddComponent<Image>();
        animImg.preserveAspect = true;
        animImg.raycastTarget  = false;
        animObj.transform.SetAsLastSibling();
        CanvasGroup animGroup = animObj.AddComponent<CanvasGroup>();

        const float fps         = 8f;
        const float holdExtra   = 0.3f;
        float frameDuration     = 1f / fps;
        float totalDuration     = frames.Length * frameDuration + holdExtra;
        float timer = 0f, frameTimer = 0f;
        int frameIndex = 0;
        animImg.sprite = frames[0];

        while (timer < totalDuration)
        {
            timer      += Time.deltaTime;
            frameTimer += Time.deltaTime;

            if (frameTimer >= frameDuration && frameIndex < frames.Length - 1)
            {
                frameTimer -= frameDuration;
                frameIndex++;
                animImg.sprite = frames[frameIndex];
            }

            float progress = Mathf.Clamp01(timer / totalDuration);

            // Flash: sobe rápido, desce suave
            float flashAlpha = progress < 0.15f
                ? Mathf.Lerp(0f, 0.25f, progress / 0.15f)
                : Mathf.Lerp(0.25f, 0f, (progress - 0.15f) / 0.85f);
            flashImg.color = new Color(accent.r, accent.g, accent.b, flashAlpha);

            // Alpha do sprite
            animGroup.alpha = progress < 0.12f
                ? Mathf.Lerp(0f, 1f, progress / 0.12f)
                : progress > 0.85f
                    ? Mathf.Lerp(1f, 0f, (progress - 0.85f) / 0.15f)
                    : 1f;

            yield return null;
        }

        Destroy(flashObj);
        Destroy(animObj);
    }

    // Fallback de animação quando não há sprite-sheet
    private IEnumerator ColorFlashRoutine(HotPotatoCardType cardType)
    {
        Color accent = FrameColor(cardType);

        GameObject flashObj = scene.CreateUIObject("CardFlash", root);
        scene.Stretch(flashObj.GetComponent<RectTransform>());
        Image flashImg = flashObj.AddComponent<Image>();
        flashImg.color = Color.clear;
        flashObj.transform.SetAsLastSibling();

        GameObject labelObj = scene.CreateUIObject("CardLabel", root);
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot     = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(700f, 110f);
        Text label = scene.CreateText(labelObj, HotPotatoCardInfo.DisplayName(cardType) + "!",
                                       68, font, TextAnchor.MiddleCenter, accent);
        label.fontStyle = FontStyle.Bold;
        labelObj.transform.SetAsLastSibling();
        CanvasGroup labelGroup = labelObj.AddComponent<CanvasGroup>();

        float t = 0f;
        while (t < 0.14f)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / 0.14f);
            flashImg.color = new Color(accent.r, accent.g, accent.b, p * 0.28f);
            labelGroup.alpha = p;
            labelObj.transform.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, p);
            yield return null;
        }
        yield return new WaitForSeconds(0.52f);
        t = 0f;
        while (t < 0.28f)
        {
            t += Time.deltaTime;
            float p = 1f - Mathf.Clamp01(t / 0.28f);
            flashImg.color = new Color(accent.r, accent.g, accent.b, p * 0.28f);
            labelGroup.alpha = p;
            yield return null;
        }
        Destroy(flashObj);
        Destroy(labelObj);
    }
}
