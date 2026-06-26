namespace Enrollment.Domain.ValueObjects;

// SessionType được định nghĩa độc lập trong Enrollment BC.
// Enrollment BC không share enum này với CourseManagement BC
// vì mỗi Bounded Context tự sở hữu model của mình.
public enum SessionType {
    Morning,
    Afternoon
}