using SharedInfrastructure.Email;

namespace Enrollment.UnitTests.Fakes;

// Fake IEmailSender — ghi lại mọi email đã gửi để assert; có thể cấu hình fail cho 1 số địa chỉ
// nhằm test isolation (1 email lỗi không được làm hỏng cả batch).
public sealed class FakeEmailSender : IEmailSender {
    public List<EmailMessage> Sent { get; } = new();

    // Nếu trả về true cho 1 message → SendAsync ném exception (mô phỏng SMTP timeout / sai địa chỉ).
    public Func<EmailMessage, bool>? FailWhen { get; set; }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default) {
        if (FailWhen?.Invoke(message) == true)
            throw new InvalidOperationException($"SMTP failure for {message.To}");

        Sent.Add(message);
        return Task.CompletedTask;
    }
}
