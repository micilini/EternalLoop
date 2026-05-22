using EternalLoop.App.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace EternalLoop.App.Views;

public partial class AnalysisView : UserControl
{
    public AnalysisView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AnalysisViewModel viewModel)
        {
            await viewModel.StartAsync();
        }
    }
}
