using EternalLoop.App.ViewModels;
using System.Windows.Controls;

namespace EternalLoop.App.Views;

public partial class PlayerView : UserControl
{
    public PlayerView()
    {
        InitializeComponent();
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
