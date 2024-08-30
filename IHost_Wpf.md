Dưới đây là một ví dụ hoàn chỉnh về việc sử dụng `Microsoft.Extensions.Hosting` trong một ứng dụng WPF theo mô hình MVVM. Ví dụ này bao gồm việc cấu hình `Host`, đăng ký các dịch vụ với Dependency Injection (DI), và sử dụng các dịch vụ này trong ViewModel và View của ứng dụng.

### Bước 1: Tạo dự án WPF

1. Mở Visual Studio và tạo một dự án WPF mới.
2. Cài đặt các gói NuGet cần thiết:
   - `Microsoft.Extensions.Hosting`
   - `Microsoft.Extensions.DependencyInjection`
   - `Microsoft.Extensions.Logging`
   - `Microsoft.Extensions.Configuration`
   - `Microsoft.Extensions.Configuration.Json`

### Bước 2: Cấu hình `Host` trong `App.xaml.cs`

Cấu hình `Host` để quản lý DI, cấu hình, và logging.

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

### Bước 3: Tạo ViewModel và Service

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

### Bước 4: Tạo `RelayCommand` để hỗ trợ ICommand

**RelayCommand.cs**
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

### Bước 5: Thiết kế giao diện người dùng

**MainWindow.xaml**

```xml
<Window x:Class="WpfAppWithHost.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="WPF with Host" Height="200" Width="400">
    <Grid>
        <Button Content="Say Hello" Command="{Binding SayHelloCommand}"
                HorizontalAlignment="Center" VerticalAlignment="Center" />
    </Grid>
</Window>
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

### Bước 6: Chạy ứng dụng

Khi bạn chạy ứng dụng, `MainWindow` sẽ được khởi tạo từ DI container, và ViewModel cũng sẽ được inject tự động. Khi người dùng nhấn nút "Say Hello", `IHelloService` sẽ được gọi và hiển thị một thông báo.

### Giải thích:

- **Host Configuration**: `Host.CreateDefaultBuilder()` thiết lập các cấu hình mặc định cho ứng dụng, bao gồm việc đọc cấu hình từ `appsettings.json`, thiết lập logging và DI.
- **Dependency Injection**: Các dịch vụ như `IHelloService` và `MainViewModel` được đăng ký và quản lý bởi DI container, sau đó được inject vào `MainWindow`.
- **RelayCommand**: Được sử dụng để liên kết các command trong ViewModel với các hành động cụ thể, trong trường hợp này là `SayHelloCommand`.
- **Logging**: Logging được cấu hình để ghi lại thông tin ra console, có thể mở rộng hoặc thay đổi theo yêu cầu.

Sử dụng `Microsoft.Extensions.Hosting` trong WPF giúp bạn tổ chức ứng dụng một cách rõ ràng và dễ bảo trì, đồng thời tận dụng được các công nghệ hiện đại như DI, cấu hình và logging.

### II - Mở cửa số mới
Để mở một cửa sổ mới trong ứng dụng WPF khi sử dụng `IHost` và Dependency Injection (DI), bạn có thể làm theo các bước dưới đây. Giả sử bạn muốn mở một cửa sổ mới để hiển thị danh sách liên hệ khi nhấn một nút trong `MainWindow`.

### Bước 1: Tạo một cửa sổ mới để hiển thị danh sách liên hệ

**ContactsWindow.xaml**

```xml
<Window x:Class="WpfAppWithHost.ContactsWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Contacts" Height="300" Width="400">
    <Grid>
        <ListBox ItemsSource="{Binding Contacts}" />
    </Grid>
</Window>
```

**ContactsWindow.xaml.cs**

```csharp
using System.Windows;

namespace WpfAppWithHost
{
    public partial class ContactsWindow : Window
    {
        public ContactsWindow(ContactsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
```

**ContactsViewModel.cs**

```csharp
using System.Collections.ObjectModel;

namespace WpfAppWithHost
{
    public class ContactsViewModel
    {
        public ObservableCollection<string> Contacts { get; }

        public ContactsViewModel()
        {
            Contacts = new ObservableCollection<string>
            {
                "Contact 1",
                "Contact 2",
                "Contact 3"
            };
        }
    }
}
```

### Bước 2: Đăng ký `ContactsWindow` và `ContactsViewModel` trong `App.xaml.cs`

Cập nhật `App.xaml.cs` để đăng ký `ContactsWindow` và `ContactsViewModel` với DI container.

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

                    // Đăng ký thêm ContactsWindow và ContactsViewModel
                    services.AddTransient<ContactsWindow>();
                    services.AddTransient<ContactsViewModel>();
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

### Bước 3: Thêm command để mở cửa sổ mới từ `MainViewModel`

Cập nhật `MainViewModel` để thêm một `ICommand` mở `ContactsWindow`.

**MainViewModel.cs**

```csharp
using System.Windows.Input;

namespace WpfAppWithHost
{
    public class MainViewModel
    {
        private readonly IHelloService _helloService;
        private readonly IServiceProvider _serviceProvider;

        public ICommand SayHelloCommand { get; }
        public ICommand OpenContactsCommand { get; }

        public MainViewModel(IHelloService helloService, IServiceProvider serviceProvider)
        {
            _helloService = helloService;
            _serviceProvider = serviceProvider;

            SayHelloCommand = new RelayCommand(SayHello);
            OpenContactsCommand = new RelayCommand(OpenContacts);
        }

        private void SayHello()
        {
            _helloService.SayHello();
        }

        private void OpenContacts()
        {
            var contactsWindow = _serviceProvider.GetRequiredService<ContactsWindow>();
            contactsWindow.Show();
        }
    }
}
```

### Bước 4: Thêm nút mở cửa sổ Contacts trong `MainWindow.xaml`

Cập nhật `MainWindow.xaml` để thêm một nút mở cửa sổ `ContactsWindow`.

**MainWindow.xaml**

```xml
<Window x:Class="WpfAppWithHost.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="WPF with Host" Height="200" Width="400">
    <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
        <Button Content="Say Hello" Command="{Binding SayHelloCommand}" />
        <Button Content="Open Contacts" Command="{Binding OpenContactsCommand}" Margin="0,20,0,0"/>
    </StackPanel>
</Window>
```

### Bước 5: Chạy ứng dụng

Khi bạn chạy ứng dụng, cửa sổ chính sẽ hiển thị với hai nút. Khi bạn nhấn nút "Open Contacts", cửa sổ `ContactsWindow` sẽ được mở và hiển thị danh sách các liên hệ.

### Giải thích:

- **`IServiceProvider` trong `MainViewModel`**: Được sử dụng để lấy `ContactsWindow` từ DI container và mở nó khi người dùng nhấn nút "Open Contacts".
- **`AddTransient<ContactsWindow>`**: Đảm bảo rằng mỗi lần mở cửa sổ `ContactsWindow` sẽ tạo một instance mới.
- **`ContactsViewModel`**: Cung cấp dữ liệu cho `ContactsWindow`, trong trường hợp này là một danh sách các tên liên hệ.

Việc sử dụng `IHost` trong WPF giúp bạn quản lý các cửa sổ và dịch vụ trong ứng dụng một cách linh hoạt và hiệu quả hơn thông qua Dependency Injection.
