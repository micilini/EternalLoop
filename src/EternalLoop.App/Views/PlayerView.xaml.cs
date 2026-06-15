using System.Windows.Controls;
using EternalLoop.App.ViewModels;

namespace EternalLoop.App.Views;

public partial class PlayerView : UserControl
{
    private bool _initialized;

    public PlayerView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_initialized || DataContext is not PlayerViewModel viewModel)
        {
            return;
        }

        _initialized = true;
        await viewModel.InitializeAsync().ConfigureAwait(true);
    }

    private void PositionSlider_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is PlayerViewModel vm && vm.BeginSeekCommand.CanExecute(null))
        {
            vm.BeginSeekCommand.Execute(null);
        }
    }

    private void PositionSlider_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is not PlayerViewModel vm || sender is not Slider slider)
        {
            return;
        }

        if (vm.CommitSeekCommand.CanExecute(slider.Value))
        {
            vm.CommitSeekCommand.Execute(slider.Value);
        }
    }
}
