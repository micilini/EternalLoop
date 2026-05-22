using EternalLoop.App.ViewModels;
using System.Windows;

namespace EternalLoop.App;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) => viewModel.Initialize();
    }
}
