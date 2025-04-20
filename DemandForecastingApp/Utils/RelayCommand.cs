using System;
using System.Windows.Input;

namespace DemandForecastingApp.Utils
{
    /// <summary>
    /// Implementation of ICommand for binding commands in MVVM pattern
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Determines whether this command can be executed in its current state.
        /// </summary>
        /// <param name="parameter">
        /// Data used by the command. If the command does not require data,
        /// this parameter can be set to null.
        /// </param>
        /// <returns>True if this command can be executed; otherwise, false.</returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="parameter">
        /// Data used by the command. If the command does not require data,
        /// this parameter can be set to null.
        /// </param>
        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        /// <summary>
        /// Forces a re-query of the command's CanExecute method
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}