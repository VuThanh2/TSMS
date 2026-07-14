using EnrollmentManagement.Application.Common.Interfaces;
using MediatR;
using SharedKernel.Primitives;

namespace EnrollmentManagement.Application.Dev.ResetDemoEnrollmentData;

// Không seed lại — chỉ xóa. Theo Deploy Plan, Enrollment/Attendance/Grade để trống sau reset;
// Admin/Lecturer/Student tự enroll/điểm danh/chấm điểm TRỰC TIẾP trong lúc demo (tương tác thật).
public sealed record ResetDemoEnrollmentDataCommand : IRequest<Result<ResetDemoEnrollmentDataOutputDto>>;

public sealed class ResetDemoEnrollmentDataCommandHandler
    : IRequestHandler<ResetDemoEnrollmentDataCommand, Result<ResetDemoEnrollmentDataOutputDto>> {
    private readonly IEnrollmentDataResetter _enrollmentDataResetter;

    public ResetDemoEnrollmentDataCommandHandler(IEnrollmentDataResetter enrollmentDataResetter) {
        _enrollmentDataResetter = enrollmentDataResetter;
    }

    public async Task<Result<ResetDemoEnrollmentDataOutputDto>> Handle(
        ResetDemoEnrollmentDataCommand request,
        CancellationToken cancellationToken) {
        await _enrollmentDataResetter.ClearAllAsync(cancellationToken);

        return Result.Success(new ResetDemoEnrollmentDataOutputDto(true));
    }
}