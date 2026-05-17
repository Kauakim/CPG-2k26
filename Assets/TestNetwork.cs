// HPNetworkDebug.cs — só para testes, delete depois
using UnityEngine;

public class HPNetworkDebug : MonoBehaviour
{
    [SerializeField] private string forceRoomCode = ""; // deixe vazio no host

    private void Awake()
    {
        if (!string.IsNullOrEmpty(forceRoomCode))
            PlayerPrefs.SetString("debug_room", forceRoomCode);
        else
            PlayerPrefs.DeleteKey("debug_room");
    }
}