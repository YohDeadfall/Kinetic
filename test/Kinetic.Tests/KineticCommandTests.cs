using System;
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
    }
}
