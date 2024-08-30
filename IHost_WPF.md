Trong một ứng dụng WPF, `IHost` từ `Microsoft.Extensions.Hosting` có thể được sử dụng để tích hợp Dependency Injection (DI), cấu hình, và logging một cách nhất quán và dễ quản lý. Điều này rất hữu ích khi bạn muốn ứng dụng WPF của mình có một cấu trúc tương tự như các ứng dụng web hoặc console theo mô hình .NET hiện đại.

### Các bước sử dụng `IHost` trong ứng dụng WPF:

1. **Cài đặt các gói NuGet cần thiết**:
   - Bạn cần cài đặt các gói sau:
     - `Microsoft.Extensions.Hosting`
     - `Microsoft.Extensions.DependencyInjection`
     - `Microsoft.Extensions.Logging`
     - `Microsoft.Extensions.Configuration`
     - `Microsoft.Extensions.Configuration.Json`

2. **Cấu hình `Host` trong `App.xaml.cs`**:
   - Trong file `App.xaml.cs`, bạn sẽ thiết lập `Host` để quản lý DI, cấu hình và logging.

3. **Sử dụng DI trong `MainWindow` và ViewModel**:
   - Inject các dịch vụ cần thiết vào `MainWindow` hoặc các ViewModel thông qua DI.

### Ví dụ cụ thể:

**App.xaml.cs**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Windows;

namespace WpfAppWithHost
{
    public partial class App : Application
    {
        private IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Đăng ký các dịch vụ của bạn ở đây
                    services.AddSingleton<MainWindow>();
                    services.AddTransient<MainViewModel>();
                    services.AddTransient<IHelloService, HelloService>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Khởi động Host
            await _host.StartAsync();

            // Lấy MainWindow từ DI container
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            // Dừng Host khi ứng dụng thoát
            await _host.StopAsync();
            _host.Dispose();

            base.OnExit(e);
        }
    }
}
```

**MainWindow.xaml.cs**

```csharp
using System.Windows;

namespace WpfAppWithHost
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

**MainViewModel.cs**

```csharp
using System.Windows.Input;

namespace WpfAppWithHost
{
    public class MainViewModel
    {
        private readonly IHelloService _helloService;

        public ICommand SayHelloCommand { get; }

        public MainViewModel(IHelloService helloService)
        {
            _helloService = helloService;
            SayHelloCommand = new RelayCommand(SayHello);
        }

        private void SayHello()
        {
            _helloService.SayHello();
        }
    }
}
```

**IHelloService.cs**

```csharp
namespace WpfAppWithHost
{
    public interface IHelloService
    {
        void SayHello();
    }

    public class HelloService : IHelloService
    {
        public void SayHello()
        {
            MessageBox.Show("Hello from HelloService!");
        }
    }
}
```

**RelayCommand.cs** (Để hỗ trợ các command)

```csharp
using System;
using System.Windows.Input;

namespace WpfAppWithHost
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
```

### Giải thích:

1. **`Host.CreateDefaultBuilder()`**:
   - Tạo một `HostBuilder` với cấu hình mặc định, bao gồm việc tải cấu hình từ các tệp `appsettings.json`, biến môi trường, và logging console.

2. **`ConfigureServices`**:
   - Bạn đăng ký các dịch vụ mà ứng dụng của bạn cần. Trong ví dụ này, `MainWindow`, `MainViewModel`, và `IHelloService` được đăng ký.

3. **`OnStartup`**:
   - Khi ứng dụng khởi động, `Host` sẽ bắt đầu, và `MainWindow` sẽ được lấy từ DI container và hiển thị.

4. **`OnExit`**:
   - Khi ứng dụng thoát, `Host` sẽ được dừng và tài nguyên sẽ được giải phóng.

### Lợi ích của việc sử dụng `IHost` trong WPF:

- **Tích hợp dễ dàng với DI và Logging**: Bạn có thể dễ dàng sử dụng DI, cấu hình và logging trong ứng dụng WPF của mình.
- **Cấu trúc nhất quán**: Giữ cho cấu trúc ứng dụng của bạn nhất quán với các loại ứng dụng khác như Web API hoặc Console apps.
- **Quản lý vòng đời ứng dụng**: Dễ dàng quản lý vòng đời ứng dụng, bao gồm khởi động và tắt, thông qua `IHost`.

Sử dụng `Microsoft.Extensions.Hosting` giúp bạn xây dựng các ứng dụng WPF theo cách hiện đại và nhất quán với các loại ứng dụng khác trong hệ sinh thái .NET.
