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


Dưới đây là một ví dụ hoàn chỉnh về việc sử dụng Dependency Injection (DI) trong một ứng dụng C# console đơn giản. Ứng dụng này sẽ có một lớp `NotificationService` sử dụng `EmailService` để gửi thông báo. Chúng ta sẽ sử dụng DI để inject các phụ thuộc vào `NotificationService`.

### Bước 1: Tạo các Interface và Lớp Dịch vụ

```csharp
using System;

namespace DIExample
{
    // Định nghĩa interface IEmailService
    public interface IEmailService
    {
        void SendEmail(string to, string subject, string body);
    }

    // Triển khai interface IEmailService trong lớp EmailService
    public class EmailService : IEmailService
    {
        public void SendEmail(string to, string subject, string body)
        {
            // Giả sử đây là logic gửi email thực tế
            Console.WriteLine($"Sending Email to {to}: {subject} - {body}");
        }
    }

    // Lớp NotificationService sử dụng IEmailService
    public class NotificationService
    {
        private readonly IEmailService _emailService;

        // Inject IEmailService thông qua constructor
        public NotificationService(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public void SendNotification(string message)
        {
            // Sử dụng emailService để gửi email
            _emailService.SendEmail("user@example.com", "Notification", message);
        }
    }
}
```

### Bước 2: Cấu hình Dependency Injection

Chúng ta sẽ sử dụng `Microsoft.Extensions.DependencyInjection` để cấu hình DI container.

```csharp
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DIExample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Tạo một DI container mới
            var services = new ServiceCollection();

            // Đăng ký các dịch vụ
            services.AddTransient<IEmailService, EmailService>(); // IEmailService sẽ được triển khai bởi EmailService
            services.AddTransient<NotificationService>(); // Đăng ký NotificationService

            // Xây dựng ServiceProvider từ ServiceCollection
            var serviceProvider = services.BuildServiceProvider();

            // Lấy một instance của NotificationService từ container
            var notificationService = serviceProvider.GetService<NotificationService>();

            // Gửi một thông báo
            notificationService.SendNotification("This is a test notification.");

            Console.ReadLine();
        }
    }
}
```

### Giải thích các bước:
1. **Tạo các interface và lớp dịch vụ**: `IEmailService` là interface định nghĩa các hành vi gửi email. `EmailService` là lớp triển khai của `IEmailService`. `NotificationService` sử dụng `IEmailService` để gửi thông báo.

2. **Cấu hình DI**:
   - Tạo một `ServiceCollection` để đăng ký các dịch vụ.
   - Sử dụng `AddTransient` để đăng ký dịch vụ `IEmailService` và `NotificationService`. `AddTransient` có nghĩa là một instance mới sẽ được tạo ra mỗi lần có yêu cầu.
   - Xây dựng `ServiceProvider` từ `ServiceCollection`.

3. **Sử dụng DI**:
   - Lấy một instance của `NotificationService` từ container.
   - Gọi phương thức `SendNotification` để gửi thông báo.

Khi chạy chương trình, bạn sẽ thấy đầu ra như sau:

```
Sending Email to user@example.com: Notification - This is a test notification.
```

Điều này cho thấy rằng `NotificationService` đã sử dụng `EmailService` để gửi thông báo, và phụ thuộc (`IEmailService`) đã được inject thành công.
