using System.Windows.Input;

namespace EternalLoop.App.Commands;

public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;
    private event EventHandler? CanExecuteChangedCore;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add
        {
            CanExecuteChangedCore += value;
            CommandManager.RequerySuggested += value;
        }
        remove
        {
            CanExecuteChangedCore -= value;
            CommandManager.RequerySuggested -= value;
        }
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke() ?? true;
    }

    public void Execute(object? parameter)
    {
        _execute();
    }

    public void NotifyCanExecuteChanged()
    {
        CanExecuteChangedCore?.Invoke(this, EventArgs.Empty);
        CommandManager.InvalidateRequerySuggested();
    }
}
