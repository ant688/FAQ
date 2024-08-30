
IoC (Inversion of Control) là một nguyên tắc trong lập trình phần mềm, đặc biệt là trong lập trình hướng đối tượng. Nó giúp giảm sự phụ thuộc giữa các lớp và tạo điều kiện cho việc mở rộng, bảo trì và kiểm thử phần mềm dễ dàng hơn. Trong ngữ cảnh của C#, IoC thường được thực hiện thông qua Dependency Injection (DI).

### Ví dụ về IoC trong C#

Giả sử bạn có một ứng dụng cần gửi email. Bạn có thể có một lớp `EmailService` chịu trách nhiệm gửi email và một lớp `NotificationService` sử dụng `EmailService` để gửi thông báo.

#### Trước khi sử dụng IoC:
```csharp
public class EmailService
{
    public void SendEmail(string to, string subject, string body)
    {
        // Code để gửi email
    }
}

public class NotificationService
{
    private EmailService _emailService;

    public NotificationService()
    {
        _emailService = new EmailService(); // Dependency trực tiếp
    }

    public void SendNotification(string message)
    {
        _emailService.SendEmail("example@example.com", "Notification", message);
    }
}
```
Trong ví dụ này, `NotificationService` phụ thuộc trực tiếp vào `EmailService`. Điều này làm cho việc thay thế hoặc thay đổi `EmailService` trở nên khó khăn.

#### Sau khi sử dụng IoC với Dependency Injection:
```csharp
public interface IEmailService
{
    void SendEmail(string to, string subject, string body);
}

public class EmailService : IEmailService
{
    public void SendEmail(string to, string subject, string body)
    {
        // Code để gửi email
    }
}

public class NotificationService
{
    private readonly IEmailService _emailService;

    public NotificationService(IEmailService emailService)
    {
        _emailService = emailService; // Dependency được inject qua constructor
    }

    public void SendNotification(string message)
    {
        _emailService.SendEmail("example@example.com", "Notification", message);
    }
}
```

Trong phiên bản này, `NotificationService` không còn phụ thuộc trực tiếp vào một lớp cụ thể (`EmailService`). Thay vào đó, nó phụ thuộc vào một interface (`IEmailService`). Khi tạo một instance của `NotificationService`, bạn có thể "inject" một instance của `IEmailService` (có thể là `EmailService` hoặc một lớp khác) từ bên ngoài.

Điều này giúp bạn dễ dàng thay thế `EmailService` bằng một dịch vụ khác khi cần mà không phải thay đổi `NotificationService`. Đồng thời, việc kiểm thử `NotificationService` cũng trở nên đơn giản hơn khi bạn có thể mock `IEmailService`.
