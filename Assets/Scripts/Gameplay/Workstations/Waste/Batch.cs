using UnityEngine;
using System;

namespace Gameplay.Workstations
{
    public class Batch
    {
        public readonly Guid Id = Guid.NewGuid();
        public BatchStatus Status { get; private set; } = BatchStatus.Valid;
        public WasteReason WasteReasons { get; private set; } = WasteReason.None;

        public readonly double PlannedYield;
        public double ActualYield { get; private set; }

        public readonly DateTime StartTime;
        public DateTime? EndTime { get; private set; }

        public Batch(double plannedYield, DateTime startTime)
        {
            PlannedYield = plannedYield;
            ActualYield = plannedYield;
            StartTime = startTime;
        }
        public bool IsDeliverable => Status == BatchStatus.Valid || Status == BatchStatus.Degraded;

        public bool IsCompleted => EndTime.HasValue;

        internal void MarkCompleted()
        {
            EndTime = DateTime.UtcNow;
        }
        internal void ApplyWaste(
            WasteReason reason,
            bool invalidate = false,
            double yieldPenalty = 0)
        {
            if (Status == BatchStatus.Waste)
                return;
           
            WasteReasons |= reason;

            if (invalidate)
            {
                Status = BatchStatus.Waste;
                ActualYield = 0;
                return;
            }
            if (yieldPenalty > 0)
            {
                Status = BatchStatus.Degraded;
                ActualYield *= (1 - yieldPenalty);
                ActualYield = Math.Max(0, ActualYield);
            }
        }
    }
}
