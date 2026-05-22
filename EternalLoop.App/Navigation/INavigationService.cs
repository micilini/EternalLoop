using CommunityToolkit.Mvvm.ComponentModel;

namespace EternalLoop.App.Navigation;

public interface INavigationService
{
    ObservableObject? CurrentViewModel { get; }

    event EventHandler? CurrentChanged;

    void NavigateTo<TViewModel>() where TViewModel : ObservableObject;
}
