Dependency Injection (DI) là một kỹ thuật trong lập trình phần mềm, đặc biệt là trong lập trình hướng đối tượng, để quản lý sự phụ thuộc giữa các đối tượng. Thay vì để một đối tượng tự tạo ra hoặc quản lý các đối tượng mà nó phụ thuộc vào, Dependency Injection sẽ "tiêm" các phụ thuộc này từ bên ngoài vào. Điều này giúp giảm sự phụ thuộc cứng nhắc giữa các lớp, tăng khả năng mở rộng, dễ dàng kiểm thử, và cải thiện khả năng bảo trì của ứng dụng.

### Các kiểu Dependency Injection chính
1. **Constructor Injection**: Tiêm phụ thuộc thông qua constructor của lớp.
2. **Property Injection**: Tiêm phụ thuộc thông qua các thuộc tính của lớp.
3. **Method Injection**: Tiêm phụ thuộc thông qua các phương thức của lớp.

### Ví dụ về Dependency Injection trong C#

Giả sử bạn có một dịch vụ gửi thông báo (`NotificationService`) cần sử dụng một dịch vụ gửi email (`EmailService`). Bạn có thể sử dụng DI để inject `EmailService` vào `NotificationService`.

#### Constructor Injection
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

    // Inject dependency thông qua constructor
    public NotificationService(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public void SendNotification(string message)
    {
        _emailService.SendEmail("example@example.com", "Notification", message);
    }
}
```

Trong ví dụ này, `EmailService` được inject vào `NotificationService` thông qua constructor. Khi tạo một instance của `NotificationService`, bạn sẽ phải cung cấp một instance của `IEmailService` (ví dụ `EmailService`).

#### Property Injection
```csharp
public class NotificationService
{
    public IEmailService EmailService { get; set; }

    public void SendNotification(string message)
    {
        EmailService.SendEmail("example@example.com", "Notification", message);
    }
}
```
Ở đây, phụ thuộc (`EmailService`) được tiêm qua thuộc tính `EmailService`. Cách này linh hoạt hơn nhưng có thể dẫn đến lỗi nếu quên set thuộc tính này trước khi sử dụng.

#### Method Injection
```csharp
public class NotificationService
{
    public void SendNotification(IEmailService emailService, string message)
    {
        emailService.SendEmail("example@example.com", "Notification", message);
    }
}
```
Với method injection, phụ thuộc (`EmailService`) được tiêm qua tham số của phương thức `SendNotification`. Điều này phù hợp khi phụ thuộc chỉ cần thiết trong một số phương thức nhất định.

### Sử dụng Dependency Injection với Container
Trong thực tế, việc quản lý các phụ thuộc thường được thực hiện thông qua một IoC Container (như Unity, Autofac, hay Microsoft.Extensions.DependencyInjection).

Ví dụ với Microsoft.Extensions.DependencyInjection:

```csharp
var services = new ServiceCollection();
services.AddTransient<IEmailService, EmailService>();
services.AddTransient<NotificationService>();

var serviceProvider = services.BuildServiceProvider();
var notificationService = serviceProvider.GetService<NotificationService>();
notificationService.SendNotification("Hello World!");
```

Ở đây, `ServiceCollection` được dùng để đăng ký các dịch vụ, sau đó `ServiceProvider` được dùng để lấy các dịch vụ đã được cấu hình. Phụ thuộc sẽ tự động được tiêm bởi container này.
