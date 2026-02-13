using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
<<<<<<< Updated upstream
using Gameplay.Workstations;

namespace Gameplay.Workstations
{
    public class DeliveryService
    {
        public double CalculateDeliverableQuantity(IEnumerable<Batch> batches)
        {
            if (batches == null)
            {
                return batches
                .Where(b => b != null && b.IsDeliverable)
                .Sum(b => b.ActualYield);
            }
        }
        public void ValidateBeforeDispatch(IEnumerable<Batch> batches)
        {
            
            var invalid = batches
                .Where(b => b != null && b.Status == BatchStatus.Waste)
                .ToList();

            if (invalid.Any())
            {
                throw new InvalidOperationException($"Cannot deliver {invalid.Count} waste batch(es).");
            }
               
        }
    }
}

=======

public class DeliveryService
{
    public double CalculateDeliverableQuantity(IEnumerable<Batch> batches)
    {
        return batches
            .Where(b => b.IsDeliverable)
            .Sum(b => b.ActualYield);
    }
    public void ValidateBeforeDispatch(IEnumerable<Batch> batches)
    {
        var invalid = batches
            .Where(b => b.Status == BatchStatus.Waste)
            .ToList();

        if (invalid.Any())
            throw new InvalidOperationException($"Cannot deliver {invalid.Count} waste batch(es).");
    }
}
>>>>>>> Stashed changes
