using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace EternalLoop.App.Navigation;

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public ObservableObject? CurrentViewModel { get; private set; }

    public event EventHandler? CurrentChanged;

    public void NavigateTo<TViewModel>() where TViewModel : ObservableObject
    {
        CurrentViewModel = _serviceProvider.GetRequiredService<TViewModel>();
        CurrentChanged?.Invoke(this, EventArgs.Empty);
    }
}
