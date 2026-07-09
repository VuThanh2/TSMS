using CourseManagement.Application.Common.Mappers;
using CourseManagement.Domain.Errors;
using CourseManagement.Domain.Repositories;
using MediatR;
using SharedKernel.Primitives;

namespace CourseManagement.Application.WeeklySlots.GetWeeklySlots;

// Dùng chung cho cả Admin, Lecturer, Student (Presentation tự enforce ownership).
// Trả về đúng granularity "khung giờ lặp lại hàng tuần" (2-vài item), KHÔNG phải
// danh sách ClassSession (có thể hàng chục item trải dài cả kỳ)
public sealed record GetWeeklySlotsQuery(Guid CourseId)
    : IRequest<Result<IReadOnlyList<GetWeeklySlotsOutputDto>>>;
 
public sealed class GetWeeklySlotsQueryHandler
    : IRequestHandler<GetWeeklySlotsQuery, Result<IReadOnlyList<GetWeeklySlotsOutputDto>>> {
    private readonly ICourseRepository _courseRepository;
 
    public GetWeeklySlotsQueryHandler(ICourseRepository courseRepository) {
        _courseRepository = courseRepository;
    }
 
    public async Task<Result<IReadOnlyList<GetWeeklySlotsOutputDto>>> Handle(
        GetWeeklySlotsQuery request,
        CancellationToken cancellationToken) {
        var course = await _courseRepository.GetByIdWithWeeklySlotsAsync(
            request.CourseId, cancellationToken);
 
        if (course is null)
            return Result.Failure<IReadOnlyList<GetWeeklySlotsOutputDto>>(CourseErrors.NotFound);
 
        var dtos = course.WeeklySlots
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.SessionType)
            .Select(WeeklySlotMapper.ToGetWeeklySlotsOutputDto)
            .ToList();
 
        return Result.Success<IReadOnlyList<GetWeeklySlotsOutputDto>>(dtos);
    }
}