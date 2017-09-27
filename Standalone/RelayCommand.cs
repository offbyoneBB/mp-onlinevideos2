using System;
using System.Windows.Input;

namespace Standalone
{
    public class RelayCommand : ICommand
    {
        private Predicate<object> canExecute;
        private Action<object> execute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            this.canExecute = canExecute;
            this.execute = execute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return canExecute != null ?canExecute(parameter) : true;
        }

        public void Execute(object parameter)
        {
            execute(parameter);
        }
    }
}