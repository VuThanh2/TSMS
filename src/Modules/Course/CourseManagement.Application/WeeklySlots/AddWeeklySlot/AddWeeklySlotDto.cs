namespace CourseManagement.Application.WeeklySlots.AddWeeklySlot;

public sealed record AddWeeklySlotInputDto(
    string DayOfWeek,
    string SessionType);

public sealed record AddWeeklySlotOutputDto(
    Guid WeeklySlotId,
    Guid CourseId,
    string DayOfWeek,
    string SessionType,
    int GeneratedSessionCount);