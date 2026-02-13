using UnityEngine;
using System;

<<<<<<< Updated upstream
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
=======
[Flags]
public enum WasteReason
{
    None = 0,
    WrongOrder = 1,
    MissedTimeWindow = 2,
    Overheated = 4
>>>>>>> Stashed changes
}
