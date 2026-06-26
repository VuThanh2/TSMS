namespace Enrollment.Domain.ValueObjects;

public enum EnrollmentStatus {
    // Student đã đăng ký và chưa được chấm điểm.
    Active,

    // Lecturer đã chấm điểm cho enrollment này.
    Graded
}