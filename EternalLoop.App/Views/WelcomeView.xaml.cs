using EternalLoop.App.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace EternalLoop.App.Views;

public partial class WelcomeView : UserControl
{
    public WelcomeView()
    {
        InitializeComponent();
    }

    private void DropZone_DragEnter(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is not WelcomeViewModel viewModel)
        {
            return;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
        {
            viewModel.FileDroppedCommand.Execute(files[0]);
        }
    }
}
