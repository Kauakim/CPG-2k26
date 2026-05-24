using UnityEngine;

public enum HPPhase { Lobby, LobbyCountdown, Playing, GameOver }

public enum HPCardType { None, NP3, Atestado, Monster, GPT, Calculadora }

public static class HPCardInfo
{
    public static string DisplayName(HPCardType t)
    {
        switch (t)
        {
            case HPCardType.NP3:      return "NP3";
            case HPCardType.Atestado: return "Atestado";
            case HPCardType.Monster:  return "Monster";
            case HPCardType.GPT:      return "GPT";
            case HPCardType.Calculadora: return "Calculadora";
            default:                  return "";
        }
    }

    public static Color FrameColor(HPCardType t)
    {
        switch (t)
        {
            case HPCardType.NP3:      return Hex("#FF274F");
            case HPCardType.Atestado: return Hex("#FFD700");
            case HPCardType.GPT:      return Hex("#26A7FF");
            case HPCardType.Monster:  return Hex("#27E36F");
            case HPCardType.Calculadora: return Hex("#B8B8B8");
            default:                  return Color.white;
        }
    }

    public static Color Hex(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString(hex, out c);
        return c;
    }
}

public class HPPlayer
{
    public string Name;
    public bool IsLocal;
    public int Lives = 3;
    public HPCardType HeldCard  = HPCardType.None;
    public HPCardType HeldCard2 = HPCardType.None;
    public bool Eliminated;
    public HPSeatView View;

    public bool Alive => Lives > 0 && !Eliminated;
}
