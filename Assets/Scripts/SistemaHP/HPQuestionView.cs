using UnityEngine;
using UnityEngine.UI;

/// QUADRO: panel fica sempre visível depois que o jogo começa (ShowBoard).
/// Input e botão somem quando não é o turno do jogador local.
public class HPQuestionView : MonoBehaviour
{
    [Header("Container")]
    [SerializeField] private GameObject panel;

    [Header("Conteúdo")]
    [SerializeField] private Text       playerNameText;
    [SerializeField] private Text       questionText;
    [SerializeField] private InputField answerInput;
    [SerializeField] private Button     submitButton;

    private HPGameManager game;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void SetGame(HPGameManager manager)
    {
        game = manager;

        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(OnSubmit);
        }
        if (answerInput != null)
        {
            answerInput.onValidateInput = ValidateChar;
            answerInput.onEndEdit.RemoveAllListeners();
            answerInput.onEndEdit.AddListener(OnEndEdit);
        }
    }

    // ── QUADRO ────────────────────────────────────────────────────────────────

    /// Chama uma vez em BeginGame — o painel nunca some depois disso.
    public void ShowBoard()
    {
        if (panel != null) panel.SetActive(true);
        if (playerNameText != null) playerNameText.text = "";
        SetInputVisible(false);
    }

    public void ShowLobbyStatus(string title, string message)
    {
        if (panel != null) panel.SetActive(true);
        if (playerNameText != null) playerNameText.text = title;
        if (questionText != null) questionText.text = message;
        SetInputVisible(false);
        if (answerInput != null) answerInput.text = "";
    }

    public void HideBoard()
    {
        if (panel != null) panel.SetActive(false);
        SetInputVisible(false);
    }

    public void UpdateQuestion(string question)
    {
        if (questionText != null) questionText.text = question;
    }

    // ── Turno ─────────────────────────────────────────────────────────────────

    public void Show(string playerName, string question, bool isLocal)
    {
        if (panel != null) panel.SetActive(true);
        if (playerNameText != null) playerNameText.text = playerName;
        if (questionText   != null) questionText.text   = question;
        SetInputVisible(isLocal);
        if (isLocal && answerInput != null)
        {
            answerInput.text = "";
            answerInput.Select();
            answerInput.ActivateInputField();
        }
    }

    /// Turno do bot — atualiza só o texto da pergunta no quadro.
    /// Não exibe nome, não exibe input, não revela o que o bot está processando.
    public void ShowForBot(string question)
    {
        if (panel          != null) panel.SetActive(true);
        if (playerNameText != null) playerNameText.text = "";
        if (questionText   != null) questionText.text   = question;
        SetInputVisible(false);
        if (answerInput    != null) answerInput.text    = "";
    }

    /// Esconde apenas o campo de input — o QUADRO continua visível.
    public void Hide()
    {
        SetInputVisible(false);
        if (answerInput != null) answerInput.text = "";
    }

    public void SetInteractable(bool value)
    {
        if (answerInput  != null) answerInput.interactable  = value;
        if (submitButton != null) submitButton.interactable = value;
    }

    public void SetInputText(string text)
    {
        if (answerInput != null) answerInput.text = text;
    }

    public string GetInputText() => answerInput != null ? answerInput.text : "";

    // ── Eventos ───────────────────────────────────────────────────────────────

    private void OnEndEdit(string text)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            OnSubmit();
    }

    private void OnSubmit()
    {
        if (answerInput == null || game == null) return;
        string text = answerInput.text.Trim();
        if (string.IsNullOrEmpty(text) || text == "-") return;
        game.SubmitAnswer(text);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetInputVisible(bool visible)
    {
        if (answerInput  != null) answerInput.gameObject.SetActive(visible);
        if (submitButton != null) submitButton.gameObject.SetActive(visible);
        if (answerInput  != null) answerInput.interactable  = visible;
        if (submitButton != null) submitButton.interactable = visible;
    }

    private char ValidateChar(string text, int charIndex, char addedChar)
    {
        if (addedChar >= '0' && addedChar <= '9') return addedChar;
        if ((addedChar == '.' || addedChar == ',') && !text.Contains(".") && !text.Contains(","))
            return addedChar;
        if (addedChar == '-' && charIndex == 0 && text.Length == 0)
            return addedChar;
        return '\0';
    }
}
