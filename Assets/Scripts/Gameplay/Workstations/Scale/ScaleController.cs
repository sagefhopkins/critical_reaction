using System;
using Gameplay.Items;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Workstations.Scale
{
    public class ScaleController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Workstation workstation;

        [Header("Settings")]
        [SerializeField] private float workDuration = 5f;

        private void Awake()
        {
            if (workstation == null)
                workstation = GetComponent<Workstation>();
        }

        private void Update()
        {
            if (!IsServer) return;
            if (workstation == null) return;
            if (workstation.CurrentWorkState != WorkState.Working) return;

            float duration = workstation.AssignedRecipe != null
                ? workstation.AssignedRecipe.WorkDuration
                : workDuration;

            float delta = Time.deltaTime / duration;
            workstation.AddProgressServer(delta);

            if (workstation.WorkProgress >= 1f)
            {
                workstation.CompleteWorkServer();
            }
        }
    }
}
