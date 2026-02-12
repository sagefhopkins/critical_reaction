using System;
using UnityEngine;

public class CountdownTimer
{
    public float Duration { get; private set; }
    public float TimeRemaining { get; private set; }
    public bool IsRunning { get; private set; }

    public event Action OnTimerComplete;

    public CountdownTimer(float duration)
    {
        Duration = duration;
        TimeRemaining = duration;
    }
    public void Start()
    {
        IsRunning = true;
    }
    public void Stop()
    {
        IsRunning = false;
    }
    public void Reset()
    {
        TimeRemaining = Duration;
    }
    public void Tick(float deltaTime)
    {
        if (!IsRunning) return;

        TimeRemaining -= deltaTime;
        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            IsRunning = false;
            OnTimerComplete?.Invoke();
        }
    }
    public float NormalizedTime => Mathf.Clamp01(TimeRemaining / Duration);
}