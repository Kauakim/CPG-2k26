using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// Constrói o prefab HPCanvas completo com todos os sub-sistemas do SistemaHP
/// já conectados entre si. Após instanciar na cena, só é necessário arrastar
/// as referências de cena (SceneBridge e HPSeatView[8]) no Inspector do HPGameManager.
/// Menu: HotPotato → Criar Prefab HPCanvas
public static class HPCanvasPrefabBuilder
{
    private const string PREFAB_PATH = "Assets/Prefabs/HPCanvas.prefab";

    static readonly Vector2 C  = new Vector2(0.5f, 0.5f);  // centro
    static readonly Vector2 TL = new Vector2(0f,   1f);    // topo-esquerda
    static readonly Vector2 TC = new Vector2(0.5f, 1f);    // topo-centro
    static readonly Vector2 BL = new Vector2(0f,   0f);    // baixo-esquerda
    static readonly Vector2 BR = new Vector2(1f,   0f);    // baixo-direita

    // ── Entry point ───────────────────────────────────────────────────────────

    [MenuItem("HotPotato/Criar Prefab HPCanvas")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        Font font = GetFont();

        // ── Canvas raiz ───────────────────────────────────────────────────────
        GameObject root = new GameObject("HPCanvas");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        // Scripts lógicos direto na raiz
        HPQuestionSystem questionSystem = root.AddComponent<HPQuestionSystem>();
        HPGameManager    gameManager    = root.AddComponent<HPGameManager>();

        // ── Áudio ─────────────────────────────────────────────────────────────
        GameObject musicGO  = Child("MusicSource",  root.transform);
        AudioSource musicSrc = musicGO.AddComponent<AudioSource>();
        musicSrc.loop = true; musicSrc.playOnAwake = false;
        HPMusicSystem musicSystem = musicGO.AddComponent<HPMusicSystem>();

        GameObject sfxGO  = Child("SFXSource", root.transform);
        AudioSource sfxSrc = sfxGO.AddComponent<AudioSource>();
        sfxSrc.loop = false; sfxSrc.playOnAwake = false;

        // ── Timer ─────────────────────────────────────────────────────────────
        GameObject timerGO  = Child("TimerSystem", root.transform);
        HPTimerSystem timerSystem = timerGO.AddComponent<HPTimerSystem>();

        GameObject timerTextGO = Child("TimerText", root.transform);
        Anchored(timerTextGO, C, C, C, new Vector2(-120f, 80f), new Vector2(120f, 60f));
        Text timerText = Txt(timerTextGO, font, "30", 48, FontStyle.Bold,
                             TextAnchor.MiddleCenter, Color.white);

        // ── Lobby ─────────────────────────────────────────────────────────────
        GameObject lobbyTop = Child("LobbyTop", root.transform);
        Anchored(lobbyTop, TC, TC, TC, new Vector2(0f, -80f), new Vector2(800f, 80f));
        HPLobbyView lobbyView = lobbyTop.AddComponent<HPLobbyView>();

        GameObject lobbyTextGO = Child("LobbyText", lobbyTop.transform);
        Stretch(lobbyTextGO);
        Text lobbyText = Txt(lobbyTextGO, font, "Aguardando jogadores...",
                              28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        lobbyText.horizontalOverflow = HorizontalWrapMode.Overflow;
        lobbyText.verticalOverflow   = VerticalWrapMode.Overflow;

        // Countdown
        GameObject cdRoot = Child("CountdownRoot", root.transform);
        Anchored(cdRoot, TC, TC, TC, new Vector2(0f, -80f), new Vector2(800f, 140f));
        cdRoot.SetActive(false);
        Shadow cdShadow = cdRoot.AddComponent<Shadow>();
        cdShadow.effectColor    = new Color(0f, 0f, 0f, 0.85f);
        cdShadow.effectDistance = new Vector2(2f, -2f);

        GameObject cdTextGO = Child("CountdownText", cdRoot.transform);
        Stretch(cdTextGO);
        Text cdText = Txt(cdTextGO, font, "", 82, FontStyle.Bold,
                           TextAnchor.MiddleCenter, Hex("#FFDF4D"));

        // AddBot button
        GameObject addBotGO = MakeButton("AddBotButton", root.transform, font, "Adicionar Bot", 20);
        Anchored(addBotGO, BL + new Vector2(0.5f, 0f), BL + new Vector2(0.5f, 0f),
                 BL + new Vector2(0.5f, 0f), new Vector2(0f, 100f), new Vector2(230f, 52f));

        // ── Textos avulsos ────────────────────────────────────────────────────
        // LastAnswer
        GameObject laGO = Child("LastAnswerText", root.transform);
        Anchored(laGO, BL, BL, BL, new Vector2(28f, 24f), new Vector2(320f, 80f));
        Text lastAnswerText = Txt(laGO, font, "", 22, FontStyle.Bold,
                                   TextAnchor.MiddleLeft, Color.white);
        lastAnswerText.horizontalOverflow = HorizontalWrapMode.Overflow;
        lastAnswerText.verticalOverflow   = VerticalWrapMode.Overflow;

        // Sudden Death
        GameObject sdGO = Child("SuddenDeathText", root.transform);
        Anchored(sdGO, C, C, C, new Vector2(0f, 180f), new Vector2(620f, 80f));
        Text sdText = Txt(sdGO, font, "MORTE SÚBITA", 54, FontStyle.Bold,
                           TextAnchor.MiddleCenter, Hex("#FF274F"));
        sdGO.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.85f);
        sdGO.SetActive(false);

        // ── Botões de usar carta ───────────────────────────────────────────────
        (Button ucBtn1, Image ucIcon1) = MakeCardSlot("UseCardButton1", root.transform,
                                                        new Vector2(-20f,  70f));
        (Button ucBtn2, Image ucIcon2) = MakeCardSlot("UseCardButton2", root.transform,
                                                        new Vector2(-20f, -20f));

        // ── Painel de pergunta ─────────────────────────────────────────────────
        GameObject qPanel = Child("QuestionPanel", root.transform);
        Anchored(qPanel, C, C, C, new Vector2(0f, -60f), new Vector2(860f, 320f));
        Image qPanelBg = qPanel.AddComponent<Image>();
        qPanelBg.color = new Color(0.05f, 0.06f, 0.12f, 0.93f);
        qPanel.SetActive(false);
        HPQuestionView questionView = qPanel.AddComponent<HPQuestionView>();

        GameObject pNameGO = Child("PlayerNameText", qPanel.transform);
        Anchored(pNameGO, TC, TC, TC, new Vector2(0f, -22f), new Vector2(800f, 36f));
        Text pNameText = Txt(pNameGO, font, "Jogador", 24, FontStyle.Bold,
                              TextAnchor.MiddleCenter, Hex("#FFDF4D"));

        GameObject qTextGO = Child("QuestionText", qPanel.transform);
        Anchored(qTextGO, C, C, C, new Vector2(0f, 40f), new Vector2(800f, 80f));
        Text qText = Txt(qTextGO, font, "", 32, FontStyle.Bold,
                  TextAnchor.MiddleCenter, Color.white);
        qText.horizontalOverflow = HorizontalWrapMode.Overflow;

        InputField answerInput = MakeInputField("AnswerInput", qPanel.transform, font);
        Anchored(answerInput.gameObject, C, C, C, new Vector2(-90f, -52f), new Vector2(480f, 56f));

        GameObject submitGO = MakeButton("SubmitButton", qPanel.transform, font, "Responder", 22);
        Anchored(submitGO, C, C, C, new Vector2(280f, -52f), new Vector2(180f, 56f));

        // ── Painel de escolha de carta ─────────────────────────────────────────
        // CardSystem — pai de todos os elementos de carta
        GameObject cardSystem = Child("CardSystem", root.transform);
        Stretch(cardSystem);
        HPCardView cardView = cardSystem.AddComponent<HPCardView>();

        GameObject cardPanel = Child("CardChoicePanel", cardSystem.transform);
        Anchored(cardPanel, C, C, C, new Vector2(0f, -10f), new Vector2(620f, 280f));
        cardPanel.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.12f, 0.95f);
        cardPanel.SetActive(false);

        GameObject cardTitleGO = Child("CardTitle", cardPanel.transform);
        Anchored(cardTitleGO, TC, TC, TC, new Vector2(0f, -22f), new Vector2(580f, 40f));
        Text cardTitleText = Txt(cardTitleGO, font, "ganhou uma carta!", 24, FontStyle.Bold,
                                  TextAnchor.MiddleCenter, Color.white);

        (GameObject opt1GO, Image opt1Icon, Text opt1Label) =
            MakeCardOption("Option1", cardPanel.transform, font, new Vector2(-148f, -38f));
        (GameObject opt2GO, Image opt2Icon, Text opt2Label) =
            MakeCardOption("Option2", cardPanel.transform, font, new Vector2( 148f, -38f));

        // Anim overlay
        GameObject animOverlay = Child("AnimOverlay", cardSystem.transform);
        Stretch(animOverlay);
        animOverlay.SetActive(false);
        CanvasGroup animCG = animOverlay.AddComponent<CanvasGroup>();
        animCG.alpha             = 0f;
        animCG.interactable      = false;
        animCG.blocksRaycasts    = false;

        GameObject flashGO = Child("FlashImage", animOverlay.transform);
        Stretch(flashGO);
        Image flashImg = flashGO.AddComponent<Image>();
        flashImg.color         = Color.clear;
        flashImg.raycastTarget = false;

        GameObject animImgGO = Child("AnimImage", animOverlay.transform);
        Anchored(animImgGO, C, C, C, Vector2.zero, new Vector2(340f, 340f));
        Image animImg = animImgGO.AddComponent<Image>();
        animImg.preserveAspect = true;
        animImg.raycastTarget  = false;

        // Notificação flutuante
        GameObject floatRoot = Child("FloatingCard", cardSystem.transform);
        Anchored(floatRoot, C, C, C, new Vector2(0f, 118f), new Vector2(420f, 56f));
        floatRoot.SetActive(false);
        CanvasGroup floatCG = floatRoot.AddComponent<CanvasGroup>();

        GameObject floatTextGO = Child("FloatingText", floatRoot.transform);
        Stretch(floatTextGO);
        Text floatText = Txt(floatTextGO, font, "", 28, FontStyle.Bold,
                              TextAnchor.MiddleCenter, Hex("#FFDF4D"));

        // ── Painel de operação ─────────────────────────────────────────────────
        GameObject opPanel = Child("OpChoicePanel", root.transform);
        Anchored(opPanel, C, C, C, new Vector2(0f, -20f), new Vector2(760f, 240f));
        opPanel.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.12f, 0.95f);
        opPanel.SetActive(false);
        HPOpChoiceView opView = opPanel.AddComponent<HPOpChoiceView>();

        GameObject opTitleGO = Child("OpTitle", opPanel.transform);
        Anchored(opTitleGO, TC, TC, TC, new Vector2(0f, -18f), new Vector2(720f, 38f));
        Text opTitle = Txt(opTitleGO, font, "Como complicar?", 22, FontStyle.Bold,
                            TextAnchor.MiddleCenter, Hex("#FFDF4D"));

        float[] opX = { -240f, 0f, 240f };
        (GameObject op1GO, Text op1Lbl) = MakeOpButton("Op1", opPanel.transform, font, opX[0]);
        (GameObject op2GO, Text op2Lbl) = MakeOpButton("Op2", opPanel.transform, font, opX[1]);
        (GameObject op3GO, Text op3Lbl) = MakeOpButton("Op3", opPanel.transform, font, opX[2]);

        // ── Resultado ─────────────────────────────────────────────────────────
        GameObject resultOverlay = Child("ResultOverlay", root.transform);
        Stretch(resultOverlay);
        resultOverlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
        resultOverlay.SetActive(false);
        HPResultView resultView = resultOverlay.AddComponent<HPResultView>();

        GameObject winnerTextGO = Child("WinnerText", resultOverlay.transform);
        Anchored(winnerTextGO, C, C, C, new Vector2(0f, 96f), new Vector2(900f, 90f));
        Text winnerText = Txt(winnerTextGO, font, "", 50, FontStyle.Bold,
                               TextAnchor.MiddleCenter, Color.white);
        winnerText.GetComponent<Shadow>()?.Equals(null);     // garante sem shadow duplo
        Shadow wShadow = winnerTextGO.AddComponent<Shadow>();
        wShadow.effectColor = Hex("#FFDF4D");

        GameObject restartGO = MakeButton("RestartButton", resultOverlay.transform,
                                           font, "Jogar Novamente", 22);
        Anchored(restartGO, C, C, C, new Vector2(0f, -18f), new Vector2(260f, 58f));

        // ── Conectar referências ───────────────────────────────────────────────

        // HPTimerSystem
        Wire(timerSystem, "timerLabel", timerText);

        // HPMusicSystem
        Wire(musicSystem, "audioSource", musicSrc);

        // HPLobbyView
        Wire(lobbyView, "lobbyRoot",      lobbyTop);
        Wire(lobbyView, "lobbyText",      lobbyText);
        Wire(lobbyView, "countdownRoot",  cdRoot);
        Wire(lobbyView, "countdownText",  cdText);
        Wire(lobbyView, "addBotButton",   addBotGO.GetComponent<Button>());

        // HPQuestionView
        Wire(questionView, "panel",          qPanel);
        Wire(questionView, "playerNameText", pNameText);
        Wire(questionView, "questionText",   qText);
        Wire(questionView, "answerInput",    answerInput);
        Wire(questionView, "submitButton",   submitGO.GetComponent<Button>());

        // HPResultView
        Wire(resultView, "overlay",        resultOverlay);
        Wire(resultView, "winnerText",     winnerText);
        Wire(resultView, "restartButton",  restartGO.GetComponent<Button>());

        // HPCardView
        Wire(cardView, "choicePanel",     cardPanel);
        Wire(cardView, "choiceTitleText", cardTitleText);
        Wire(cardView, "option1Button",   opt1GO.GetComponent<Button>());
        Wire(cardView, "option2Button",   opt2GO.GetComponent<Button>());
        Wire(cardView, "option1Icon",     opt1Icon);
        Wire(cardView, "option2Icon",     opt2Icon);
        Wire(cardView, "option1Label",    opt1Label);
        Wire(cardView, "option2Label",    opt2Label);
        Wire(cardView, "animOverlay",     animOverlay);
        Wire(cardView, "animGroup",       animCG);
        Wire(cardView, "animImage",       animImg);
        Wire(cardView, "flashImage",      flashImg);
        Wire(cardView, "floatingRoot",    floatRoot);
        Wire(cardView, "floatingText",    floatText);
        Wire(cardView, "floatingRect",    floatRoot.GetComponent<RectTransform>());
        Wire(cardView, "floatingGroup",   floatCG);

        // HPOpChoiceView
        Wire(opView, "panel",     opPanel);
        Wire(opView, "titleText", opTitle);
        Wire(opView, "op1Button", op1GO.GetComponent<Button>());
        Wire(opView, "op2Button", op2GO.GetComponent<Button>());
        Wire(opView, "op3Button", op3GO.GetComponent<Button>());
        Wire(opView, "op1Label",  op1Lbl);
        Wire(opView, "op2Label",  op2Lbl);
        Wire(opView, "op3Label",  op3Lbl);

        // HPGameManager — sub-sistemas
        Wire(gameManager, "timerSystem",    timerSystem);
        Wire(gameManager, "musicSystem",    musicSystem);
        Wire(gameManager, "questionSystem", questionSystem);
        Wire(gameManager, "lobbyView",      lobbyView);
        Wire(gameManager, "questionView",   questionView);
        Wire(gameManager, "resultView",     resultView);
        Wire(gameManager, "cardView",       cardView);
        Wire(gameManager, "opChoiceView",   opView);
        // UI avulsa
        Wire(gameManager, "lastAnswerText",  lastAnswerText);
        Wire(gameManager, "suddenDeathText", sdText);
        Wire(gameManager, "screenRoot",      root.GetComponent<RectTransform>());
        Wire(gameManager, "useCardButton1",  ucBtn1);
        Wire(gameManager, "useCardButton2",  ucBtn2);
        Wire(gameManager, "useCardIcon1",    ucIcon1);
        Wire(gameManager, "useCardIcon2",    ucIcon2);
        Wire(gameManager, "sfxSource",       sfxSrc);
        // sceneBridge e seatViews[] ficam vazios — preencher via Inspector após instanciar na cena

        // ── Salvar prefab ──────────────────────────────────────────────────────
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("[HotPotato] HPCanvas prefab criado em: " + PREFAB_PATH);
    }

    // ── Helpers de layout ─────────────────────────────────────────────────────

    private static GameObject Child(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static RectTransform RT(GameObject go) => go.GetComponent<RectTransform>();

    private static void Anchored(GameObject go,
                                  Vector2 anchorMin, Vector2 anchorMax,
                                  Vector2 pivot, Vector2 pos, Vector2 size)
    {
        RectTransform rt = RT(go);
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
    }

    private static void Stretch(GameObject go,
                                 float padL = 0, float padR = 0,
                                 float padB = 0, float padT = 0)
    {
        RectTransform rt = RT(go);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot     = C;
        rt.offsetMin = new Vector2(padL, padB);
        rt.offsetMax = new Vector2(-padR, -padT);
    }

    // ── Helpers de componentes ────────────────────────────────────────────────

    private static Text Txt(GameObject go, Font font, string text,
                              int size, FontStyle style,
                              TextAnchor align, Color color)
    {
        Text t = go.GetComponent<Text>() ?? go.AddComponent<Text>();
        t.font       = font;
        t.text       = text;
        t.fontSize   = size;
        t.fontStyle  = style;
        t.alignment  = align;
        t.color      = color;
        return t;
    }

    private static GameObject MakeButton(string name, Transform parent,
                                          Font font, string label, int fontSize)
    {
        GameObject go  = Child(name, parent);
        Image img      = go.AddComponent<Image>();
        img.color      = new Color(0.18f, 0.20f, 0.30f, 0.95f);
        Button btn     = go.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb  = btn.colors;
        cb.highlightedColor = new Color(0.28f, 0.30f, 0.42f, 1f);
        cb.pressedColor     = new Color(0.10f, 0.12f, 0.20f, 1f);
        btn.colors = cb;

        GameObject txtGO = Child("Text", go.transform);
        Stretch(txtGO, 6, 6, 4, 4);
        Txt(txtGO, font, label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        return go;
    }

    private static InputField MakeInputField(string name, Transform parent, Font font)
    {
        GameObject go  = Child(name, parent);
        Image img      = go.AddComponent<Image>();
        img.color      = new Color(0.08f, 0.09f, 0.14f, 0.95f);
        InputField inp = go.AddComponent<InputField>();

        GameObject phGO = Child("Placeholder", go.transform);
        Stretch(phGO, 10, 10, 6, 6);
        Text phTxt = Txt(phGO, font, "Digite a resposta...", 20, FontStyle.Italic,
                          TextAnchor.MiddleLeft, new Color(0.55f, 0.55f, 0.55f, 0.8f));

        GameObject txtGO = Child("Text", go.transform);
        Stretch(txtGO, 10, 10, 6, 6);
        Text txt = Txt(txtGO, font, "", 20, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);

        inp.textComponent  = txt;
        inp.placeholder    = phTxt;
        inp.contentType    = InputField.ContentType.DecimalNumber;
        inp.lineType       = InputField.LineType.SingleLine;
        inp.characterLimit = 12;
        return inp;
    }

    private static (Button btn, Image icon) MakeCardSlot(string name, Transform parent, Vector2 pos)
    {
        GameObject go = Child(name, parent);
        Anchored(go, BR, BR, BR, pos, new Vector2(80f, 80f));
        go.SetActive(false);
        Image bg   = go.AddComponent<Image>();
        bg.color   = new Color(1f, 1f, 1f, 0.10f);
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;

        GameObject iconGO = Child("Icon", go.transform);
        Stretch(iconGO);
        Image icon            = iconGO.AddComponent<Image>();
        icon.preserveAspect   = true;
        icon.raycastTarget    = false;
        icon.color            = Color.clear;

        return (btn, icon);
    }

    private static (GameObject, Image, Text) MakeCardOption(string name, Transform parent,
                                                              Font font, Vector2 pos)
    {
        GameObject go = Child(name, parent);
        Anchored(go, C, C, C, pos, new Vector2(260f, 200f));
        Image bg   = go.AddComponent<Image>();
        bg.color   = new Color(0.10f, 0.12f, 0.20f, 1f);
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;

        GameObject iconGO = Child("Icon", go.transform);
        Anchored(iconGO, TL, new Vector2(1f, 1f), TC,
                 new Vector2(0f, -10f), new Vector2(-20f, -50f));
        // posição relativa ao botão (ocupa quase tudo exceto o label)
        RectTransform iconRT = RT(iconGO);
        iconRT.anchorMin        = new Vector2(0.1f, 0.2f);
        iconRT.anchorMax        = new Vector2(0.9f, 0.85f);
        iconRT.offsetMin        = Vector2.zero;
        iconRT.offsetMax        = Vector2.zero;
        Image icon              = iconGO.AddComponent<Image>();
        icon.preserveAspect     = true;
        icon.raycastTarget      = false;
        icon.color              = Color.clear;

        GameObject lblGO = Child("Label", go.transform);
        Anchored(lblGO, BL, BR, new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(0f, 32f));
        RectTransform lblRT  = RT(lblGO);
        lblRT.anchorMin      = new Vector2(0f, 0f);
        lblRT.anchorMax      = new Vector2(1f, 0f);
        lblRT.offsetMin      = new Vector2(4f, 6f);
        lblRT.offsetMax      = new Vector2(-4f, 38f);
        Text lbl = Txt(lblGO, font, "", 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);

        return (go, icon, lbl);
    }

    private static (GameObject, Text) MakeOpButton(string name, Transform parent,
                                                     Font font, float x)
    {
        GameObject go = MakeButton(name, parent, font, "", 32);
        Anchored(go, C, C, C, new Vector2(x, -30f), new Vector2(210f, 120f));
        // O label é o único Text filho do botão
        Text lbl = go.GetComponentInChildren<Text>();
        if (lbl != null)
        {
            lbl.fontSize  = 32;
            lbl.fontStyle = FontStyle.Bold;
            lbl.alignment = TextAnchor.MiddleCenter;
        }
        return (go, lbl);
    }

    // ── Wire (SerializedObject) ───────────────────────────────────────────────

    private static void Wire(Object target, string field, Object value)
    {
        SerializedObject   so   = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogWarning($"[HPCanvasBuilder] Campo '{field}' não encontrado em {target.GetType().Name}");
            return;
        }
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ── Utilitários ───────────────────────────────────────────────────────────

    private static Color Hex(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString(hex, out c);
        return c;
    }

    private static Font GetFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }
}
