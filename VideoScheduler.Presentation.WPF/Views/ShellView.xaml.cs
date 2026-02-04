using System.Windows;
using VideoScheduler.Presentation.WPF.ViewModels;

namespace VideoScheduler.Presentation.WPF.Views;

public partial class ShellView : Window
{
    public ShellView(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
