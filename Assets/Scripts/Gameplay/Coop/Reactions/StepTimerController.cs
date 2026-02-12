using UnityEngine;
using System;

public class StepTimerController
{
    public CountdownTimer StepTimer { get; private set; }

    private Action _onFail;

    public StepTimerController(float duration, Action onFail)
    {
        _onFail = onFail;
        StepTimer = new CountdownTimer(duration);
        StepTimer.OnTimerComplete += _onFail;
        StepTimer.Start();
    }
    public void Tick(float deltaTime)
    {
        StepTimer.Tick(deltaTime);
    }
    public void Stop()
    {
        StepTimer.OnTimerComplete -= _onFail;
        StepTimer.Stop();
    }
}
