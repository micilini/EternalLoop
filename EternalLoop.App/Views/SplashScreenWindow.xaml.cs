using EternalLoop.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace EternalLoop.App.Views;

public partial class SplashScreenWindow : Window
{
    private readonly SplashScreenViewModel _viewModel;

    public SplashScreenWindow(SplashScreenViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.LoadingComplete += OnLoadingComplete;
    }

    private async void Window_ContentRendered(object sender, EventArgs e)
    {
        await _viewModel.InitializeAsync();
    }

    private void OnLoadingComplete()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var mainWindow = ((App)Application.Current).Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            Close();
        });
    }
}
