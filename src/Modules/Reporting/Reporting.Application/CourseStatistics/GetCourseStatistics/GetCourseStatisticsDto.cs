namespace Reporting.Application.CourseStatistics.GetCourseStatistics;

public sealed record GetCourseStatisticsOutputDto(
    int TotalCount,
    IReadOnlyList<CourseStatisticsItemDto> Items);

public sealed record CourseStatisticsItemDto(
    Guid CourseId,
    string CourseName,
    string LecturerName,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    int EnrolledCount,
    decimal? AverageScore,
    int GradedStudentCount,
    int UngradedStudentCount);