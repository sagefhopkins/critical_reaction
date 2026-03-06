using UnityEngine;
using System.Collections.Generic;

namespace Gameplay.Analytics
{
    public class GameplayTelemetry : MonoBehaviour
    {
        public static GameplayTelemetry Instance { get; private set; }

        private Dictionary<string, int> stationInteractionCounts = new();
        private Dictionary<string, float> stationActiveTime = new();
        private Dictionary<ulong, Dictionary<string, int>> playerTaskCounts = new();

        private float levelStartTime;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            levelStartTime = Time.time;
        }

        public void LogStationInteraction(string stationType, ulong playerId)
        {
            if (!stationInteractionCounts.ContainsKey(stationType)) stationInteractionCounts[stationType] = 0;

            stationInteractionCounts[stationType]++;

            if (!playerTaskCounts.ContainsKey(playerId)) playerTaskCounts[playerId] = new Dictionary<string, int>();

            if (!playerTaskCounts[playerId].ContainsKey(stationType)) playerTaskCounts[playerId][stationType] = 0;

            playerTaskCounts[playerId][stationType]++;
        }

        public void LogStationActiveTime(string stationType, float duration)
        {
            if (!stationActiveTime.ContainsKey(stationType)) stationActiveTime[stationType] = 0f;

            stationActiveTime[stationType] += duration;
        }

        public void PrintSummary()
        {
            Debug.Log("===== TELEMETRY SUMMARY =====");

            foreach (var kvp in stationInteractionCounts)
                Debug.Log($"{kvp.Key} Interactions: {kvp.Value}");

            foreach (var kvp in stationActiveTime)
                Debug.Log($"{kvp.Key} ActiveTime: {kvp.Value:F2}s");

            foreach (var player in playerTaskCounts)
            {
                Debug.Log($"Player {player.Key} Task Breakdown: ");

                foreach (var task in player.Value)
                    Debug.Log($" {task.Key}: {task.Value}");
            }

            Debug.Log("================");
        }
    }
}


