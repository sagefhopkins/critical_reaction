using Gameplay.Workstations;
using UnityEngine;
namespace Gameplay.Workstations
{
    public class BatchRuleEvaluator
    {
        public void Evaluate(Batch batch, ProcessContext context)
        {
            if (context.IsWrongOrder)
                batch.ApplyWaste(WasteReason.WrongOrder, invalidate: true);

            if (context.MissedTimeWindow)
                batch.ApplyWaste(WasteReason.MissedTimeWindow, yieldPenalty: 0.25);

            if (context.MaxTemperature > context.AllowedMaxTemperature)
                batch.ApplyWaste(WasteReason.Overheated, yieldPenalty: 0.5);
        }
    }
}
