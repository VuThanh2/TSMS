using SharedKernel.Abstractions;

namespace CourseManagement.Application.Common.Interfaces;

// Marker interface — đảm bảo DI container resolve đúng CourseDbContext,
// tránh đụng độ với IUnitOfWork của các BC khác khi có nhiều đăng ký cùng interface gốc.
public interface ICourseUnitOfWork : IUnitOfWork { }