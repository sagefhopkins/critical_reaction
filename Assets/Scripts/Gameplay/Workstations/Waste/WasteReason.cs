using UnityEngine;
using System;

namespace Gameplay.Workstations
{
    [Flags]
    public enum WasteReason
    {
        None = 0,
        WrongOrder = 1,
        MissedTimeWindow = 2,
        Overheated = 4,

        All = WrongOrder | MissedTimeWindow | Overheated
    }
}
