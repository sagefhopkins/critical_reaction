using UnityEngine;
namespace Gameplay.Workstations
{
    public class ProcessContext
    {
        public bool IsWrongOrder { get; set; } = false;
        public bool MissedTimeWindow { get; set; } = false;
        public double MaxTemperature { get; set; } = 0;
        public double AllowedMaxTemperature { get; set; } = 100;

        public override string ToString()
        {
            return $"IsWrongOrder: {IsWrongOrder}, MissedTimeWindow: {MissedTimeWindow}, MaxTemp: {MaxTemperature}, AllowedMax: {AllowedMaxTemperature}";
        }
    }
}


