using UnityEngine;
using UnityEngine.UI;

/// Gerencia a UI do lobby e o countdown de inicio.
public class HPLobbyView : MonoBehaviour
{
    [Header("Lobby")]
    [SerializeField] private GameObject lobbyRoot;
    [SerializeField] private Text lobbyText;

    [Header("Countdown")]
    [SerializeField] private GameObject countdownRoot;
    [SerializeField] private Text countdownText;

    private void Awake()
    {
        HideAll();
    }

    public void ShowLobby(int playerCount, int maxPlayers, int countdownSeconds)
    {
        if (lobbyRoot != null) lobbyRoot.SetActive(true);
        if (countdownRoot != null) countdownRoot.SetActive(false);

        if (lobbyText == null) return;

        if (playerCount < 2)
            lobbyText.text = "Aguardando jogadores...\n" + playerCount + "/" + maxPlayers;
        else
            lobbyText.text = "Iniciando em " + countdownSeconds + "s\n" + playerCount + "/" + maxPlayers;
    }

    public void ShowCountdown(int secondsRemaining)
    {
        if (lobbyRoot != null) lobbyRoot.SetActive(false);
        if (countdownRoot != null) countdownRoot.SetActive(true);

        if (countdownText != null)
            countdownText.text = secondsRemaining > 0
                ? secondsRemaining + "..."
                : "Comecar!";
    }

    public void HideAll()
    {
        if (lobbyRoot != null) lobbyRoot.SetActive(false);
        if (countdownRoot != null) countdownRoot.SetActive(false);
    }
}
