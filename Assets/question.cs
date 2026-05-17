using UnityEngine;
using UnityEngine.UI;

public class question : MonoBehaviour
{
    private core game;
    private RectTransform canvasRoot;
    private GameObject panel;
    private GameObject inputContainer;
    private GameObject buttonContainer;
    private Text questionText;
    private InputField answerInput;
    private Button answerButton;
    private Image quadroImage;

    public float AnswerBarBottomY { get { return 8f; } }
    public float AnswerBarHeight  { get { return 52f; } }

    public void Initialize(core owner, RectTransform root, Font font, Sprite quadroSprite = null)
    {
        game = owner;
        canvasRoot = root;

        panel = scene.CreateUIObject("QuestionUI", root);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        scene.Stretch(panelRect);

        // Quadro de fundo da pergunta
        GameObject quadroObject = scene.CreateUIObject("Quadro", panel.transform);
        RectTransform quadroRect = quadroObject.GetComponent<RectTransform>();
        quadroRect.anchorMin = new Vector2(0f, 1f);
        quadroRect.anchorMax = new Vector2(1f, 1f);
        quadroRect.pivot = new Vector2(0.5f, 1f);
        quadroRect.anchoredPosition = new Vector2(0f, 90f);
        quadroRect.sizeDelta = new Vector2(-800f, 450f);
        quadroImage = quadroObject.AddComponent<Image>();
        quadroImage.preserveAspect = false;
        if (quadroSprite != null)
        {
            quadroImage.sprite = quadroSprite;
            quadroImage.color = Color.white;
        }
        else
        {
            quadroImage.color = new Color(0.04f, 0.06f, 0.14f, 0.88f);
        }

        GameObject activeObject = scene.CreateUIObject("ActivePlayer", quadroObject.transform);
        RectTransform activeRect = activeObject.GetComponent<RectTransform>();
        activeRect.anchorMin = new Vector2(0f, 1f);
        activeRect.anchorMax = new Vector2(1f, 1f);
        activeRect.pivot = new Vector2(0.5f, 1f);
        activeRect.anchoredPosition = new Vector2(0f, -14f);
        activeRect.sizeDelta = new Vector2(-40f, 32f);

        GameObject questionObject = scene.CreateUIObject("Question", quadroObject.transform);
        RectTransform questionRect = questionObject.GetComponent<RectTransform>();
        questionRect.anchorMin = new Vector2(0f, 0f);
        questionRect.anchorMax = new Vector2(1f, 1f);
        questionRect.pivot = new Vector2(0.5f, 1f);
        questionRect.offsetMin = new Vector2(20f, 80f);
        questionRect.offsetMax = new Vector2(-20f, -50f);
        questionText = scene.CreateText(questionObject, "", 28, font, TextAnchor.MiddleCenter, Color.white);
        questionText.fontStyle = FontStyle.Bold;
        questionText.verticalOverflow = VerticalWrapMode.Overflow;

        // Input e botao de resposta ficam na base do root, fora do quadro
        GameObject inputObject = scene.CreateUIObject("AnswerInput", root);
        inputContainer = inputObject;
        RectTransform inputRect = inputObject.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.5f, 0f);
        inputRect.anchorMax = new Vector2(0.5f, 0f);
        inputRect.pivot = new Vector2(1f, 0f);
        inputRect.anchoredPosition = new Vector2(60f, AnswerBarBottomY);
        inputRect.sizeDelta = new Vector2(300f, AnswerBarHeight);
        inputObject.SetActive(false);

        Image inputBackground = inputObject.AddComponent<Image>();
        inputBackground.sprite = scene.CreateRoundedRectSprite(260, 64, 14, scene.Hex("#101a32"), scene.Hex("#46e4ff"), 2);
        inputBackground.type = Image.Type.Sliced;
        inputBackground.color = Color.white;
        answerInput = inputObject.AddComponent<InputField>();
        answerInput.contentType = InputField.ContentType.Custom;
        answerInput.lineType = InputField.LineType.SingleLine;
        answerInput.characterLimit = 12;
        answerInput.onValidateInput += ValidateAnswerChar;
        answerInput.onValueChanged.AddListener(delegate { UpdateSubmitState(); });

        GameObject inputTextObject = scene.CreateUIObject("Text", inputObject.transform);
        RectTransform inputTextRect = inputTextObject.GetComponent<RectTransform>();
        inputTextRect.anchorMin = Vector2.zero;
        inputTextRect.anchorMax = Vector2.one;
        inputTextRect.offsetMin = new Vector2(18f, 2f);
        inputTextRect.offsetMax = new Vector2(-18f, -2f);
        Text inputText = scene.CreateText(inputTextObject, "", 24, font, TextAnchor.MiddleLeft, Color.white);
        inputText.supportRichText = false;

        GameObject placeholderObject = scene.CreateUIObject("Placeholder", inputObject.transform);
        RectTransform placeholderRect = placeholderObject.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(18f, 2f);
        placeholderRect.offsetMax = new Vector2(-18f, -2f);
        Text placeholder = scene.CreateText(placeholderObject, "Resposta", 20, font, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.38f));

        answerInput.textComponent = inputText;
        answerInput.placeholder = placeholder;

        GameObject buttonObject = scene.CreateButton("AnswerButton", root, "Responder", font, Submit);
        buttonContainer = buttonObject;
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0f, 0f);
        buttonRect.anchoredPosition = new Vector2(68f, AnswerBarBottomY);
        buttonRect.sizeDelta = new Vector2(180f, AnswerBarHeight);
        answerButton = buttonObject.GetComponent<Button>();
        buttonObject.SetActive(false);

        Hide();
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf || answerInput == null || !answerInput.interactable)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Submit();
        }
    }

    public void Show(string playerName, string text)
    {
        ShowQuestion(playerName, text, true);
    }

    public void ShowQuestion(string playerName, string text, bool allowInput)
    {
        panel.SetActive(true);
        questionText.text = text;
        SetInputVisible(allowInput);

        if (allowInput)
        {
            answerInput.text = "";
            SetInteractable(true);
            UpdateSubmitState();
            answerInput.ActivateInputField();
        }
        else
        {
            SetInteractable(false);
        }
    }

    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    public void ClearInput()
    {
        if (answerInput != null)
        {
            answerInput.text = "";
            answerInput.ActivateInputField();
        }
    }

    public void SetInputText(string value)
    {
        if (answerInput != null)
        {
            answerInput.text = value;
        }
    }

    public void SetInputVisible(bool visible)
    {
        if (inputContainer != null) inputContainer.SetActive(visible);
        if (buttonContainer != null) buttonContainer.SetActive(visible);
    }

    public void SetQuestionText(string text)
    {
        if (questionText != null)
        {
            questionText.text = text;
        }
    }

    public void SetInteractable(bool enabled)
    {
        if (answerInput != null)
        {
            answerInput.interactable = enabled;
        }

        UpdateSubmitState();
    }

    private void UpdateSubmitState()
    {
        if (answerButton == null)
        {
            return;
        }

        bool hasText = answerInput != null && !string.IsNullOrEmpty(answerInput.text);
        bool canSubmit = answerInput != null && answerInput.interactable && hasText;
        answerButton.interactable = canSubmit;
    }

    private char ValidateAnswerChar(string text, int index, char addedChar)
    {
        if ((addedChar >= '0' && addedChar <= '9') || addedChar == ',' || addedChar == '.')
        {
            return addedChar;
        }

        if (addedChar == '-' && index == 0 && !text.Contains("-"))
        {
            return addedChar;
        }

        return '\0';
    }

    private void Submit()
    {
        if (answerInput == null || game == null)
        {
            return;
        }

        game.SubmitAnswer(answerInput.text);
    }
}
