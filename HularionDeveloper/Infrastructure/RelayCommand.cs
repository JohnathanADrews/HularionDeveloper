#region License
/*
MIT License

Copyright (c) 2023-2024 Johnathan A Drews

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace HularionDeveloper.Infrastructure
{
    public class RelayCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        private Func<object?, bool> canExecute;
        private Action<object?> execute;
        private Action<string> onPropertyChange;
        private string propertyName;

        public RelayCommand(Func<object?, bool> canExecute, Action<object?> execute)
        {
            this.canExecute = canExecute;
            this.execute = execute;
        }

        public RelayCommand(Func<object?, bool> canExecute, Action<object?> execute, Action<string> onPropertyChange, string propertyName)
        {
            this.canExecute = canExecute;
            this.execute = execute;
            this.onPropertyChange = onPropertyChange;
            this.propertyName = propertyName;
        }

        public bool CanExecute(object? parameter)
        {
            return canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            execute(parameter);
            if (onPropertyChange != null) { onPropertyChange(propertyName); }
        }
    }
}
