using UnityEngine;
using UnityEngine.UI;

/// Gerencia a UI do lobby (texto de espera) e o countdown de início.
/// Todos os elementos são atribuídos via Inspector.
public class HPLobbyView : MonoBehaviour
{
    [Header("Lobby")]
    [SerializeField] private GameObject lobbyRoot;
    [SerializeField] private Text       lobbyText;

    [Header("Countdown")]
    [SerializeField] private GameObject countdownRoot;
    [SerializeField] private Text       countdownText;

    [Header("Botão Adicionar Bot")]
    [SerializeField] private Button addBotButton;

    public Button AddBotButton => addBotButton;

    private void Awake()
    {
        HideAll();
    }

    public void ShowLobby(int playerCount, int maxPlayers, int countdownSeconds, bool canAddBot)
    {
        if (lobbyRoot     != null) lobbyRoot.SetActive(true);
        if (countdownRoot != null) countdownRoot.SetActive(false);
        if (addBotButton  != null)
        {
            addBotButton.gameObject.SetActive(true);
            addBotButton.interactable = canAddBot;
        }

        if (lobbyText == null) return;

        if (playerCount < 2)
            lobbyText.text = "Aguardando jogadores...\n" + playerCount + "/" + maxPlayers;
        else
            lobbyText.text = "Iniciando em " + countdownSeconds + "s\n" + playerCount + "/" + maxPlayers;
    }

    public void ShowCountdown(int secondsRemaining)
    {
        if (lobbyRoot     != null) lobbyRoot.SetActive(false);
        if (countdownRoot != null) countdownRoot.SetActive(true);
        if (addBotButton  != null) addBotButton.gameObject.SetActive(false);
        if (countdownText != null)
            countdownText.text = secondsRemaining > 0 ? secondsRemaining + "..." : "Começar!";
    }

    public void HideAll()
    {
        if (lobbyRoot     != null) lobbyRoot.SetActive(false);
        if (countdownRoot != null) countdownRoot.SetActive(false);
        if (addBotButton  != null) addBotButton.gameObject.SetActive(false);
    }
}
