using SharedKernel.Abstractions;

namespace EnrollmentManagement.Application.Common.Interfaces;

// Marker interface — đảm bảo DI container resolve đúng EnrollmentDbContext,
// tránh đụng độ với IUnitOfWork của các BC khác khi có nhiều đăng ký cùng interface gốc.
public interface IEnrollmentUnitOfWork : IUnitOfWork { }