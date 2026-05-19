using UnityEngine;

public class HPDebugJoin : MonoBehaviour
{
    [SerializeField] private string roomCode = "";

    private void Awake()
    {
        if (!string.IsNullOrEmpty(roomCode))
            PlayerPrefs.SetString("debug_room", roomCode);
        else
            PlayerPrefs.DeleteKey("debug_room");

        PlayerPrefs.Save(); // força gravação imediata
    }
}