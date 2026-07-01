namespace Reporting.Application.ScoreDistribution.GetScoreDistribution;

public sealed record GetScoreDistributionOutputDto(
    Guid CourseId,
    string CourseName,
    int GradedStudentCount,
    IReadOnlyList<ScoreDistributionItemDto> Items);

// Mỗi item ứng với một Score Group (Xuất sắc / Giỏi / Trung bình / Yếu).
public sealed record ScoreDistributionItemDto(
    string ScoreGroup,
    decimal RangeStart,
    decimal RangeEnd,
    int StudentCount,
    decimal Percentage);