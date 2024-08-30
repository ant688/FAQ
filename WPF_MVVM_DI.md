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

            // Cấu hình dịch vụ
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            _serviceProvider = serviceCollection.BuildServiceProvider();

            // Khởi tạo và hiển thị MainWindow thông qua ServiceProvider
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
        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(MainViewModel viewModel)
        :this() //Gọi constructor mặc định
        {
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

### Giải thích quá trình tạo ra MainViewModel khi gọi MainWindow

Trong ứng dụng WPF sử dụng Dependency Injection (DI), `MainViewModel` được tạo ra khi `MainWindow` được khởi tạo thông qua `ServiceProvider`. Điều này xảy ra do `MainWindow` có một constructor nhận `MainViewModel` làm tham số, và `MainWindow` được khởi tạo bằng cách sử dụng `ServiceProvider`, nơi đã được cấu hình để biết cách cung cấp `MainViewModel`.

### Cụ thể quá trình như sau:

1. **Đăng ký các dịch vụ và ViewModel**:
   - Trong `App.xaml.cs`, các dịch vụ và ViewModel, bao gồm `MainViewModel` và `MainWindow`, được đăng ký với `ServiceCollection`:
     ```csharp
     private void ConfigureServices(ServiceCollection services)
     {
         services.AddTransient<IEmailService, EmailService>();
         services.AddTransient<NotificationService>();
         services.AddTransient<MainViewModel>();
         services.AddTransient<MainWindow>();
     }
     ```

2. **Khởi tạo `MainWindow`**:
   - Khi ứng dụng khởi động (`OnStartup`), `ServiceProvider` được xây dựng từ `ServiceCollection` và sau đó được sử dụng để khởi tạo `MainWindow`:
     ```csharp
     var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
     mainWindow.Show();
     ```

3. **Inject `MainViewModel` vào `MainWindow`**:
   - Khi `ServiceProvider` khởi tạo `MainWindow`, nó tự động nhận ra rằng `MainWindow` cần một `MainViewModel` (do constructor của `MainWindow` yêu cầu). `ServiceProvider` sau đó sẽ tạo ra một instance của `MainViewModel` và truyền nó vào constructor của `MainWindow`.

4. **Khi `MainWindow` được tạo ra**:
   - Lúc này, `MainViewModel` cũng đã được khởi tạo và được inject vào `MainWindow`. `MainWindow` sau đó thiết lập `DataContext` của nó bằng `MainViewModel`, cho phép binding dữ liệu trong XAML hoạt động:
     ```csharp
     public MainWindow(MainViewModel viewModel)
     {
         InitializeComponent();
         DataContext = viewModel;
     }
     ```

### Tổng kết:

`MainViewModel` được tạo ra ngay tại thời điểm bạn gọi `mainWindow = _serviceProvider.GetRequiredService<MainWindow>();`. Cụ thể, quá trình này xảy ra khi `ServiceProvider` khởi tạo `MainWindow` và nhận ra rằng `MainWindow` cần một instance của `MainViewModel`. Do đó, `MainViewModel` sẽ được khởi tạo trước khi `MainWindow` hoàn tất quá trình khởi tạo của nó.



Câu lệnh `services.AddTransient<IEmailService, EmailService>();` là một phần của việc cấu hình Dependency Injection (DI) trong ứng dụng .NET, và cụ thể là trong ứng dụng WPF sử dụng DI.

### Giải thích:

- **`services`**: Đây là một đối tượng của `IServiceCollection`, dùng để đăng ký các dịch vụ và cấu hình các phụ thuộc (dependencies) mà ứng dụng sẽ sử dụng. `IServiceCollection` là một tập hợp các dịch vụ được sử dụng bởi `ServiceProvider` để tạo ra các instance của các dịch vụ này khi cần thiết.

- **`AddTransient<IEmailService, EmailService>()`**:
  - **`IEmailService`**: Đây là interface định nghĩa các chức năng mà dịch vụ email phải cung cấp. Nó đóng vai trò là hợp đồng để các lớp khác có thể tương tác với dịch vụ này mà không cần biết về cách triển khai cụ thể.
  - **`EmailService`**: Đây là lớp triển khai cụ thể của `IEmailService`. Lớp này chứa logic thực sự để gửi email hoặc thực hiện các chức năng được định nghĩa trong `IEmailService`.
  - **`AddTransient<TService, TImplementation>()`**: Đây là một phương thức mở rộng của `IServiceCollection` được sử dụng để đăng ký một dịch vụ theo thời gian sống (lifecycle) dạng "transient". Khi một dịch vụ được đăng ký là "transient", mỗi khi dịch vụ này được yêu cầu (injected), một instance mới của lớp triển khai (`EmailService` trong trường hợp này) sẽ được tạo ra.

### Các thời gian sống (lifecycles) trong DI:
- **Transient**:
  - Mỗi khi một dịch vụ được yêu cầu, một instance mới sẽ được tạo ra. Thích hợp cho các dịch vụ có trạng thái ngắn hạn hoặc không lưu giữ trạng thái.
  - Ví dụ: `services.AddTransient<IEmailService, EmailService>();`
  
- **Scoped**:
  - Một instance của dịch vụ sẽ được tạo cho mỗi "scope" (phạm vi) riêng biệt, thường là mỗi request web trong ứng dụng ASP.NET Core.
  - Ví dụ: `services.AddScoped<IEmailService, EmailService>();`
  
- **Singleton**:
  - Chỉ một instance duy nhất của dịch vụ sẽ được tạo ra và sử dụng xuyên suốt vòng đời của ứng dụng.
  - Ví dụ: `services.AddSingleton<IEmailService, EmailService>();`

### Ví dụ về `AddTransient`:

Giả sử bạn có một `MainViewModel` cần sử dụng `IEmailService` để gửi email. Khi `MainViewModel` được khởi tạo, nó sẽ yêu cầu một instance của `IEmailService`. Do `IEmailService` đã được đăng ký với DI container bằng `AddTransient`, một instance mới của `EmailService` sẽ được tạo ra và inject vào `MainViewModel`.

### Tóm lại:
Câu lệnh `services.AddTransient<IEmailService, EmailService>();` đăng ký `EmailService` như là một dịch vụ thực thi cho interface `IEmailService` với thời gian sống dạng "transient", đảm bảo rằng mỗi lần `IEmailService` được yêu cầu, một instance mới của `EmailService` sẽ được tạo ra.

### MainWindow
Khi quyết định nên đăng ký `MainWindow` là `Transient` hay `Singleton`, bạn cần xem xét cách mà `MainWindow` sẽ được sử dụng trong ứng dụng của bạn. 

### 1. **`MainWindow` là `Singleton`**

- **Khi sử dụng**: 
  - Trong hầu hết các ứng dụng WPF, `MainWindow` là cửa sổ chính duy nhất được tạo ra và tồn tại trong suốt thời gian sống của ứng dụng. 
  - Việc sử dụng `Singleton` đảm bảo rằng chỉ có một instance duy nhất của `MainWindow` được tạo ra và tái sử dụng mỗi khi cần.

- **Ưu điểm**:
  - Giảm thiểu việc khởi tạo lại không cần thiết và tiết kiệm tài nguyên hệ thống.
  - Dễ dàng quản lý trạng thái của cửa sổ chính, đặc biệt nếu `MainWindow` chứa dữ liệu hoặc trạng thái cần được duy trì trong suốt vòng đời của ứng dụng.

- **Nhược điểm**:
  - Khó khăn hơn trong việc kiểm thử đơn vị (unit testing) vì singleton có thể giữ trạng thái toàn cục.
  - Nếu ứng dụng yêu cầu nhiều instance của `MainWindow` trong tương lai, việc thay đổi từ singleton có thể phức tạp.

### 2. **`MainWindow` là `Transient`**

- **Khi sử dụng**: 
  - Khi bạn cần tạo ra nhiều instance của `MainWindow`, hoặc khi `MainWindow` không giữ bất kỳ trạng thái nào cần phải duy trì suốt vòng đời của ứng dụng.
  - Dùng trong các trường hợp mà `MainWindow` có thể bị đóng và mở lại nhiều lần trong suốt thời gian ứng dụng chạy.

- **Ưu điểm**:
  - Mỗi khi bạn yêu cầu `MainWindow`, một instance mới sẽ được tạo ra, đảm bảo rằng không có dữ liệu hoặc trạng thái không mong muốn được tái sử dụng từ một instance trước đó.
  - Dễ kiểm thử hơn vì mỗi test có thể khởi tạo một instance mới và sạch sẽ.

- **Nhược điểm**:
  - Sử dụng nhiều tài nguyên hơn do mỗi lần cần một cửa sổ mới, một instance mới phải được khởi tạo.
  - Có thể khó quản lý trạng thái của ứng dụng nếu `MainWindow` là transient nhưng lại chứa dữ liệu quan trọng cần duy trì.

### Kết luận:

- **Thông thường, `MainWindow` nên được đăng ký là `Singleton`** trong hầu hết các ứng dụng WPF vì chỉ có một cửa sổ chính và nó thường tồn tại trong suốt vòng đời của ứng dụng.

  ```csharp
  services.AddSingleton<MainWindow>();
  ```

- **Sử dụng `Transient`** nếu bạn có một tình huống cụ thể cần nhiều cửa sổ chính hoặc bạn muốn tạo một cửa sổ mới mỗi lần cần sử dụng.

  ```csharp
  services.AddTransient<MainWindow>();
  ```

Trong phần lớn các trường hợp, `Singleton` sẽ là lựa chọn tốt nhất cho `MainWindow`.

### Open new window
Để mở một cửa sổ mới hiển thị danh sách các liên hệ trong một ứng dụng WPF sử dụng mô hình MVVM và Dependency Injection (DI), bạn có thể thực hiện các bước sau:

### 1. Tạo `Contact` Model

Trước tiên, bạn cần một mô hình (model) để biểu diễn thông tin liên hệ.

**Contact.cs**
```csharp
public class Contact
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
}
```

### 2. Tạo `ContactsViewModel`

Tiếp theo, tạo một `ViewModel` để quản lý danh sách các liên hệ. `ViewModel` này sẽ chứa một danh sách các `Contact` và có thể có các lệnh (command) để tương tác với UI.

**ContactsViewModel.cs**
```csharp
using System.Collections.ObjectModel;

public class ContactsViewModel : BaseViewModel
{
    public ObservableCollection<Contact> Contacts { get; }

    public ContactsViewModel()
    {
        // Khởi tạo danh sách liên hệ với một số dữ liệu mẫu
        Contacts = new ObservableCollection<Contact>
        {
            new Contact { Name = "Alice", Email = "alice@example.com", Phone = "123-456-7890" },
            new Contact { Name = "Bob", Email = "bob@example.com", Phone = "098-765-4321" },
            new Contact { Name = "Charlie", Email = "charlie@example.com", Phone = "555-555-5555" }
        };
    }
}
```

### 3. Tạo `ContactsWindow`

Tạo một cửa sổ mới để hiển thị danh sách liên hệ, và liên kết cửa sổ này với `ContactsViewModel`.

**ContactsWindow.xaml**
```xml
<Window x:Class="DIWpfMvvmExample.ContactsWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Contacts" Height="300" Width="400">
    <Grid>
        <ListView ItemsSource="{Binding Contacts}">
            <ListView.View>
                <GridView>
                    <GridViewColumn Header="Name" DisplayMemberBinding="{Binding Name}" Width="120"/>
                    <GridViewColumn Header="Email" DisplayMemberBinding="{Binding Email}" Width="150"/>
                    <GridViewColumn Header="Phone" DisplayMemberBinding="{Binding Phone}" Width="120"/>
                </GridView>
            </ListView.View>
        </ListView>
    </Grid>
</Window>
```

**ContactsWindow.xaml.cs**
```csharp
public partial class ContactsWindow : Window
{
    public ContactsWindow(ContactsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
```

### 4. Cấu hình DI trong `App.xaml.cs`

Đăng ký `ContactsViewModel` và `ContactsWindow` trong DI container.

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

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<NotificationService>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<ContactsViewModel>();
            services.AddTransient<ContactsWindow>();
            services.AddTransient<MainWindow>();
        }
    }
}
```

### 5. Mở cửa sổ `ContactsWindow` từ `MainWindow`

Thêm một nút vào `MainWindow` để mở cửa sổ `ContactsWindow`.

**MainViewModel.cs**
```csharp
using System.Windows.Input;

public class MainViewModel : BaseViewModel
{
    private readonly ServiceProvider _serviceProvider;
    public ICommand OpenContactsCommand { get; }

    public MainViewModel(ServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        OpenContactsCommand = new RelayCommand(OpenContacts);
    }

    private void OpenContacts()
    {
        var contactsWindow = _serviceProvider.GetRequiredService<ContactsWindow>();
        contactsWindow.Show();
    }
}
```

**MainWindow.xaml**
```xml
<Window x:Class="DIWpfMvvmExample.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="DI WPF MVVM Example" Height="200" Width="400">
    <Grid>
        <Button Content="Open Contacts" Width="150" Height="50"
                HorizontalAlignment="Center" VerticalAlignment="Center"
                Command="{Binding OpenContactsCommand}"/>
    </Grid>
</Window>
```

### Giải thích:

- **ContactsWindow**: Là cửa sổ mới để hiển thị danh sách các liên hệ.
- **ContactsViewModel**: Quản lý danh sách liên hệ và là `DataContext` của `ContactsWindow`.
- **OpenContactsCommand**: Một `ICommand` trong `MainViewModel` để mở `ContactsWindow` khi người dùng nhấn nút.

### Kết quả:

Khi chạy ứng dụng và nhấn vào nút "Open Contacts" trên `MainWindow`, một cửa sổ mới `ContactsWindow` sẽ mở ra, hiển thị danh sách các liên hệ.
