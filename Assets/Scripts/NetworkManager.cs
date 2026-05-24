using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.UI;

/// Gerencia conexão Photon com fluxo automático:
/// — Quem inicia o jogo vira host e cria a sala automaticamente.
/// — Quem acessa via link entra direto na sala sem nenhuma interface extra.
/// — Primeiro personagem disponível é atribuído automaticamente a todos.
[RequireComponent(typeof(PhotonView))]
public class HPNetworkManager : MonoBehaviourPunCallbacks
{
    public static HPNetworkManager Instance { get; private set; }

    // ── Configurações ─────────────────────────────────────────────────────────

    [Header("Photon")]
    [SerializeField] private string gameVersion = "1.0";

    [Header("UI — tela de espera (opcional)")]
    [SerializeField] private GameObject waitingPanel;   // exibido enquanto conecta
    [SerializeField] private Text       statusText;     // "Conectando…" / "Aguardando…"
    [SerializeField] private Text       linkText;       // exibe o link compartilhável

    [Header("UI — Código da sala (opcional)")]
    [SerializeField] private Text       roomCodeDisplay; // exibe apenas o código (ex: "XKBR") no canto superior esquerdo

    [Header("Labels de nome sobre cada personagem (opcional)")]
    [SerializeField] private Text[] seatLabels = new Text[8];

    [Header("Referência ao GameManager")]
    [SerializeField] private HPGameManager gameManager;

    // ── Propriedades internas ─────────────────────────────────────────────────

    private const string SEAT_KEY_PREFIX  = "s";
    private const string ROOM_STARTED_KEY = "gs";
    private const string URL_PARAM        = "room";   // ?room=XKBR

    private string pendingRoomCode = null;  // código lido da URL ao conectar

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        PhotonNetwork.GameVersion            = gameVersion;
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.NickName               = "Jogador";

        pendingRoomCode = ReadRoomCodeFromURL();

        Debug.Log($"[Network] Iniciando — modo: {(pendingRoomCode != null ? "CLIENTE (código: " + pendingRoomCode + ")" : "HOST")}");
        Debug.Log("[Network] Tentando conectar ao Photon...");

        SetStatus(pendingRoomCode != null ? "Entrando na sessão..." : "Iniciando sessão...");
        if (waitingPanel != null) waitingPanel.SetActive(true);

        PhotonNetwork.ConnectUsingSettings();
    }

    // ── Callbacks Photon — Conexão ────────────────────────────────────────────

    public override void OnConnectedToMaster()
    {
        Debug.Log("[Network] ✓ Conectado ao servidor Photon. Entrando no lobby...");
        PhotonNetwork.JoinLobby(TypedLobby.Default);
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[Network] ✓ Lobby Photon pronto.");
        if (pendingRoomCode != null)
        {
            Debug.Log($"[Network] Tentando entrar na sala: {pendingRoomCode}");
            JoinRoom(pendingRoomCode);
        }
        else
        {
            Debug.Log("[Network] Criando nova sala...");
            CreateRoom();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[Network] ✗ Desconectado: {cause}");
        SetStatus("Desconectado: " + cause);
    }

    // ── Criar / Entrar ────────────────────────────────────────────────────────

    private void CreateRoom()
    {
        string code = GenerateRoomCode();
        var props   = new Hashtable { { ROOM_STARTED_KEY, false } };
        var options = new RoomOptions
        {
            MaxPlayers                   = 8,
            IsVisible                    = true,
            IsOpen                       = true,
            CustomRoomProperties         = props,
            CustomRoomPropertiesForLobby = new[] { ROOM_STARTED_KEY }
        };
        PhotonNetwork.JoinOrCreateRoom(code, options, TypedLobby.Default);
    }

    private void JoinRoom(string code)
    {
        PhotonNetwork.JoinRoom(code.ToUpper().Trim());
    }

    public override void OnJoinedRoom()
    {
        // Ocupa o primeiro assento livre automaticamente
        ClaimFirstAvailableSeat();

        // Exibe o link compartilhável para o host
        string code = PhotonNetwork.CurrentRoom.Name;
        string link = BuildShareLink(code);
        if (linkText != null) linkText.text = link;
        if (roomCodeDisplay != null) 
        {
            roomCodeDisplay.text = code;
            Debug.Log($"[Network] Código exibido no RoomCodeDisplay: {code}");
        }
        else
        {
            Debug.LogWarning("[Network] ⚠️ roomCodeDisplay está NULL! Verifique o Inspector do NetworkManager!");
        }

        SetStatus("Na sessão — código: " + code);
        RefreshSeats();
        gameManager?.OnNetworkJoinedRoom();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        // Sala não existe ou cheia → cria uma nova
        SetStatus("Sessão não encontrada, criando nova...");
        pendingRoomCode = null;
        CreateRoom();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        SetStatus("Erro ao criar sessão: " + message);
    }

    // ── Callbacks de sala ─────────────────────────────────────────────────────

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        RefreshSeats();
        if (PhotonNetwork.IsMasterClient)
            gameManager?.OnNetworkPlayerCountChanged(PhotonNetwork.CurrentRoom.PlayerCount);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RefreshSeats();
        if (PhotonNetwork.IsMasterClient)
            gameManager?.OnNetworkPlayerCountChanged(PhotonNetwork.CurrentRoom.PlayerCount);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
            gameManager?.OnBecameMaster();
    }

    public override void OnRoomPropertiesUpdate(Hashtable props)  => RefreshSeats();
    public override void OnPlayerPropertiesUpdate(Player p, Hashtable props) => RefreshSeats();

    // ── Assentos ──────────────────────────────────────────────────────────────

    private void ClaimFirstAvailableSeat()
    {
        // Libera assento anterior antes de pegar novo
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
            return;
        }
    }

    public int GetSeatOwner(int idx)
    {
        string key = SEAT_KEY_PREFIX + idx;
        foreach (var p in PhotonNetwork.PlayerList)
            if (p.CustomProperties.TryGetValue(key, out object v) && v is bool b && b)
                return p.ActorNumber;
        return 0;
    }

    public int GetLocalSeatIndex()
    {
        for (int i = 0; i < 8; i++)
        {
            string key = SEAT_KEY_PREFIX + i;
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(key, out object v)
                && v is bool b && b)
                return i;
        }
        return -1;
    }

    public Player GetPlayerAtSeat(int idx)
    {
        int actor = GetSeatOwner(idx);
        if (actor == 0) return null;
        foreach (var p in PhotonNetwork.PlayerList)
            if (p.ActorNumber == actor) return p;
        return null;
    }

    public int OccupiedSeatCount()
    {
        int c = 0;
        for (int i = 0; i < 8; i++) if (GetSeatOwner(i) != 0) c++;
        return c;
    }

    public void MarkGameStarted()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        PhotonNetwork.CurrentRoom.SetCustomProperties(
            new Hashtable { { ROOM_STARTED_KEY, true } });
        PhotonNetwork.CurrentRoom.IsOpen = false;
    }

    public bool IsGameStarted()
    {
        if (PhotonNetwork.CurrentRoom == null) return false;
        return PhotonNetwork.CurrentRoom.CustomProperties
            .TryGetValue(ROOM_STARTED_KEY, out object v) && v is bool b && b;
    }

    // ── Refresh UI ────────────────────────────────────────────────────────────

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

    // ── URL / Link ────────────────────────────────────────────────────────────

    /// Lê o código da sala a partir do parâmetro ?room=XXXX da URL (WebGL).
    /// Em builds standalone retorna null (o host sempre cria).
    private static string ReadRoomCodeFromURL()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            string url = Application.absoluteURL;
            int idx = url.IndexOf("?" + URL_PARAM + "=");
            if (idx < 0) idx = url.IndexOf("&" + URL_PARAM + "=");
            if (idx < 0) return null;
            int start = url.IndexOf('=', idx) + 1;
            int end   = url.IndexOf('&', start);
            return end < 0 ? url.Substring(start) : url.Substring(start, end - start);
        }
        catch { return null; }
#else
        // PlayerPrefs — usado no Editor para simular um segundo jogador
        string debugRoom = PlayerPrefs.GetString("debug_room", "");
        if (!string.IsNullOrEmpty(debugRoom)) return debugRoom;

        // Argumento de linha de comando: game.exe -room XXXX
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-room") return args[i + 1];

        return null;
#endif
    }

    /// Monta o link compartilhável com o código da sala.
    private static string BuildShareLink(string code)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string url = Application.absoluteURL;
        int q = url.IndexOf('?');
        string base_url = q >= 0 ? url.Substring(0, q) : url;
        return base_url + "?" + URL_PARAM + "=" + code;
#else
        return "Código da sessão: " + code;
#endif
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var code = new System.Text.StringBuilder(4);
        for (int i = 0; i < 4; i++)
            code.Append(chars[Random.Range(0, chars.Length)]);
        return code.ToString();
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log("[Network] " + msg);
    }
}