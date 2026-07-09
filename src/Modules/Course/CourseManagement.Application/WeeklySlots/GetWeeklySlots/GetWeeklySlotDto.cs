namespace CourseManagement.Application.WeeklySlots.GetWeeklySlots;

public sealed record GetWeeklySlotsOutputDto(
    Guid WeeklySlotId,
    Guid CourseId,
    string DayOfWeek,
    string SessionType);