using UnityEngine;
using UnityEngine.UI;

public class result : MonoBehaviour
{
    private GameObject overlay;
    private Text winnerText;
    private Button restartButton;

    public void Initialize(RectTransform root, Font font, UnityEngine.Events.UnityAction restartAction)
    {
        overlay = scene.CreateUIObject("ResultOverlay", root);
        RectTransform rect = overlay.GetComponent<RectTransform>();
        scene.Stretch(rect);

        Image background = overlay.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.72f);

        GameObject titleObject = scene.CreateUIObject("WinnerTitle", overlay.transform);
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 96f);
        titleRect.sizeDelta = new Vector2(900f, 90f);
        winnerText = scene.CreateText(titleObject, "", 50, font, TextAnchor.MiddleCenter, Color.white);
        winnerText.fontStyle = FontStyle.Bold;
        winnerText.GetComponent<Shadow>().effectColor = scene.Hex("#ffdf4d");

        GameObject buttonObject = scene.CreateButton("RestartButton", overlay.transform, "Jogar Novamente", font, restartAction);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -18f);
        buttonRect.sizeDelta = new Vector2(260f, 58f);
        restartButton = buttonObject.GetComponent<Button>();

        Hide();
    }

    public void Show(string winnerName)
    {
        if (overlay == null)
        {
            return;
        }

        winnerText.text = "Vit\u00f3ria de " + winnerName;
        overlay.SetActive(true);
        restartButton.Select();
    }

    public void Hide()
    {
        if (overlay != null)
        {
            overlay.SetActive(false);
        }
    }
}
