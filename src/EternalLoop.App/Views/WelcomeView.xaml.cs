using System.Windows;
using System.Windows.Controls;
using EternalLoop.App.ViewModels;

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
        e.Handled = true;
    }

    private void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is not WelcomeViewModel viewModel ||
            e.Data.GetData(DataFormats.FileDrop) is not string[] files ||
            files.Length == 0)
        {
            return;
        }

        if (viewModel.FileDroppedCommand.CanExecute(files[0]))
        {
            viewModel.FileDroppedCommand.Execute(files[0]);
        }
    }
}
