using UnityEngine;

namespace Gameplay.Workstations
{
    public class ProcessContext
    {
        public bool IsWrongOrder { get; set; }
        public bool MissedTimeWindow { get; set; }
        public double MaxTemperature { get; set; }
        public double AllowedMaxTemperature { get; set; }
    }
}

