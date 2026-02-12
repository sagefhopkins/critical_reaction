using UnityEngine;

public class DeliveryTimerController : MonoBehaviour
{
    public CountdownTimer GlobalTimer { get; private set; }

    private float deliveryTimeLimit = 120f;

    void Start()
    {
        GlobalTimer.Tick(Time.deltaTime);
    }
    private void HandleDeliveryFailed()
    {
        Debug.Log("Delivery failed - time expired.");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
