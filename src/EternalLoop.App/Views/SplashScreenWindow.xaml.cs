using System.Windows;
using EternalLoop.App.ViewModels;

namespace EternalLoop.App.Views;

public partial class SplashScreenWindow : Window
{
    private readonly SplashScreenViewModel _viewModel;

    public SplashScreenWindow()
    {
        InitializeComponent();
        _viewModel = new SplashScreenViewModel();
        DataContext = _viewModel;
        _viewModel.LoadingComplete += OnLoadingComplete;
        Closed += OnClosed;
    }

    private async void Window_ContentRendered(object sender, EventArgs e)
    {
        await _viewModel.InitializeAsync();
    }

    private void OnLoadingComplete()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            Close();
        });
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.LoadingComplete -= OnLoadingComplete;
        Closed -= OnClosed;
    }
}
