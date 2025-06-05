using Ardalis.Specification;
using System;
using TNK.Core.ServiceManagementAggregate.Entities; // Assuming AvailabilitySlot is here

namespace TNK.UseCases.AvailabilitySlots.Specifications;

public class SlotByIdAndWorkerSpec : Specification<AvailabilitySlot>, ISingleResultSpecification<AvailabilitySlot>
{
  public SlotByIdAndWorkerSpec(Guid availabilitySlotId, Guid workerId, int businessProfileId)
  {
    Query
        .Where(slot => slot.Id == availabilitySlotId &&
                       slot.WorkerId == workerId &&
                       slot.BusinessProfileId == businessProfileId);
  }
}
