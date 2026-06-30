namespace Reporting.Application.PersonalSummary.GetMyPersonalSummary;

public sealed record GetMyPersonalSummaryOutputDto(
    decimal? OverallGpa,
    IReadOnlyList<PersonalSummaryItemDto> Items);

// Course chưa có điểm vẫn xuất hiện với Grade = null.
public sealed record PersonalSummaryItemDto(
    Guid CourseId,
    string CourseName,
    string Status,
    decimal? Grade,
    int TotalSessions,
    int PresentCount,
    int ExcusedCount,
    int AbsentCount,
    decimal AttendanceRate);