using System.Windows.Controls;
using EternalLoop.App.ViewModels;

namespace EternalLoop.App.Views;

public partial class AnalysisView : UserControl
{
    public AnalysisView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is AnalysisViewModel viewModel)
        {
            await viewModel.StartAsync().ConfigureAwait(true);
        }
    }
}
