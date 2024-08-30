Dưới đây là một ví dụ hoàn chỉnh về việc sử dụng Dependency Injection (DI) trong một ứng dụng WPF sử dụng mô hình MVVM (Model-View-ViewModel).

### Bước 1: Tạo các Interface và Lớp Dịch vụ

**IEmailService.cs**
```csharp
public interface IEmailService
{
    void SendEmail(string to, string subject, string body);
}
```

**EmailService.cs**
```csharp
public class EmailService : IEmailService
{
    public void SendEmail(string to, string subject, string body)
    {
        // Giả sử đây là logic gửi email thực tế
        MessageBox.Show($"Sending Email to {to}: {subject} - {body}");
    }
}
```

**NotificationService.cs**
```csharp
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

Trong MVVM, ViewModel chịu trách nhiệm quản lý dữ liệu và logic cho UI.

**MainViewModel.cs**
```csharp
using System.Windows.Input;

public class MainViewModel : BaseViewModel
{
    private readonly NotificationService _notificationService;

    public ICommand NotifyCommand { get; }

    public MainViewModel(NotificationService notificationService)
    {
        _notificationService = notificationService;
        NotifyCommand = new RelayCommand(Notify);
    }

    private void Notify()
    {
        _notificationService.SendNotification("This is a WPF MVVM notification.");
    }
}
```

**BaseViewModel.cs**
```csharp
using System.ComponentModel;

public class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

**RelayCommand.cs**
```csharp
using System;
using System.Windows.Input;

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool> _canExecute;

    public RelayCommand(Action execute, Func<bool> canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object parameter)
    {
        return _canExecute == null || _canExecute();
    }

    public void Execute(object parameter)
    {
        _execute();
    }

    public event EventHandler CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }
}
```

### Bước 3: Cấu hình DI trong WPF

Cấu hình DI được thực hiện trong `App.xaml.cs`.

**App.xaml.cs**
```csharp
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace DIWpfMvvmExample
{
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

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

### Bước 4: Thiết kế View (XAML)

**MainWindow.xaml**
```xml
<Window x:Class="DIWpfMvvmExample.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="DI WPF MVVM Example" Height="200" Width="400">
    <Grid>
        <Button Content="Send Notification" Width="150" Height="50" 
                HorizontalAlignment="Center" VerticalAlignment="Center"
                Command="{Binding NotifyCommand}"/>
    </Grid>
</Window>
```

**MainWindow.xaml.cs**
```csharp
using System.Windows;

namespace DIWpfMvvmExample
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
```

### Kết quả

Khi bạn chạy ứng dụng và nhấn nút "Send Notification", ứng dụng sẽ hiển thị một `MessageBox` với nội dung "Sending Email to user@example.com: Notification - This is a WPF MVVM notification."

### Giải thích:
1. **MVVM**:
   - `MainViewModel` quản lý logic gửi thông báo và tương tác với dịch vụ `NotificationService`.
   - `RelayCommand` giúp quản lý các hành động từ giao diện người dùng (UI) thông qua lệnh (`Command`).
   - `MainWindow.xaml` sử dụng binding để kết nối UI với `MainViewModel`.

2. **Dependency Injection**:
   - Các dịch vụ (`IEmailService`, `NotificationService`) và ViewModel (`MainViewModel`) được đăng ký trong `App.xaml.cs`.
   - `ServiceProvider` quản lý và cung cấp các phụ thuộc cần thiết cho các đối tượng khi chúng được khởi tạo.

3. **Lợi ích của DI trong MVVM**:
   - DI giúp đơn giản hóa việc quản lý các phụ thuộc, làm cho mã dễ bảo trì và kiểm thử hơn.
   - MVVM tách biệt logic kinh doanh khỏi giao diện người dùng, làm cho ứng dụng dễ dàng mở rộng và thay đổi hơn.
