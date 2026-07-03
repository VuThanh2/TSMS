namespace CourseManagement.Application.WeeklySlots.RemoveWeeklySlot;

// Query-only style — không cần InputDto body vì CourseId/WeeklySlotId đến từ route.
public sealed record RemoveWeeklySlotOutputDto(bool Success);