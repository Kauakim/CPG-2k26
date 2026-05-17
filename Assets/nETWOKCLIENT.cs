using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.UI;

/// Versão cliente do NetworkManager — apenas entra em salas existentes, nunca cria.
/// Use este script no clone/segundo jogador. O host continua usando HPNetworkManager.
[RequireComponent(typeof(PhotonView))]
public class HPNetworkClient : MonoBehaviourPunCallbacks
{
    [Header("Referência ao GameManager")]
    [SerializeField] private HPGameManager gameManager;

    [Header("UI (opcional)")]
    [SerializeField] private Text statusText;
    [SerializeField] private Text linkText;

    [Header("Labels de nome sobre cada personagem (opcional)")]
    [SerializeField] private Text[] seatLabels = new Text[8];

    private const string SEAT_KEY_PREFIX  = "s";
    private const string ROOM_STARTED_KEY = "gs";
    private const string URL_PARAM        = "room";

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        PhotonNetwork.GameVersion            = "1.0";
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.NickName               = "Jogador";

        string code = ReadRoomCode();

        if (string.IsNullOrEmpty(code))
        {
            Debug.LogError("[Client] Nenhum código de sala encontrado. Configure o HPNetworkDebug com o código.");
            SetStatus("Erro: código de sala não encontrado.");
            return;
        }

        Debug.Log($"[Client] Conectando para entrar na sala: {code}");
        SetStatus("Conectando...");
        PhotonNetwork.ConnectUsingSettings();
    }

    // ── Callbacks ─────────────────────────────────────────────────────────────

    public override void OnConnectedToMaster()
    {
        Debug.Log("[Client] ✓ Conectado ao Photon. Entrando no lobby...");
        PhotonNetwork.JoinLobby(TypedLobby.Default);
    }

    public override void OnJoinedLobby()
    {
        string code = ReadRoomCode();
        Debug.Log($"[Client] ✓ Lobby pronto. Tentando entrar na sala: {code}");
        SetStatus("Entrando na sessão...");
        PhotonNetwork.JoinRoom(code.ToUpper().Trim());
    }

    public override void OnJoinedRoom()
    {
        string code = PhotonNetwork.CurrentRoom.Name;
        Debug.Log($"[Client] ✓ Entrou na sala {code}. Jogadores: {PhotonNetwork.CurrentRoom.PlayerCount}");
        SetStatus("Na sessão — código: " + code);

        ClaimFirstAvailableSeat();
        RefreshSeats();
        gameManager?.OnNetworkJoinedRoom();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        // Cliente NUNCA cria sala — apenas reporta o erro
        Debug.LogError($"[Client] ✗ Falha ao entrar na sala ({returnCode}): {message}");
        SetStatus("Sessão não encontrada. Verifique o código.");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[Client] ✗ Desconectado: {cause}");
        SetStatus("Desconectado: " + cause);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[Client] Jogador entrou: {newPlayer.NickName}. Total: {PhotonNetwork.CurrentRoom.PlayerCount}");
        RefreshSeats();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[Client] Jogador saiu: {otherPlayer.NickName}. Total: {PhotonNetwork.CurrentRoom.PlayerCount}");
        RefreshSeats();
    }

    public override void OnRoomPropertiesUpdate(Hashtable props) => RefreshSeats();
    public override void OnPlayerPropertiesUpdate(Player p, Hashtable props) => RefreshSeats();

    // ── Assentos ──────────────────────────────────────────────────────────────

    private void ClaimFirstAvailableSeat()
    {
        var release = new Hashtable();
        for (int i = 0; i < 8; i++) release[SEAT_KEY_PREFIX + i] = false;
        PhotonNetwork.LocalPlayer.SetCustomProperties(release);

        for (int i = 0; i < 8; i++)
        {
            if (GetSeatOwner(i) != 0) continue;
            var claim = new Hashtable();
            for (int j = 0; j < 8; j++) claim[SEAT_KEY_PREFIX + j] = false;
            claim[SEAT_KEY_PREFIX + i] = true;
            PhotonNetwork.LocalPlayer.SetCustomProperties(claim);
            Debug.Log($"[Client] Assento {i} ocupado.");
            return;
        }

        Debug.LogWarning("[Client] Nenhum assento disponível.");
    }

    public int GetSeatOwner(int idx)
    {
        string key = SEAT_KEY_PREFIX + idx;
        foreach (var p in PhotonNetwork.PlayerList)
            if (p.CustomProperties.TryGetValue(key, out object v) && v is bool b && b)
                return p.ActorNumber;
        return 0;
    }

    public Player GetPlayerAtSeat(int idx)
    {
        int actor = GetSeatOwner(idx);
        if (actor == 0) return null;
        foreach (var p in PhotonNetwork.PlayerList)
            if (p.ActorNumber == actor) return p;
        return null;
    }

    private void RefreshSeats()
    {
        for (int i = 0; i < seatLabels.Length; i++)
        {
            if (seatLabels[i] == null) continue;
            Player p = GetPlayerAtSeat(i);
            seatLabels[i].text = p != null ? p.NickName : "";
        }
        gameManager?.RefreshCharacterVisuals();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string ReadRoomCode()
    {
        // PlayerPrefs (Editor/clone via HPNetworkDebug)
        string code = PlayerPrefs.GetString("debug_room", "");
        if (!string.IsNullOrEmpty(code)) return code;

        // URL param ?room=XXXX (WebGL)
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            string url = Application.absoluteURL;
            int idx = url.IndexOf("?" + URL_PARAM + "=");
            if (idx < 0) idx = url.IndexOf("&" + URL_PARAM + "=");
            if (idx >= 0)
            {
                int start = url.IndexOf('=', idx) + 1;
                int end   = url.IndexOf('&', start);
                return end < 0 ? url.Substring(start) : url.Substring(start, end - start);
            }
        }
        catch { }
#endif
        // Arg de linha de comando: game.exe -room XXXX
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-room") return args[i + 1];

        return "";
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}