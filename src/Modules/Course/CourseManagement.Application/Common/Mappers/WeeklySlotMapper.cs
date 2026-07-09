using CourseManagement.Application.WeeklySlots.GetWeeklySlots;
using CourseManagement.Domain.Entities;

namespace CourseManagement.Application.Common.Mappers;

public static class WeeklySlotMapper {
    public static GetWeeklySlotsOutputDto ToGetWeeklySlotsOutputDto(WeeklySlot slot) =>
        new(
            WeeklySlotId: slot.Id,
            CourseId: slot.CourseId,
            DayOfWeek: slot.DayOfWeek.ToString(),
            SessionType: slot.SessionType.ToString());
}