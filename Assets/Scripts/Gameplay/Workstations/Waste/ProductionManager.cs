using JetBrains.Annotations;
using UnityEngine;

namespace Gameplay.Workstations
{
    public class ProductionManager : MonoBehaviour
    {
        public static ProductionManager Instance { get; private set; }
        private BatchRuleEvaluator evaluator = new BatchRuleEvaluator();

        private Batch currentBatch;
        private ProcessContext context;

        private void Awake()
        {

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        public void StartBatch(double plannedYield)
        {
            currentBatch = new Batch(plannedYield, System.DateTime.UtcNow);
            context = new ProcessContext();
        }
        
        public void ReportWrongOrder()
        {
            context.IsWrongOrder = true;
        }
        public void ReportMissedTimeWindow()
        {
            context.MissedTimeWindow = true;
        }
        public void ReportTemperature(double maxTemp, double allowedMax)
        {
            context.MaxTemperature = maxTemp;
            context.AllowedMaxTemperature = allowedMax;
        }
        public Batch CompleteBatch()
        { 
            evaluator.Evaluate(currentBatch, context);
            currentBatch.MarkCompleted();

            return currentBatch;
        }
    }
}
