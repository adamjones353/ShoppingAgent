using System.Windows;
using ShoppingAgent.ViewModels;

namespace ShoppingAgent;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
