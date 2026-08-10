using System.Collections.Generic;
using UnityEngine;

public class next : MonoBehaviour
{
    public int GetNextAliveIndex(List<HotPotatoPlayer> players, int currentIndex)
    {
        if (players == null || players.Count == 0)
        {
            return -1;
        }

        for (int step = 1; step <= players.Count; step++)
        {
            int index = (currentIndex + step + players.Count) % players.Count;
            if (players[index].Alive)
            {
                return index;
            }
        }

        return -1;
    }
}
