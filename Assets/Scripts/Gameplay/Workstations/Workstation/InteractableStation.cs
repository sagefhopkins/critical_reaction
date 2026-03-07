using UnityEngine;

public class InteractableStation : MonoBehaviour
{
    public StationLock stationLock;
    void Awake()
    {
        stationLock = GetComponent<StationLock>();
    }

    public void Interact(GameObject player)
    {
        if (!stationLock.IsUser(player))
        {
            bool acquired = stationLock.TryAcquire(player);

            if (!acquired)
            {
                Debug.Log("Station busy, player queued");
                return;
            }
        }

        StartStationAction(player);
    }

    void StartStationAction(GameObject player)
    {
        Debug.Log(player.name + "started using the station");
    }
}

