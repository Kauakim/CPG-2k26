using UnityEngine;
using UnityEngine.UI;

/// MonoBehaviour colocado no PlayerUI (World Space Canvas filho de cada Personagem).
/// Todas as referências de filhos são atribuídas via Inspector ou com o
/// [ContextMenu] abaixo. Não cria nenhum elemento em runtime.
public class HPSeatView : MonoBehaviour
{
    [Header("Textos")]
    [SerializeField] private Text nameText;
    [SerializeField] private Text livesText;
    [SerializeField] private Text cardText;
    [SerializeField] private Text badgeText;

    [Header("Imagens")]
    [SerializeField] private Image livesImage;
    [SerializeField] private Image cardImage;

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

    private CanvasGroup group;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
    }

    [ContextMenu("Auto-Preencher Filhos")]
    private void AutoFill()
    {
        nameText   = Find<Text>("Name");
        livesText  = Find<Text>("Lives");
        cardText   = Find<Text>("Cards");
        badgeText  = Find<Text>("Badge");
        livesImage = Find<Image>("LivesImage");
        cardImage  = Find<Image>("CardImage");
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private T Find<T>(string childName) where T : Component
    {
        Transform t = transform.Find(childName);
        return t != null ? t.GetComponent<T>() : null;
    }

    // ── API ──────────────────────────────────────────────────────────────────

    public void SetPlayer(HPPlayer player, int seatNumber)
    {
        if (player == null)
        {
            if (nameText  != null) nameText.text = "Slot " + seatNumber;
            if (livesText != null) { livesText.text = ""; livesText.gameObject.SetActive(false); }
            if (livesImage != null) { livesImage.sprite = null; livesImage.gameObject.SetActive(false); }
            if (cardText  != null) cardText.text = "";
            if (cardImage != null) cardImage.gameObject.SetActive(false);
            if (badgeText != null) badgeText.gameObject.SetActive(false);
            return;
        }

        if (nameText != null) nameText.text = player.Name;

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

        HPCardType card = player.HeldCard != HPCardType.None ? player.HeldCard : player.HeldCard2;
        Sprite cardSprite = GetCardSprite(card);
        if (cardSprite != null)
        {
            if (cardImage != null) { cardImage.sprite = cardSprite; cardImage.gameObject.SetActive(true); }
            if (cardText  != null) cardText.text = "";
        }
        else
        {
            if (cardImage != null) cardImage.gameObject.SetActive(false);
            if (cardText  != null) cardText.text = card != HPCardType.None ? HPCardInfo.DisplayName(card) : "";
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
        // Pulso visual para seleção de alvo do Atestado
        if (group != null) group.alpha = hint ? 1f : 0.45f;
    }

    // Mantido para compatibilidade com HPGameManager; no modo cena a Light2D cuida do indicador ativo
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
