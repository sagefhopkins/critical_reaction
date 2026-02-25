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
<<<<<<< HEAD

        All = WrongOrder | MissedTimeWindow | Overheated
=======
>>>>>>> b0ce7ae1bff843602fba15f53fc0d593c9880ef2
    }
}
