using UnityEngine;
using UnityEngine.UI;

/// Overlay de resultado final. Todos os elementos são atribuídos via Inspector.
public class HPResultView : MonoBehaviour
{
    [SerializeField] private GameObject overlay;
    [SerializeField] private Text       winnerText;
    [SerializeField] private Button     restartButton;

    private void Awake()
    {
        Hide();
    }

    public void SetRestartAction(UnityEngine.Events.UnityAction action)
    {
        if (restartButton == null) return;
        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(action);
    }

    public void Show(string winnerName)
    {
        if (overlay    != null) overlay.SetActive(true);
        if (winnerText != null) winnerText.text = "Vitória de " + winnerName;
        if (restartButton != null) restartButton.Select();
    }

    public void Hide()
    {
        if (overlay != null) overlay.SetActive(false);
    }
}
