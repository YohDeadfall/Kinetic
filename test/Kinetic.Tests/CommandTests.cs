using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xunit;

namespace Kinetic.Tests
{
    public class CommandTests
    {
        [Fact]
        public void ExecuteAction()
        {
            var executions = 0;
            var command = Command.Create(
                execute: () => { executions += 1; });
            var result = command
                .FirstOrDefaultAsync()
                .GetAwaiter();

            command.Execute();

            Assert.True(result.IsCompleted);
            Assert.Equal(default, result.GetResult());
            Assert.Equal(1, executions);
        }

        [Fact]
        public void ExecuteActionWithParameter()
        {
            var executions = 0;
            var parameter = 0;
            var command = Command<int>.Create(
                execute: p =>
                {
                    parameter = p;
                    executions += 1;
                });
            var result = command
                .FirstOrDefaultAsync()
                .GetAwaiter();

            command.Execute(42);

            Assert.True(result.IsCompleted);
            Assert.Equal(default, result.GetResult());
            Assert.Equal(1, executions);
            Assert.Equal(42, parameter);
        }

        [Fact]
        public void ExecuteActionStatePreserved()
        {
            var state = new State();
            var command = Command.Create(
                state.CanExecute.Changed,
                execute: s => { Assert.True(s); },
                canExecute: s => { state.CanExecute.Set(false); return s; });
            var result = command
                .FirstOrDefaultAsync()
                .GetAwaiter();

            state.CanExecute.Set(true);
            command.Execute();

            Assert.True(result.IsCompleted);
            Assert.Equal(default, result.GetResult());
        }

        [Fact]
        public async ValueTask ExecuteActionAsync()
        {
            var executions = 0;
            var command = Command.CreateForTask(
                execute: async () =>
                {
                    await Task.Yield();
                    executions += 1;
                });
            var result = command
                .FirstOrDefaultAsync();

            command.Execute();

            Assert.Equal(default, await result);
            Assert.Equal(1, executions);
        }

        [Fact]
        public async ValueTask ExecuteActionAsyncWithParameter()
        {
            var executions = 0;
            var parameter = 0;
            var command = Command<int>.CreateForTask(
                execute: async p =>
                {
                    await Task.Yield();
                    parameter = p;
                    executions += 1;
                });
            var result = command
                .FirstOrDefaultAsync();

            command.Execute(42);

            Assert.Equal(default, await result);
            Assert.Equal(1, executions);
            Assert.Equal(42, parameter);
        }

        [Fact]
        public async ValueTask ExecuteActionAsyncStatePreserved()
        {
            var state = new State();
            var command = Command.CreateForTask(
                state.CanExecute.Changed,
                execute: async s => { await Task.Yield(); Assert.True(s); },
                canExecute: s => { state.CanExecute.Set(false); return s; });
            var result = command
                .FirstOrDefaultAsync();

            state.CanExecute.Set(true);
            command.Execute();

            Assert.Equal(default, await result);
        }

        [Fact]
        public void ExecuteFunction()
        {
            var executions = 0;
            var command = Command.Create(
                execute: () => executions += 1);
            var result = command
                .FirstOrDefaultAsync()
                .GetAwaiter();

            command.Execute();

            Assert.True(result.IsCompleted);
            Assert.Equal(1, result.GetResult());
            Assert.Equal(1, executions);
        }

        [Fact]
        public void ExecuteFunctionWithParameter()
        {
            var executions = 0;
            var parameter = 0;
            var command = Command<int>.Create(
                execute: p =>
                {
                    parameter = p;
                    return executions += 1;
                });
            var result = command
                .FirstOrDefaultAsync()
                .GetAwaiter();

            command.Execute(42);

            Assert.True(result.IsCompleted);
            Assert.Equal(1, result.GetResult());
            Assert.Equal(1, executions);
            Assert.Equal(42, parameter);
        }

        [Fact]
        public void ExecuteFunctionStatePreserved()
        {
            var state = new State();
            var command = Command.Create(
                state.CanExecute.Changed,
                execute: s => { Assert.True(s); return s; },
                canExecute: s => { state.CanExecute.Set(false); return s; });
            var result = command
                .FirstOrDefaultAsync()
                .GetAwaiter();

            state.CanExecute.Set(true);
            command.Execute();

            Assert.True(result.IsCompleted);
            Assert.True(result.GetResult());
        }

        [Fact]
        public async ValueTask ExecuteFunctionAsync()
        {
            var executions = 0;
            var command = Command.CreateForTask(
                execute: async () =>
                {
                    await Task.Yield();
                    return executions += 1;
                });
            var result = command
                .FirstOrDefaultAsync();

            command.Execute();

            Assert.Equal(1, await result);
            Assert.Equal(1, executions);
        }

        [Fact]
        public async ValueTask ExecuteFunctionAsyncWithParameter()
        {
            var executions = 0;
            var parameter = 0;
            var command = Command<int>.CreateForTask(
                execute: async p =>
                {
                    await Task.Yield();
                    parameter = p;
                    return executions += 1;
                });
            var result = command
                .FirstOrDefaultAsync();

            command.Execute(42);

            Assert.Equal(1, await result);
            Assert.Equal(1, executions);
            Assert.Equal(42, parameter);
        }

        [Fact]
        public async ValueTask ExecuteFunctionAsyncStatePreserved()
        {
            var state = new State();
            var command = Command.CreateForTask(
                state.CanExecute.Changed,
                execute: async s => { await Task.Yield(); Assert.True(s); return s; },
                canExecute: s => { state.CanExecute.Set(false); return s; });
            var result = command
                .FirstOrDefaultAsync();

            state.CanExecute.Set(true);
            command.Execute();

            Assert.True(await result);
        }

        [Fact]
        public void CanExecuteAction()
        {
            var executions = 0;
            var validations = 0;
            var command = Command.Create(
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
            var command = Command<int>.Create(
                execute: p => { executions += 1; },
                canExecute: p => { validations += 1; return p == 0; });

            Assert.False(command.CanExecute(42));
            Assert.Equal(1, validations);

            Assert.Throws<InvalidOperationException>(() => command.Execute(42));
            Assert.Equal(0, executions);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async ValueTask CanExecuteActionAsync(bool awaitCompletion)
        {
            var executions = 0;
            var validations = 0;
            var state = new State();
            var semaphore = new SemaphoreSlim(1);
            var command = Command.CreateForTask(
                state.CanExecute.Changed,
                execute: async s => { await semaphore.WaitAsync(); executions += 1; },
                canExecute: s => { validations += 1; return s; },
                awaitCompletion);
            var result = command
                .FirstOrDefaultAsync();

            Assert.False(command.CanExecute());
            Assert.Equal(1, validations);

            Assert.Throws<InvalidOperationException>(() => command.Execute());
            Assert.Equal(0, executions);

            state.CanExecute.Set(true);
            semaphore.Wait(0);

            Assert.False(command.CanExecute());
            Assert.Equal(2, validations);

            command.Execute();

            Assert.Equal(awaitCompletion, !command.CanExecute());
            Assert.Equal(4, validations);

            semaphore.Release();
            await result;

            Assert.True(command.CanExecute());
            Assert.Equal(5, validations);
        }

        [Fact]
        public void CanExecuteFunction()
        {
            var executions = 0;
            var validations = 0;
            var command = Command.Create(
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
            var command = Command<int>.Create(
                execute: p => executions += 1,
                canExecute: p => { validations += 1; return p == 0; });

            Assert.False(command.CanExecute(42));
            Assert.Equal(1, validations);

            Assert.Throws<InvalidOperationException>(() => command.Execute(42));
            Assert.Equal(0, executions);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async ValueTask CanExecuteFunctionAsync(bool awaitCompletion)
        {
            var executions = 0;
            var validations = 0;
            var state = new State();
            var semaphore = new SemaphoreSlim(1);
            var command = Command.CreateForTask(
                state.CanExecute.Changed,
                execute: async s => { await semaphore.WaitAsync(); return executions += 1; },
                canExecute: s => { validations += 1; return s; },
                awaitCompletion);
            var result = command
                .FirstOrDefaultAsync();

            Assert.False(command.CanExecute());
            Assert.Equal(1, validations);

            Assert.Throws<InvalidOperationException>(() => command.Execute());
            Assert.Equal(0, executions);

            state.CanExecute.Set(true);
            semaphore.Wait(0);

            Assert.False(command.CanExecute());
            Assert.Equal(2, validations);

            command.Execute();

            Assert.Equal(awaitCompletion, !command.CanExecute());
            Assert.Equal(4, validations);

            semaphore.Release();
            await result;

            Assert.True(command.CanExecute());
            Assert.Equal(5, validations);
        }

        [Fact]
        public void CanExecuteChangedAction()
        {
            var state = new State();
            var events = 0;
            var command = Command.Create(
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
            var command = Command<bool>.Create(
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
            var command = Command.Create(
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
            var command = Command<bool>.Create(
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
            var command = Command.Create(
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
            var command = Command.Create(
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

        private sealed class State : Object
        {
            private bool _canExecute;
            private int _counter;

            public Property<bool> CanExecute => Property(ref _canExecute);
            public Property<int> Counter => Property(ref _counter);
        }

        [Fact]
        public void ParameterCheck()
        {
            ICommand command1 = Command<int>.Create(p => p);

            Assert.False(command1.CanExecute(null));
            Assert.False(command1.CanExecute(default(long)));
            Assert.True(command1.CanExecute(default(int)));

            ICommand command2 = Command<int?>.Create(p => p);

            Assert.True(command2.CanExecute(null));
            Assert.True(command2.CanExecute(default(int)));

            ICommand command3 = Command<string>.Create(p => p);

            Assert.False(command3.CanExecute(null));
            Assert.False(command1.CanExecute(default(long)));
            Assert.True(command3.CanExecute(string.Empty));

            ICommand command4 = Command<string?>.Create(p => p);

            Assert.True(command4.CanExecute(null));
            Assert.True(command4.CanExecute(string.Empty));
        }
    }
}