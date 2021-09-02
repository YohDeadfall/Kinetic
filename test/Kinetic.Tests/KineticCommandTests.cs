using System;
using System.Windows.Input;
using Xunit;

namespace Kinetic.Tests
{
    public class KineticCommandTests
    {
        [Fact]
        public void ExecuteAction()
        {
            var executions = 0;
            var command = KineticCommand.Create(() => { executions += 1; });

            command.Execute();

            Assert.Equal(1, executions);
        }

        [Fact]
        public void ExecuteActionWithParameter()
        {
            var executions = 0;
            var parameter = 0;
            var command = KineticCommand<int>.Create(p =>
            {
                parameter = p;
                executions += 1;
            });

            command.Execute(42);

            Assert.Equal(1, executions);
            Assert.Equal(42, parameter);
        }

        [Fact]
        public void ExecuteFunction()
        {
            var executions = 0;
            var command = KineticCommand.Create(() => executions += 1);

            Assert.Equal(1, command.Execute());
            Assert.Equal(1, executions);
        }

        [Fact]
        public void ExecuteFunctionWithParameter()
        {
            var executions = 0;
            var parameter = 0;
            var command = KineticCommand<int>.Create(p =>
            {
                parameter = p;
                return executions += 1;
            });

            Assert.Equal(1, command.Execute(42));
            Assert.Equal(1, executions);
            Assert.Equal(42, parameter);
        }

        [Fact]
        public void CanExecuteAction()
        {
            var executions = 0;
            var validations = 0;
            var command = KineticCommand.Create(
                execute: () => { executions += 1; },
                canExecute: () => { validations += 1; return false; });
            
            Assert.False(command.CanExecute());
            Assert.Equal(1, validations);

            Assert.Throws<InvalidOperationException>(() => command.Execute());
            Assert.Equal(0, executions);
        }

        [Fact]
        public void CanExecuteActionWithParameter()
        {
            var executions = 0;
            var validations = 0;
            var command = KineticCommand<int>.Create(
                execute: p => { executions += 1; },
                canExecute: p => { validations += 1; return p == 0; });
            
            Assert.False(command.CanExecute(42));
            Assert.Equal(1, validations);

            Assert.Throws<InvalidOperationException>(() => command.Execute(42));
            Assert.Equal(0, executions);
        }

        [Fact]
        public void CanExecuteFunction()
        {
            var executions = 0;
            var validations = 0;
            var command = KineticCommand.Create(
                execute: () => executions += 1,
                canExecute: () => { validations += 1; return false; });
            
            Assert.False(command.CanExecute());
            Assert.Equal(1, validations);

            Assert.Throws<InvalidOperationException>(() => command.Execute());
            Assert.Equal(0, executions);
        }

        [Fact]
        public void CanExecuteFunctionWithParameter()
        {
            var executions = 0;
            var validations = 0;
            var command = KineticCommand<int>.Create(
                execute: p => executions += 1,
                canExecute: p => { validations += 1; return p == 0; });
            
            Assert.False(command.CanExecute(42));
            Assert.Equal(1, validations);

            Assert.Throws<InvalidOperationException>(() => command.Execute(42));
            Assert.Equal(0, executions);
        }

        [Fact]
        public void CanExecuteChangedAction()
        {
            var state = new State();
            var events = 0;
            var command = KineticCommand.Create(
                state.CanExecute.Changed,
                execute: s => { },
                canExecute: s => s);
            
            command.CanExecuteChanged += (s, e) => events += 1;

            Assert.False(command.CanExecute());
            Assert.Equal(0, events);

            state.CanExecute.Set(true);

            Assert.True(command.CanExecute());
            Assert.Equal(1, events);
        }

        [Fact]
        public void CanExecuteChangedActionWithParameter()
        {
            var state = new State();
            var events = 0;
            var command = KineticCommand<bool>.Create(
                state.CanExecute.Changed,
                execute: (s, p) => { },
                canExecute: (s, p) => s && p);
            
            command.CanExecuteChanged += (s, e) => events += 1;

            Assert.False(command.CanExecute(false));
            Assert.False(command.CanExecute(true));
            Assert.Equal(0, events);

            state.CanExecute.Set(true);

            Assert.False(command.CanExecute(false));
            Assert.True(command.CanExecute(true));
            Assert.Equal(1, events);
        }

        [Fact]
        public void CanExecuteChangedFunction()
        {
            var state = new State();
            var events = 0;
            var command = KineticCommand.Create(
                state.CanExecute.Changed,
                execute: s => s,
                canExecute: s => s);
            
            command.CanExecuteChanged += (s, e) => events += 1;

            Assert.False(command.CanExecute());
            Assert.Equal(0, events);

            state.CanExecute.Set(true);

            Assert.True(command.CanExecute());
            Assert.Equal(1, events);
        }

        [Fact]
        public void CanExecuteChangedFunctionWithParameter()
        {
            var state = new State();
            var events = 0;
            var command = KineticCommand<bool>.Create(
                state.CanExecute.Changed,
                execute: (s, p) => s && p,
                canExecute: (s, p) => s && p);
            
            command.CanExecuteChanged += (s, e) => events += 1;

            Assert.False(command.CanExecute(false));
            Assert.False(command.CanExecute(true));
            Assert.Equal(0, events);

            state.CanExecute.Set(true);

            Assert.False(command.CanExecute(false));
            Assert.True(command.CanExecute(true));
            Assert.Equal(1, events);
        }

        [Fact]
        public void DisposeAction()
        {
            var state = new State();
            var events = 0;
            var command = KineticCommand.Create(
                state.Counter.Changed,
                execute: s => { },
                canExecute: s => s == 0);
            
            command.CanExecuteChanged += (s, e) => events += 1;            
            state.Counter.Set(1);

            Assert.Equal(1, events);

            command.Dispose();
            state.Counter.Set(2);

            Assert.Equal(1, events);
        }

        [Fact]
        public void DisposeFunction()
        {
            var state = new State();
            var events = 0;
            var command = KineticCommand.Create(
                state.Counter.Changed,
                execute: s => s,
                canExecute: s => s == 0);
            
            command.CanExecuteChanged += (s, e) => events += 1;            
            state.Counter.Set(1);

            Assert.Equal(1, events);

            command.Dispose();
            state.Counter.Set(2);

            Assert.Equal(1, events);
        }

        private sealed class State : KineticObject
        {
            private bool _canExecute;
            private int _counter;

            public KineticProperty<bool> CanExecute => Property(ref _canExecute);
            public KineticProperty<int> Counter => Property(ref _counter);
        }

        [Fact]
        public void ParameterCheck()
        {
            ICommand command1 = KineticCommand<int>.Create(p => p);

            Assert.False(command1.CanExecute(null));
            Assert.False(command1.CanExecute(default(long)));
            Assert.True(command1.CanExecute(default(int)));

            ICommand command2 = KineticCommand<int?>.Create(p => p);

            Assert.True(command2.CanExecute(null));
            Assert.True(command2.CanExecute(default(int)));

            ICommand command3 = KineticCommand<string>.Create(p => p);

            Assert.False(command3.CanExecute(null));
            Assert.False(command1.CanExecute(default(long)));
            Assert.True(command3.CanExecute(string.Empty));

            ICommand command4 = KineticCommand<string?>.Create(p => p);

            Assert.True(command4.CanExecute(null));
            Assert.True(command4.CanExecute(string.Empty));
        }
    }
}
