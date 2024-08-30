Dưới đây là một ví dụ hoàn chỉnh về việc sử dụng Dependency Injection (DI) trong một ứng dụng WPF. Chúng ta sẽ xây dựng một ứng dụng đơn giản trong đó một `MainViewModel` sẽ sử dụng `NotificationService` để gửi thông báo. Chúng ta sẽ sử dụng DI để quản lý các phụ thuộc trong ứng dụng này.

### Bước 1: Tạo các Interface và Lớp Dịch vụ

Trước tiên, chúng ta tạo các interface và lớp dịch vụ tương tự như trong ví dụ console.

```csharp
// IEmailService.cs
public interface IEmailService
{
    void SendEmail(string to, string subject, string body);
}

// EmailService.cs
public class EmailService : IEmailService
{
    public void SendEmail(string to, string subject, string body)
    {
        // Giả sử đây là logic gửi email thực tế
        MessageBox.Show($"Sending Email to {to}: {subject} - {body}");
    }
}

// NotificationService.cs
public class NotificationService
{
    private readonly IEmailService _emailService;

    public NotificationService(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public void SendNotification(string message)
    {
        _emailService.SendEmail("user@example.com", "Notification", message);
    }
}
```

### Bước 2: Tạo `MainViewModel`

Tiếp theo, chúng ta tạo lớp `MainViewModel` là lớp quản lý dữ liệu và logic cho giao diện người dùng (UI).

```csharp
public class MainViewModel
{
    private readonly NotificationService _notificationService;

    public MainViewModel(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public void Notify()
    {
        _notificationService.SendNotification("This is a WPF notification.");
    }
}
```

### Bước 3: Cấu hình DI trong WPF

Trong WPF, cấu hình DI thường được thực hiện trong lớp `App.xaml.cs`.

**App.xaml.cs:**

```csharp
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace DIWpfExample
{
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Cấu hình dịch vụ
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            _serviceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = _serviceProvider.GetService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            // Đăng ký các dịch vụ và ViewModel
            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<NotificationService>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<MainWindow>();
        }
    }
}
```

### Bước 4: Sử dụng `MainViewModel` trong `MainWindow`

**MainWindow.xaml:**

```xml
<Window x:Class="DIWpfExample.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="DI WPF Example" Height="200" Width="400">
    <Grid>
        <Button Content="Send Notification" Width="150" Height="50" 
                HorizontalAlignment="Center" VerticalAlignment="Center"
                Click="Button_Click"/>
    </Grid>
</Window>
```

**MainWindow.xaml.cs:**

```csharp
using System.Windows;

namespace DIWpfExample
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Notify();
        }
    }
}
```

### Kết quả

Khi bạn chạy ứng dụng và nhấn nút "Send Notification", ứng dụng sẽ hiển thị một thông báo dưới dạng một MessageBox với nội dung "Sending Email to user@example.com: Notification - This is a WPF notification."

### Giải thích:
1. **Cấu hình DI**:
   - `App.xaml.cs` là nơi chúng ta cấu hình DI. Chúng ta sử dụng `ServiceCollection` để đăng ký các dịch vụ như `IEmailService`, `NotificationService`, `MainViewModel`, và `MainWindow`.
   - `ServiceProvider` được xây dựng từ `ServiceCollection` và được sử dụng để lấy các instance đã được đăng ký.

2. **Inject phụ thuộc**:
   - `MainViewModel` nhận `NotificationService` qua constructor.
   - `MainWindow` nhận `MainViewModel` qua constructor, đảm bảo rằng tất cả các phụ thuộc cần thiết đều được inject một cách tự động bởi DI container.

3. **Sử dụng ViewModel**:
   - Khi người dùng nhấn nút trong UI, `MainViewModel` sẽ được gọi để thực hiện logic gửi thông báo.
