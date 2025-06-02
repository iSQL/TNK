using Ardalis.GuardClauses;
using Ardalis.SharedKernel;
using TNK.Core.BusinessAggregate;

namespace TNK.Core.ServiceManagementAggregate.Entities;

// Represents a scheduling template or a set of rules for a worker's availability.
public class Schedule : EntityBase<Guid>, IAggregateRoot
{
  public Guid WorkerId { get; private set; }
  public virtual Worker? Worker { get; private set; }

  public int BusinessProfileId { get; private set; } // Denormalized for easier vendor-specific queries
  public virtual BusinessProfile? BusinessProfile { get; private set; }

  public string Title { get; private set; } = string.Empty; // e.g., "Standard Weekly Schedule", "Summer Hours"
  public bool IsDefault { get; private set; } // Indicates if this is the default schedule for the worker
  public DateOnly EffectiveStartDate { get; private set; }
  public DateOnly? EffectiveEndDate { get; private set; } // Nullable for ongoing schedules
  public string TimeZoneId { get; private set; } = "Europe/Belgrade"; // e.g., "Europe/Belgrade", "UTC". Store TimeZoneInfo.Id

  public virtual ICollection<ScheduleRuleItem> RuleItems { get; private set; } = new List<ScheduleRuleItem>();
  public virtual ICollection<ScheduleOverride> Overrides { get; private set; } = new List<ScheduleOverride>();


  private Schedule() { }

  public Schedule(Guid workerId, int businessProfileId, string title, DateOnly effectiveStartDate, string timeZoneId, bool isDefault = false)
  {
    WorkerId = Guard.Against.Default(workerId, nameof(workerId));
    BusinessProfileId = Guard.Against.Default(businessProfileId, nameof(businessProfileId));
    Title = Guard.Against.NullOrWhiteSpace(title, nameof(title));
    EffectiveStartDate = Guard.Against.Default(effectiveStartDate, nameof(effectiveStartDate));
    TimeZoneId = Guard.Against.NullOrWhiteSpace(timeZoneId, nameof(timeZoneId)); // Validate against known TimeZoneInfo.GetSystemTimeZones() if possible
    IsDefault = isDefault;
  }

  public void UpdateInfo(string title, DateOnly effectiveStartDate, DateOnly? effectiveEndDate, string timeZoneId, bool isDefault)
  {
    Title = Guard.Against.NullOrWhiteSpace(title, nameof(title));
    EffectiveStartDate = Guard.Against.Default(effectiveStartDate, nameof(effectiveStartDate));
    EffectiveEndDate = effectiveEndDate; // Can be null
    TimeZoneId = Guard.Against.NullOrWhiteSpace(timeZoneId, nameof(timeZoneId));
    IsDefault = isDefault;

    if (EffectiveEndDate.HasValue && EffectiveEndDate.Value < EffectiveStartDate)
    {
      throw new ArgumentException("Effective end date cannot be before effective start date.");
    }
  }
  /// <summary>
  /// Removes a rule item from this schedule.
  /// </summary>
  /// <param name="scheduleRuleItemId">The ID of the rule item to remove.</param>
  /// <returns>True if the item was found and removed, false otherwise.</returns>
  public bool RemoveRuleItem(Guid scheduleRuleItemId)
  {
    var ruleItemToRemove = RuleItems.FirstOrDefault(ri => ri.Id == scheduleRuleItemId);
    if (ruleItemToRemove != null)
    {
      RuleItems.Remove(ruleItemToRemove);
      // If ScheduleRuleItem had any dependent owned entities that need cleanup beyond what EF Core handles by removing from collection,
      // that logic could go here or be handled by EF Core's cascade settings for owned types.
      // For BreakRules, since they are part of ScheduleRuleItem, removing ScheduleRuleItem should also remove its breaks
      // if ScheduleRuleItem is configured as the principal in that relationship with cascade delete for its Breaks.
      return true;
    }
    return false;
  }

  public void SetAsDefault(bool isDefault) => IsDefault = isDefault;

  public void AddRuleItem(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, bool isWorkingDay = true)
  {
    Guard.Against.InvalidInput(startTime, nameof(startTime), st => st < endTime, "Start time must be before end time.");
    var newItem = new ScheduleRuleItem(this.Id, dayOfWeek, startTime, endTime, isWorkingDay);
    RuleItems.Add(newItem);
  }

  public void AddOverride(DateOnly date, string reason, bool isWorkingDay, TimeOnly? startTime = null, TimeOnly? endTime = null)
  {
    var newOverride = new ScheduleOverride(this.Id, date, reason, isWorkingDay, startTime, endTime);
    Overrides.Add(newOverride);
  }
}

// Represents a single rule within a schedule template (e.g., Monday 9-5)
public class ScheduleRuleItem : EntityBase<Guid> // Owned by Schedule or separate small entity
{
  public Guid ScheduleId { get; private set; }
  public virtual Schedule? Schedule { get; private set; }

  public DayOfWeek DayOfWeek { get; private set; }
  public TimeOnly StartTime { get; private set; } // e.g., 09:00
  public TimeOnly EndTime { get; private set; }   // e.g., 17:00
  public bool IsWorkingDay { get; private set; } // True if working, false if day off by rule

  public virtual ICollection<BreakRule> Breaks { get; private set; } = new List<BreakRule>();

  private ScheduleRuleItem() { }

  public ScheduleRuleItem(Guid scheduleId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, bool isWorkingDay)
  {
    ScheduleId = Guard.Against.Default(scheduleId, nameof(scheduleId));
    DayOfWeek = dayOfWeek; // Enum, no null check needed
    StartTime = startTime; // Default/struct, no null check
    EndTime = endTime;     // Default/struct, no null check
    IsWorkingDay = isWorkingDay;

    if (isWorkingDay && startTime >= endTime)
    {
      throw new ArgumentException("For a working day, start time must be before end time.");
    }
  }
  /// <summary>
  /// Updates the details of this schedule rule item.
  /// DayOfWeek is not updatable here; manage that by deleting and re-adding if necessary.
  /// </summary>
  public void UpdateDetails(TimeOnly newStartTime, TimeOnly newEndTime, bool newIsWorkingDay)
  {
    IsWorkingDay = newIsWorkingDay;
    if (newIsWorkingDay)
    {
      Guard.Against.InvalidInput(newStartTime, nameof(newStartTime), st => st < newEndTime, "For a working day, start time must be before end time.");
      StartTime = newStartTime;
      EndTime = newEndTime;
    }
    else
    {
      // Convention for non-working days
      StartTime = TimeOnly.MinValue;
      EndTime = TimeOnly.MinValue;
    }
    // Note: Managing breaks (add/update/remove) within this rule item
    // would typically be done through separate methods or by directly manipulating the Breaks collection
    // followed by saving the aggregate root (Schedule).
  }

  public void AddBreak(string breakName, TimeOnly breakStartTime, TimeOnly breakEndTime)
  {
    Guard.Against.NullOrWhiteSpace(breakName, nameof(breakName));
    if (!IsWorkingDay)
    {
      throw new InvalidOperationException("Breaks can only be added to a working day rule item.");
    }
    Guard.Against.InvalidInput(breakStartTime, nameof(breakStartTime), bst => bst >= this.StartTime && bst < this.EndTime, "Break must be within the working hours of the rule item.");
    Guard.Against.InvalidInput(breakEndTime, nameof(breakEndTime), bet => bet <= this.EndTime && bet > this.StartTime, "Break must be within the working hours of the rule item.");
    Guard.Against.InvalidInput(breakStartTime, nameof(breakStartTime), bst => bst < breakEndTime, "Break start time must be before break end time.");

    // Check for overlapping breaks
    foreach (var existingBreak in Breaks)
    {
      if (breakStartTime < existingBreak.EndTime && breakEndTime > existingBreak.StartTime)
      {
        throw new ArgumentException("The new break overlaps with an existing break.");
      }
    }

    var newBreak = new BreakRule(this.Id, breakName, breakStartTime, breakEndTime); // Assumes BreakRule constructor takes ScheduleRuleItemId
    Breaks.Add(newBreak);
  }
  /// <summary>
  /// Removes eak from this schedule rule item.
  /// </summary>
  /// <param name="breakRuleId"></param>
  public void RemoveBreak(Guid breakRuleId)
  {
    var breakToRemove = Breaks.FirstOrDefault(b => b.Id == breakRuleId);
    if (breakToRemove != null)
    {
      Breaks.Remove(breakToRemove);
    }
  }

}

// Represents a break within a ScheduleRuleItem
public class BreakRule : EntityBase<Guid> // Owned by ScheduleRuleItem
{
  public Guid ScheduleRuleItemId { get; private set; }
  public virtual ScheduleRuleItem? ScheduleRuleItem { get; private set; }
  public string Name { get; private set; } = string.Empty; // e.g., "Lunch Break"
  public TimeOnly StartTime { get; private set; }
  public TimeOnly EndTime { get; private set; }

  private BreakRule() { }
  public BreakRule(Guid scheduleRuleItemId, string name, TimeOnly startTime, TimeOnly endTime)
  {
    ScheduleRuleItemId = Guard.Against.Default(scheduleRuleItemId, nameof(scheduleRuleItemId));
    Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
    StartTime = startTime;
    EndTime = endTime;
    if (startTime >= endTime)
    {
      throw new ArgumentException("Break start time must be before end time.");
    }
  }
  public void UpdateDetails(string newName, TimeOnly newStartTime, TimeOnly newEndTime)
  {
    Name = Guard.Against.NullOrWhiteSpace(newName, nameof(newName));
    Guard.Against.InvalidInput(newStartTime, nameof(newStartTime), st => st < newEndTime, "Break start time must be before break end time.");

    StartTime = newStartTime;
    EndTime = newEndTime;
  }
}


// Represents a one-off override to the schedule (e.g., holiday, special working day)
public class ScheduleOverride : EntityBase<Guid> // Owned by Schedule
{
  public Guid ScheduleId { get; private set; }
  public virtual Schedule? Schedule { get; private set; }

  public DateOnly OverrideDate { get; private set; }
  public string Reason { get; private set; } = string.Empty; // e.g., "Public Holiday", "Special Event"
  public bool IsWorkingDay { get; private set; } // If false, it's a day off. If true, it's a working day (could be special hours)
  public TimeOnly? StartTime { get; private set; } // Optional: if IsWorkingDay is true, these can specify different hours
  public TimeOnly? EndTime { get; private set; }   // Optional: if IsWorkingDay is true, these can specify different hours

  public virtual ICollection<BreakRule> Breaks { get; private set; } = new List<BreakRule>(); // Breaks specific to this override day

  private ScheduleOverride() { }

  public ScheduleOverride(Guid scheduleId, DateOnly overrideDate, string reason, bool isWorkingDay, TimeOnly? startTime, TimeOnly? endTime)
  {
    ScheduleId = Guard.Against.Default(scheduleId, nameof(scheduleId));
    OverrideDate = Guard.Against.Default(overrideDate, nameof(overrideDate));
    Reason = Guard.Against.NullOrWhiteSpace(reason, nameof(reason));
    IsWorkingDay = isWorkingDay;
    StartTime = startTime;
    EndTime = endTime;

    if (IsWorkingDay)
    {
      Guard.Against.Null(startTime, nameof(startTime), "Start time is required for a working override day.");
      Guard.Against.Null(endTime, nameof(endTime), "End time is required for a working override day.");
      if (startTime.Value >= endTime.Value)
      {
        throw new ArgumentException("For a working override day, start time must be before end time.");
      }
    }
    else
    {
      if (startTime.HasValue || endTime.HasValue)
      {
        throw new ArgumentException("Start/End time should not be set for a non-working override day.");
      }
    }
  }

  public void AddBreak(string breakName, TimeOnly breakStartTime, TimeOnly breakEndTime)
  {
    if (!IsWorkingDay || !StartTime.HasValue || !EndTime.HasValue)
    {
      throw new InvalidOperationException("Breaks can only be added to working override days with defined start and end times.");
    }
    Guard.Against.InvalidInput(breakStartTime, nameof(breakStartTime), bst => bst < breakEndTime, "Break start time must be before break end time.");
    Guard.Against.InvalidInput(breakStartTime, nameof(breakStartTime), bst => bst >= this.StartTime.Value && bst < this.EndTime.Value, "Break must be within working hours.");
    Guard.Against.InvalidInput(breakEndTime, nameof(breakEndTime), bet => bet <= this.EndTime.Value && bet > this.StartTime.Value, "Break must be within working hours.");

    var newBreak = new BreakRule(this.Id, breakName, breakStartTime, breakEndTime); // Note: BreakRule needs a way to link to ScheduleOverrideId if it's a direct child. For now, using this.Id as a placeholder. This might need adjustment if BreakRule is reused. A specific OverrideBreakRule might be better.
                                                                                    // For simplicity, let's assume BreakRule can be parented by ScheduleRuleItem OR ScheduleOverride. This implies BreakRule needs a nullable ScheduleRuleItemId and a nullable ScheduleOverrideId, or a common interface/base for its parent.
                                                                                    // To keep it simpler for now, I'll assume BreakRule is primarily for ScheduleRuleItem. Overrides might have their breaks defined directly or not have complex break structures initially.
                                                                                    // Let's simplify: ScheduleOverride might not have its own BreakRule collection for now to avoid over-complicating the initial entity design. Breaks on override days can be implicitly handled by setting StartTime/EndTime around them.
  }
}
