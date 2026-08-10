using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// MonoBehaviour colocado no PlayerUI (World Space Canvas filho de cada Personagem).
/// Todas as referências de filhos são atribuídas via Inspector ou com o
/// [ContextMenu] abaixo. Não cria nenhum elemento em runtime.
public class HPSeatView : MonoBehaviour, IPointerClickHandler
{
    [Header("Textos")]
    [SerializeField] private Text nameText;
    [SerializeField] private Text livesText;
    [SerializeField] private Text badgeText;

    [Header("Imagens — Vidas")]
    [SerializeField] private Image livesImage;

    [Header("Imagens — Cartas (slots separados)")]
    [SerializeField] private Image cardImage1;   // HeldCard
    [SerializeField] private Image cardImage2;   // HeldCard2

    [Header("Sprites — Vidas")]
    [SerializeField] private Sprite life0;
    [SerializeField] private Sprite life1;
    [SerializeField] private Sprite life2;
    [SerializeField] private Sprite lifeFull;

    [Header("Sprites — Cartas")]
    [SerializeField] private Sprite cardNP3;
    [SerializeField] private Sprite cardAtestado;
    [SerializeField] private Sprite cardMonster;
    [SerializeField] private Sprite cardGPT;
    [SerializeField] private Sprite cardCalculadora;

    private CanvasGroup group;
    private HPGameManager game;
    private int seatIndex = -1;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
    }

    [ContextMenu("Auto-Preencher Filhos")]
    private void AutoFill()
    {
        nameText   = Find<Text>("Name");
        livesText  = Find<Text>("Lives");
        badgeText  = Find<Text>("Badge");
        livesImage = Find<Image>("LivesImage");
        cardImage1 = Find<Image>("CardImage1");
        cardImage2 = Find<Image>("CardImage2");
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private T Find<T>(string childName) where T : Component
    {
        Transform t = transform.Find(childName);
        return t != null ? t.GetComponent<T>() : null;
    }

    public void BindSeatIndex(int index, HPGameManager manager)
    {
        seatIndex = index;
        game = manager;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (seatIndex < 0 || game == null) return;
        game.OnSeatClicked(seatIndex);
    }

    // ── API ───────────────────────────────────────────────────────────────────

    public void SetPlayer(HPPlayer player, int seatNumber)
    {
        if (player == null)
        {
            if (nameText   != null) nameText.text = "Slot " + seatNumber;
            if (livesText  != null) { livesText.text = ""; livesText.gameObject.SetActive(false); }
            if (livesImage != null) livesImage.gameObject.SetActive(false);
            if (badgeText  != null) badgeText.gameObject.SetActive(false);
            SetCardSlot(cardImage1, HPCardType.None);
            SetCardSlot(cardImage2, HPCardType.None);
            return;
        }

        // Nome
        if (nameText != null) nameText.text = player.Name;

        // Vidas
        Sprite livesSprite = GetLivesSprite(player.Lives);
        if (livesSprite != null)
        {
            if (livesImage != null) { livesImage.sprite = livesSprite; livesImage.gameObject.SetActive(true); }
            if (livesText  != null) livesText.gameObject.SetActive(false);
        }
        else
        {
            if (livesImage != null) livesImage.gameObject.SetActive(false);
            if (livesText  != null) { livesText.text = LivesString(player.Lives); livesText.gameObject.SetActive(true); }
        }

        // Dois slots de carta independentes
        SetCardSlot(cardImage1, player.HeldCard);
        SetCardSlot(cardImage2, player.HeldCard2);
    }

    /// Atualiza um slot de carta: exibe o sprite se houver carta, esconde se não houver.
    private void SetCardSlot(Image slot, HPCardType cardType)
    {
        if (slot == null) return;
        Sprite spr = GetCardSprite(cardType);
        if (spr != null)
        {
            slot.sprite = spr;
            slot.gameObject.SetActive(true);
        }
        else
        {
            slot.gameObject.SetActive(false);
        }
    }

    public void SetActive(bool active)
    {
        if (group != null) group.alpha = active ? 1f : 0.78f;
    }

    public void SetBadge(bool show, string text)
    {
        if (badgeText == null) return;
        badgeText.gameObject.SetActive(show);
        if (show) badgeText.text = text;
    }

    public void SetTargetHint(bool hint)
    {
        if (group != null) group.alpha = hint ? 1f : 0.45f;
    }

    // Mantido para compatibilidade — a Light2D da cena cuida do indicador ativo
    public void SetLamp(Color color, bool blink) { }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Sprite GetLivesSprite(int lives)
    {
        switch (lives)
        {
            case 0:  return life0;
            case 1:  return life1;
            case 2:  return life2;
            default: return lifeFull;
        }
    }

    private Sprite GetCardSprite(HPCardType t)
    {
        switch (t)
        {
            case HPCardType.NP3:      return cardNP3;
            case HPCardType.Atestado: return cardAtestado;
            case HPCardType.Monster:  return cardMonster;
            case HPCardType.GPT:      return cardGPT;
            case HPCardType.Calculadora: return cardCalculadora;
            default:                  return null;
        }
    }

    private static string LivesString(int lives)
    {
        string s = "";
        for (int i = 0; i < lives; i++) s += "♥";
        return s;
    }
}