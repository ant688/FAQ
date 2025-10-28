using PDFWand.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Wpf.Ui.Abstractions.Controls;

namespace PDFWand.Views.Pages
{
    /// <summary>
    /// Interaction logic for PagingPage.xaml
    /// </summary>
    public partial class PagingPage : INavigableView<PagingViewModel>
    {
        public PagingViewModel ViewModel { get; }
        public PagingPage(PagingViewModel viewModel)
        {

            InitializeComponent();
            ViewModel = viewModel;
            DataContext = this;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menu || menu.Header is not Border border || border.Background is not SolidColorBrush brush)
            {
                return;
            }

            ViewModel.ChangeFontColorCommand.Execute(brush);
        }

        private void PreviewScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width <= 0 || e.NewSize.Height <= 0)
            {
                return;
            }

            ViewModel.UpdatePreviewViewportSize(e.NewSize.Width, e.NewSize.Height);
        }
    }
}
