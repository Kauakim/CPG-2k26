using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Exibe um tooltip com descrição curta ao passar o mouse/dedo sobre
/// a mini-carta na lateral do assento de outro jogador.
/// Anexado ao GameObject SideCard em cada assento.
/// </summary>
public class SeatCardTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform canvasRoot;
    private Font font;
    private HotPotatoCardType currentCardType = HotPotatoCardType.None;
    private GameObject tooltipObject;

    // ── Descrições curtas exibidas no hover ──────────────────────────────────
    private static string ShortDescription(HotPotatoCardType cardType)
    {
        switch (cardType)
        {
            case HotPotatoCardType.NP3:      return "+1 vida (máx 2→3)";
            case HotPotatoCardType.Atestado: return "Redireciona a seta";
            case HotPotatoCardType.Monster:  return "Dobra o tempo restante";
            case HotPotatoCardType.GPT:      return "Responde automático";
            default:                          return "";
        }
    }

    // ── Cor da moldura por tipo (coerente com card.cs) ───────────────────────
    private static Color FrameColor(HotPotatoCardType cardType)
    {
        switch (cardType)
        {
            case HotPotatoCardType.NP3:      return scene.Hex("#FF274F");
            case HotPotatoCardType.Atestado: return scene.Hex("#FFD700");
            case HotPotatoCardType.GPT:      return scene.Hex("#26A7FF");
            case HotPotatoCardType.Monster:  return scene.Hex("#27E36F");
            default:                          return Color.white;
        }
    }

    public void SetRoot(RectTransform root)
    {
        canvasRoot = root;
    }

    public void SetFont(Font uiFont)
    {
        font = uiFont;
    }

    public void SetCardType(HotPotatoCardType cardType)
    {
        currentCardType = cardType;
        // Se o tooltip já está visível, atualiza em tempo real
        if (tooltipObject != null)
        {
            HideTooltip();
            if (currentCardType != HotPotatoCardType.None)
                ShowTooltip();
        }
    }

    // ── IPointerEnterHandler / IPointerExitHandler ───────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentCardType != HotPotatoCardType.None)
            ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private void OnDestroy()
    {
        HideTooltip();
    }

    // ── Lógica de exibição ───────────────────────────────────────────────────

    private void ShowTooltip()
    {
        if (canvasRoot == null || font == null || tooltipObject != null) return;

        string desc = ShortDescription(currentCardType);
        if (string.IsNullOrEmpty(desc)) return;

        Color accent = FrameColor(currentCardType);

        // Posição: converter posição do ícone para espaço do canvas
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, transform.position);
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRoot, screenPos, null, out localPos);

        // Cria o objeto de tooltip
        tooltipObject = scene.CreateUIObject("SideCardTooltip", canvasRoot);
        RectTransform rect = tooltipObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot     = new Vector2(0.5f, 0f);
        rect.anchoredPosition = localPos + new Vector2(0f, 28f);
        rect.sizeDelta = new Vector2(200f, 42f);
        tooltipObject.transform.SetAsLastSibling();

        // Fundo arredondado escuro com borda colorida
        Image bg = tooltipObject.AddComponent<Image>();
        bg.sprite = scene.CreateRoundedRectSprite(200, 42, 10,
            new Color(0.05f, 0.06f, 0.12f, 0.92f),
            accent, 2);
        bg.type = Image.Type.Sliced;
        bg.color = Color.white;
        bg.raycastTarget = false;

        // Texto de descrição
        GameObject textObj = scene.CreateUIObject("TooltipText", tooltipObject.transform);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        scene.Stretch(textRect);
        textRect.offsetMin = new Vector2(8f, 2f);
        textRect.offsetMax = new Vector2(-8f, -2f);
        Text text = scene.CreateText(textObj, desc, 14, font,
            TextAnchor.MiddleCenter, Color.white);
        text.fontStyle = FontStyle.Bold;
        text.raycastTarget = false;
    }

    private void HideTooltip()
    {
        if (tooltipObject != null)
        {
            Destroy(tooltipObject);
            tooltipObject = null;
        }
    }
}
