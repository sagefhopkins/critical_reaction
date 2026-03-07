using UnityEngine;
using System.Collections.Generic;

public class StationLock : MonoBehaviour
{
    public int maxUsers = 1;

    private List<GameObject> currentUsers = new List<GameObject>();
    private Queue<GameObject> waitingQueue = new Queue<GameObject>();

    public bool TryAcquire(GameObject player)
    {
        if (currentUsers.Contains(player))
            return true;

        if (currentUsers.Count < maxUsers)
        {
            currentUsers.Add(player);
            return true;
        }
        if (!waitingQueue.Contains(player))
            waitingQueue.Enqueue(player);

        return false;
    }

    public void Release(GameObject player)
    {
        if (currentUsers.Contains(player))
        {
            currentUsers.Remove(player);
        }
        if (waitingQueue.Count > 0 && currentUsers.Count < maxUsers)
        {
            GameObject nextPlayer = waitingQueue.Dequeue();
            currentUsers.Add(nextPlayer);
        }
    }

    public bool IsUser(GameObject player)
    {
        return currentUsers.Contains(player);
    }
}
