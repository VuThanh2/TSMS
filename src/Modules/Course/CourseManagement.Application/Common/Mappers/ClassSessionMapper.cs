using CourseManagement.Application.ClassSessions.GetClassSessions;
using CourseManagement.Domain.Entities;

namespace CourseManagement.Application.Common.Mappers;

public static class ClassSessionMapper {
    public static GetClassSessionsOutputDto ToGetClassSessionsOutputDto(ClassSession session) =>
        new(
            ClassSessionId: session.Id,
            WeeklySlotId: session.WeeklySlotId,
            SessionDate: session.SessionDate,
            DayOfWeek: session.DayOfWeek.ToString(),
            SessionType: session.SessionType.ToString(),
            IsPast: session.IsPast());
}