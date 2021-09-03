using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Kinetic
{
    public abstract class KineticCommand<TParameter, TResult> : KineticObservable<TResult>, ICommand
    {
        private readonly bool _optionalParameter;
        private protected KineticCommand(bool optionalParameter) =>
            _optionalParameter = optionalParameter;

        public sealed override string ToString() =>
            nameof(KineticCommand) + "<" + typeof(TParameter) + ", " + typeof(TResult) + ">";

        public event EventHandler? CanExecuteChanged;

        public abstract bool CanExecute(TParameter parameter);

        public abstract void Execute(TParameter parameter);

        public abstract void Dispose();

        bool ICommand.CanExecute(object? parameter)
        {
            return
                KineticCommand<TParameter>.UnboxParameter(parameter, out var unboxed, _optionalParameter) &&
                CanExecute(unboxed);
        }

        void ICommand.Execute(object? parameter)
        {
            if (KineticCommand<TParameter>.UnboxParameter(parameter, out var unboxed, _optionalParameter))
            {
                Execute(unboxed);
            }
            else
            {
                throw parameter is null
                    ? new ArgumentNullException(nameof(parameter))
                    : new ArgumentException(nameof(parameter));
            }
        }

        private protected void OnCanExecuteChanged() =>
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    internal interface IKineticFunction<T1, T2, TResult>
    {
        TResult Invoke(T1 value1, T2 value2);
    }

    internal abstract class KineticCommandBase<TExecute, TEnabled, TState, TParameter, TResult>
        : KineticCommand<TParameter, TResult>, IObserver<TState>
        where TExecute : struct
        where TEnabled : struct, IKineticFunction<TState, TParameter, bool>
    {
        protected readonly TExecute _execute;
        protected readonly TEnabled _enabled;
        protected TState _state;

        private IDisposable? _subscription;
        private bool _canExecute;

        protected KineticCommandBase(TState state, TExecute execute, TEnabled enabled, bool optionalParameter)
            : base(optionalParameter)
        {
            _execute = execute;
            _enabled = enabled;
            _state = state;
            _canExecute = true;
        }

        protected KineticCommandBase(IObservable<TState>? state, TExecute execute, TEnabled enabled, bool optionalParameter)
            : base(optionalParameter)
        {
            _execute = execute;
            _enabled = enabled;
            _state = default!;
            _subscription = state?.Subscribe(this);
            _canExecute = true;
        }

        public sealed override bool CanExecute(TParameter parameter) =>
            _canExecute &&
            _enabled.Invoke(_state, parameter);

        public sealed override void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
            _canExecute = false;
        }

        void IObserver<TState>.OnNext(TState value)
        {
            _state = value;

            if (_canExecute)
            {
                OnCanExecuteChanged();
            }
        }

        void IObserver<TState>.OnError(Exception error) => _canExecute = false;
        void IObserver<TState>.OnCompleted() => _canExecute = false;

        protected void OnCanExecuteChanged(bool value)
        {
            if (_canExecute != value)
            {
                _canExecute = value;
                OnCanExecuteChanged();
            }
        }
    }

    internal sealed class KineticCommand<TExecute, TEnabled, TState, TParameter, TResult>
        : KineticCommandBase<TExecute, TEnabled, TState, TParameter, TResult>
        where TExecute : struct, IKineticFunction<TState, TParameter, TResult>
        where TEnabled : struct, IKineticFunction<TState, TParameter, bool>
    {
        public KineticCommand(TState state, TExecute execute, TEnabled enabled, bool optionalParameter)
            : base(state, execute, enabled, optionalParameter) { }

        public KineticCommand(IObservable<TState>? state, TExecute execute, TEnabled enabled, bool optionalParameter)
            : base(state, execute, enabled, optionalParameter) { }

        public override void Execute(TParameter parameter)
        {
            var state = _state;
            if (_enabled.Invoke(state, parameter))
            {
                try
                {
                    OnNext(_execute.Invoke(state, parameter));
                }
                catch (Exception error)
                {
                    OnError(error);
                    throw;
                }
            }
            else
            {
                throw new InvalidOperationException();   
            }
        }
    }

    internal sealed class KineticTaskCommand<TExecute, TEnabled, TState, TParameter>
        : KineticCommandBase<TExecute, TEnabled, TState, TParameter, Unit>
        where TExecute : struct, IKineticFunction<TState, TParameter, Task>
        where TEnabled : struct, IKineticFunction<TState, TParameter, bool>
    {
        private readonly bool _awaitCompletion;

        public KineticTaskCommand(TState state, TExecute execute, TEnabled enabled, bool optionalParameter, bool awaitCompletion)
            : base(state, execute, enabled, optionalParameter) => _awaitCompletion = awaitCompletion;

        public KineticTaskCommand(IObservable<TState>? state, TExecute execute, TEnabled enabled, bool optionalParameter, bool awaitCompletion)
            : base(state, execute, enabled, optionalParameter) => _awaitCompletion = awaitCompletion;

        public override void Execute(TParameter parameter)
        {
            var state = _state;
            if (_enabled.Invoke(state, parameter))
            {
                OnCanExecuteChanged(_awaitCompletion);
                try
                {
                    _execute
                        .Invoke(state, parameter)
                        .ContinueWith(state: this, continuationAction: static (task, state) =>
                        {
                            var command = Unsafe.As<KineticTaskCommand<TExecute, TEnabled, TState, TParameter>>(state);
                            var awaiter = task.GetAwaiter();
                            try
                            {
                                awaiter.GetResult();
                                command.OnNext(default);
                            }
                            catch (Exception error)
                            {
                                command.OnError(error);
                            }

                            command.OnCanExecuteChanged(true);
                        });
                }
                catch (Exception error)
                {
                    OnError(error);
                    throw;
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }

    internal sealed class KineticTaskCommand<TExecute, TEnabled, TState, TParameter, TResult>
        : KineticCommandBase<TExecute, TEnabled, TState, TParameter, TResult>
        where TExecute : struct, IKineticFunction<TState, TParameter, Task<TResult>>
        where TEnabled : struct, IKineticFunction<TState, TParameter, bool>
    {
        private readonly bool _awaitCompletion;

        public KineticTaskCommand(TState state, TExecute execute, TEnabled enabled, bool optionalParameter, bool awaitCompletion)
            : base(state, execute, enabled, optionalParameter) => _awaitCompletion = awaitCompletion;

        public KineticTaskCommand(IObservable<TState>? state, TExecute execute, TEnabled enabled, bool optionalParameter, bool awaitCompletion)
            : base(state, execute, enabled, optionalParameter) => _awaitCompletion = awaitCompletion;

        public override void Execute(TParameter parameter)
        {
            var state = _state;
            if (_enabled.Invoke(state, parameter))
            {
                OnCanExecuteChanged(_awaitCompletion);
                try
                {
                    _execute
                        .Invoke(state, parameter)
                        .ContinueWith(state: this, continuationAction: static (task, state) =>
                        {
                            var command = Unsafe.As<KineticTaskCommand<TExecute, TEnabled, TState, TParameter, TResult>>(state);
                            var awaiter = task.GetAwaiter();
                            try
                            {
                                command.OnNext(awaiter.GetResult());
                            }
                            catch (Exception error)
                            {
                                command.OnError(error);
                            }

                            command.OnCanExecuteChanged(true);
                        });
                }
                catch (Exception error)
                {
                    OnError(error);
                    throw;
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
    
    public static class KineticCommand
    {
        private static readonly ConcurrentDictionary<MethodInfo, bool> OptionalParameters = new();

        public static KineticCommand<Unit, Unit> Create(
            Action execute) =>
            Create(NoState, Execute(execute), EnabledAlways());

        public static KineticCommand<Unit, Unit> Create(
            Action execute, Func<bool> canExecute) =>
            Create(NoState, Execute(execute), Enabled(canExecute));

        public static KineticCommand<Unit, Unit> Create<TState>(
            TState state, Action<TState> execute) =>
            Create(state, Execute(execute), EnabledAlways<TState>());

        public static KineticCommand<Unit, Unit> Create<TState>(
            TState state, Action<TState> execute, Func<TState, bool> canExecute) =>
            Create(state, Execute(execute), Enabled(canExecute));

        public static KineticCommand<Unit, Unit> Create<TState>(
            IObservable<TState> state, Action<TState> execute) =>
            Create(state, Execute(execute), EnabledAlways<TState>());

        public static KineticCommand<Unit, Unit> Create<TState>(
            IObservable<TState> state, Action<TState> execute, Func<TState, bool> canExecute) =>
            Create(state, Execute(execute), Enabled(canExecute));

        public static KineticCommand<Unit, TResult> Create<TResult>(
            Func<TResult> execute) =>
            WithResult<TResult>.Create(NoState, Execute(execute), EnabledAlways<Unit>());

        public static KineticCommand<Unit, TResult> Create<TResult>(
            Func<TResult> execute, Func<bool> canExecute) =>
            WithResult<TResult>.Create(NoState, Execute(execute), Enabled(canExecute));

        public static KineticCommand<Unit, TResult> Create<TState, TResult>(
            TState state, Func<TState, TResult> execute) =>
            WithResult<TResult>.Create(state, Execute(execute), EnabledAlways<TState>());

        public static KineticCommand<Unit, TResult> Create<TState, TResult>(
            TState state, Func<TState, TResult> execute, Func<TState, bool> canExecute) =>
            WithResult<TResult>.Create(state, Execute(execute), Enabled(canExecute));

        public static KineticCommand<Unit, TResult> Create<TState, TResult>(
            IObservable<TState> state, Func<TState, TResult> execute) =>
            WithResult<TResult>.Create(state, Execute(execute), EnabledAlways<TState>());

        public static KineticCommand<Unit, TResult> Create<TState, TResult>(
            IObservable<TState> state, Func<TState, TResult> execute, Func<TState, bool> canExecute) =>
            WithResult<TResult>.Create(state, Execute(execute), Enabled(canExecute));

        public static KineticCommand<Unit, Unit> CreateForTask(
            Func<Task> execute, bool awaitCompletion = true) =>
            CreateForTask(NoState, Execute(execute), EnabledAlways(), awaitCompletion);

        public static KineticCommand<Unit, Unit> CreateForTask(
            Func<Task> execute, Func<bool> canExecute, bool awaitCompletion = true) =>
            CreateForTask(NoState, Execute(execute), Enabled(canExecute), awaitCompletion);

        public static KineticCommand<Unit, Unit> CreateForTask<TState>(
            TState state, Func<TState, Task> execute, bool awaitCompletion = true) =>
            CreateForTask(state, Execute(execute), EnabledAlways<TState>(), awaitCompletion);

        public static KineticCommand<Unit, Unit> CreateForTask<TState>(
            TState state, Func<TState, Task> execute, Func<TState, bool> canExecute, bool awaitCompletion = true) =>
            CreateForTask(state, Execute(execute), Enabled(canExecute), awaitCompletion);

        public static KineticCommand<Unit, Unit> CreateForTask<TState>(
            IObservable<TState> state, Func<TState, Task> execute, bool awaitCompletion = true) =>
            CreateForTask(state, Execute(execute), EnabledAlways<TState>(), awaitCompletion);

        public static KineticCommand<Unit, Unit> CreateForTask<TState>(
            IObservable<TState> state, Func<TState, Task> execute, Func<TState, bool> canExecute, bool awaitCompletion = true) =>
            CreateForTask(state, Execute(execute), Enabled(canExecute), awaitCompletion);

        public static KineticCommand<Unit, TResult> CreateForTask<TResult>(
            Func<Task<TResult>> execute, bool awaitCompletion = true) =>
            WithResult<TResult>.CreateForTask(NoState, Execute(execute), EnabledAlways<Unit>(), awaitCompletion);

        public static KineticCommand<Unit, TResult> CreateForTask<TResult>(
            Func<Task<TResult>> execute, Func<bool> canExecute, bool awaitCompletion = true) =>
            WithResult<TResult>.CreateForTask(NoState, Execute(execute), Enabled(canExecute), awaitCompletion);

        public static KineticCommand<Unit, TResult> CreateForTask<TState, TResult>(
            TState state, Func<TState, Task<TResult>> execute, bool awaitCompletion = true) =>
            WithResult<TResult>.CreateForTask(state, Execute(execute), EnabledAlways<TState>(), awaitCompletion);

        public static KineticCommand<Unit, TResult> CreateForTask<TState, TResult>(
            TState state, Func<TState, Task<TResult>> execute, Func<TState, bool> canExecute, bool awaitCompletion = true) =>
            WithResult<TResult>.CreateForTask(state, Execute(execute), Enabled(canExecute), awaitCompletion);

        public static KineticCommand<Unit, TResult> CreateForTask<TState, TResult>(
            IObservable<TState> state, Func<TState, Task<TResult>> execute, bool awaitCompletion = true) =>
            WithResult<TResult>.CreateForTask(state, Execute(execute), EnabledAlways<TState>(), awaitCompletion);

        public static KineticCommand<Unit, TResult> CreateForTask<TState, TResult>(
            IObservable<TState> state, Func<TState, Task<TResult>> execute, Func<TState, bool> canExecute, bool awaitCompletion = true) =>
            WithResult<TResult>.CreateForTask(state, Execute(execute), Enabled(canExecute), awaitCompletion);

        private static Unit NoState => default;

        private static ExecuteAction Execute(Action execute) => new(execute);
        private static ExecuteAction<TState> Execute<TState>(Action<TState> execute) => new(execute);

        private static Function<TResult> Execute<TResult>(Func<TResult> execute) => new(execute);
        private static Function<TState, TResult> Execute<TState, TResult>(Func<TState, TResult> execute) => new(execute);

        private static Function<bool> Enabled(Func<bool> enabled) => new(enabled);
        private static Function<TState, bool> Enabled<TState>(Func<TState, bool> enabled) => new(enabled);

        private static EnabledAlwaysFunction<Unit> EnabledAlways() => default;
        private static EnabledAlwaysFunction<TState> EnabledAlways<TState>() => default;

        private readonly struct ExecuteAction : IKineticFunction<Unit, Unit, Unit>
        {
            public readonly Action Execute;
            public ExecuteAction(Action execute) => Execute = execute;

            public Unit Invoke(Unit state, Unit parameter)
            {
                Execute();
                return default;
            }
        }

        private readonly struct ExecuteAction<TState> : IKineticFunction<TState, Unit, Unit>
        {
            public readonly Action<TState> Execute;
            public ExecuteAction(Action<TState> execute) => Execute = execute;

            public Unit Invoke(TState state, Unit parameter)
            {
                Execute(state);
                return default;
            }
        }

        private readonly struct Function<TResult> : IKineticFunction<Unit, Unit, TResult>
        {
            public readonly Func<TResult> Method;
            public Function(Func<TResult> method) => Method = method;
            public TResult Invoke(Unit state, Unit parameter) => Method();
        }

        private readonly struct Function<TState, TResult> : IKineticFunction<TState, Unit, TResult>
        {
            public readonly Func<TState, TResult> Method;
            public Function(Func<TState, TResult> method) => Method = method;
            public TResult Invoke(TState state, Unit parameter) => Method(state);
        }

        private readonly struct EnabledAlwaysFunction<TState> : IKineticFunction<TState, Unit, bool>
        {
            public bool Invoke(TState state, Unit parameter) => true;
        }

        private static KineticCommand<Unit, Unit> Create<TExecute, TEnabled, TState>(
            TState state, TExecute execute, TEnabled enabled)
            where TExecute : struct, IKineticFunction<TState, Unit, Unit>
            where TEnabled : struct, IKineticFunction<TState, Unit, bool>
        {
            return new KineticCommand<TExecute, TEnabled, TState, Unit, Unit>(
                state, execute, enabled, optionalParameter: false);
        }

        private static KineticCommand<Unit, Unit> Create<TExecute, TEnabled, TState>(
            IObservable<TState> state, TExecute execute, TEnabled enabled)
            where TExecute : struct, IKineticFunction<TState, Unit, Unit>
            where TEnabled : struct, IKineticFunction<TState, Unit, bool>
        {
            return new KineticCommand<TExecute, TEnabled, TState, Unit, Unit>(
                state, execute, enabled, optionalParameter: false);
        }

        private static KineticCommand<Unit, Unit> CreateForTask<TExecute, TEnabled, TState>(
            TState state, TExecute execute, TEnabled enabled, bool awaitCompletion)
            where TExecute : struct, IKineticFunction<TState, Unit, Task>
            where TEnabled : struct, IKineticFunction<TState, Unit, bool>
        {
            return new KineticTaskCommand<TExecute, TEnabled, TState, Unit>(
                state, execute, enabled, optionalParameter: false, awaitCompletion);
        }

        private static KineticCommand<Unit, Unit> CreateForTask<TExecute, TEnabled, TState>(
            IObservable<TState> state, TExecute execute, TEnabled enabled, bool awaitCompletion)
            where TExecute : struct, IKineticFunction<TState, Unit, Task>
            where TEnabled : struct, IKineticFunction<TState, Unit, bool>
        {
            return new KineticTaskCommand<TExecute, TEnabled, TState, Unit>(
                state, execute, enabled, optionalParameter: false, awaitCompletion);
        }

        private static class WithResult<TResult>
        {
            public static KineticCommand<Unit, TResult> Create<TExecute, TEnabled, TState>(
                TState state, TExecute execute, TEnabled enabled)
                where TExecute : struct, IKineticFunction<TState, Unit, TResult>
                where TEnabled : struct, IKineticFunction<TState, Unit, bool>
            {
                return new KineticCommand<TExecute, TEnabled, TState, Unit, TResult>(
                    state, execute, enabled, optionalParameter: false);
            }
            
            public static KineticCommand<Unit, TResult> Create<TExecute, TEnabled, TState>(
                IObservable<TState> state, TExecute execute, TEnabled enabled)
                where TExecute : struct, IKineticFunction<TState, Unit, TResult>
                where TEnabled : struct, IKineticFunction<TState, Unit, bool>
            {
                return new KineticCommand<TExecute, TEnabled, TState, Unit, TResult>(
                    state, execute, enabled, optionalParameter: false);
            }

            public static KineticCommand<Unit, TResult> CreateForTask<TExecute, TEnabled, TState>(
                TState state, TExecute execute, TEnabled enabled, bool awaitCompletion)
                where TExecute : struct, IKineticFunction<TState, Unit, Task<TResult>>
                where TEnabled : struct, IKineticFunction<TState, Unit, bool>
            {
                return new KineticTaskCommand<TExecute, TEnabled, TState, Unit, TResult>(
                    state, execute, enabled, optionalParameter: false, awaitCompletion);
            }
            
            public static KineticCommand<Unit, TResult> CreateForTask<TExecute, TEnabled, TState>(
                IObservable<TState> state, TExecute execute, TEnabled enabled, bool awaitCompletion)
                where TExecute : struct, IKineticFunction<TState, Unit, Task<TResult>>
                where TEnabled : struct, IKineticFunction<TState, Unit, bool>
            {
                return new KineticTaskCommand<TExecute, TEnabled, TState, Unit, TResult>(
                    state, execute, enabled, optionalParameter: false, awaitCompletion);
            }
        }

        public static bool CanExecute<TResult>(this KineticCommand<Unit, TResult> command) =>
            command.CanExecute(default);

        public static void Execute<TResult>(this KineticCommand<Unit, TResult> command) =>
            command.Execute(default);

        internal static bool OptionalParameter(Delegate execute) =>
            OptionalParameters.GetOrAdd(execute.Method, method =>
            {
                var parameter = method
                    .GetParameters()
                    .LastOrDefault();
                if (parameter is null)
                {
                    return false;
                }

                if (parameter.ParameterType.IsValueType)
                {
                    return Nullable.GetUnderlyingType(parameter.ParameterType) is not null;
                }

                var argument = parameter
                    .GetCustomAttributesData()
                    .FirstOrDefault(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute")?
                    .ConstructorArguments
                    .FirstOrDefault();;

                return
                    argument?.Value is byte nullability &&
                    nullability == 2;
            });
    }

    public static class KineticCommand<TParameter>
    {
        public static KineticCommand<TParameter, Unit> Create(
            Action<TParameter> execute) =>
            Create(NoState, Execute(execute), EnabledAlways(), OptionalParameter(execute));

        public static KineticCommand<TParameter, Unit> Create(
            Action<TParameter> execute, Func<TParameter, bool> canExecute) =>
            Create(NoState, Execute(execute), Enabled(canExecute), OptionalParameter(execute));

        public static KineticCommand<TParameter, Unit> Create<TState>(
            TState state, Action<TState, TParameter> execute) =>
            Create(state, Execute(execute), EnabledAlways<TState>(), OptionalParameter(execute));

        public static KineticCommand<TParameter, Unit> Create<TState>(
            TState state, Action<TState, TParameter> execute, Func<TState, TParameter, bool> canExecute) =>
            Create(state, Execute(execute), Enabled(canExecute), OptionalParameter(execute));

        public static KineticCommand<TParameter, Unit> Create<TState>(
            IObservable<TState> state, Action<TState, TParameter> execute) =>
            Create(state, Execute(execute), EnabledAlways<TState>(), OptionalParameter(execute));

        public static KineticCommand<TParameter, Unit> Create<TState>(
            IObservable<TState> state, Action<TState, TParameter> execute, Func<TState, TParameter, bool> canExecute) =>
            Create(state, Execute(execute), Enabled(canExecute), OptionalParameter(execute));
            
        public static KineticCommand<TParameter, TResult> Create<TResult>(
            Func<TParameter, TResult> execute) =>
            WithResult<TResult>.Create(NoState, Execute(execute), EnabledAlways(), OptionalParameter(execute));

        public static KineticCommand<TParameter, TResult> Create<TResult>(
            Func<TParameter, TResult> execute, Func<TParameter, bool> canExecute) =>
            WithResult<TResult>.Create(NoState, Execute(execute), Enabled(canExecute), OptionalParameter(execute));

        public static KineticCommand<TParameter, TResult> Create<TState, TResult>(
            TState state, Func<TState, TParameter, TResult> execute) =>
            WithResult<TResult>.Create(state, Execute(execute), EnabledAlways<TState>(), OptionalParameter(execute));

        public static KineticCommand<TParameter, TResult> Create<TState, TResult>(
            TState state, Func<TState, TParameter, TResult> execute, Func<TState, TParameter, bool> canExecute) =>
            WithResult<TResult>.Create(state, Execute(execute), Enabled(canExecute), OptionalParameter(execute));

        public static KineticCommand<TParameter, TResult> Create<TState, TResult>(
            IObservable<TState> state, Func<TState, TParameter, TResult> execute) =>
            WithResult<TResult>.Create(state, Execute(execute), EnabledAlways<TState>(), OptionalParameter(execute));

        public static KineticCommand<TParameter, TResult> Create<TState, TResult>(
            IObservable<TState> state, Func<TState, TParameter, TResult> execute, Func<TState, TParameter, bool> canExecute) =>
            WithResult<TResult>.Create(state, Execute(execute), Enabled(canExecute), OptionalParameter(execute));
            
        public static KineticCommand<TParameter, Unit> CreateForTask(
            Func<TParameter, Task> execute, bool awaitCompletion = true) =>
            CreateForTask(NoState, Execute(execute), EnabledAlways(), OptionalParameter(execute), awaitCompletion);

        public static KineticCommand<TParameter, Unit> CreateForTask(
            Func<TParameter, Task> execute, Func<TParameter, bool> canExecute, bool awaitCompletion = true) =>
            CreateForTask(NoState, Execute(execute), Enabled(canExecute), OptionalParameter(execute), awaitCompletion);

        public static KineticCommand<TParameter, Unit> CreateForTask<TState>(
            TState state, Func<TState, TParameter, Task> execute, bool awaitCompletion = true) =>
            CreateForTask(state, Execute(execute), EnabledAlways<TState>(), OptionalParameter(execute), awaitCompletion);

        public static KineticCommand<TParameter, Unit> CreateForTask<TState>(
            TState state, Func<TState, TParameter, Task> execute, Func<TState, TParameter, bool> canExecute, bool awaitCompletion = true) =>
            CreateForTask(state, Execute(execute), Enabled(canExecute), OptionalParameter(execute), awaitCompletion);

        public static KineticCommand<TParameter, Unit> CreateForTask<TState>(
            IObservable<TState> state, Func<TState, TParameter, Task> execute, bool awaitCompletion = true) =>
            CreateForTask(state, Execute(execute), EnabledAlways<TState>(), OptionalParameter(execute), awaitCompletion);

        public static KineticCommand<TParameter, Unit> CreateForTask<TState>(
            IObservable<TState> state, Func<TState, TParameter, Task> execute, Func<TState, TParameter, bool> canExecute, bool awaitCompletion = true) =>
            CreateForTask(state, Execute(execute), Enabled(canExecute), OptionalParameter(execute), awaitCompletion);
            
        public static KineticCommand<TParameter, TResult> CreateForTask<TResult>(
            Func<TParameter, Task<TResult>> execute, bool awaitCompletion = true) =>
            WithResult<TResult>.CreateForTask(NoState, Execute(execute), EnabledAlways(), OptionalParameter(execute), awaitCompletion);

        public static KineticCommand<TParameter, TResult> CreateForTask<TResult>(
            Func<TParameter, Task<TResult>> execute, Func<TParameter, bool> canExecute, bool awaitCompletion = true) =>
            WithResult<TResult>.CreateForTask(NoState, Execute(execute), Enabled(canExecute), OptionalParameter(execute), awaitCompletion);

        public static KineticCommand<TParameter, TResult> CreateForTask<TState, TResult>(
            TState state, Func<TState, TParameter, Task<TResult>> execute, bool awaitCompletion = true) =>
            WithResult<TResult>.CreateForTask(state, Execute(execute), EnabledAlways<TState>(), OptionalParameter(execute), awaitCompletion);

        public static KineticCommand<TParameter, TResult> CreateForTask<TState, TResult>(
            TState state, Func<TState, TParameter, Task<TResult>> execute, Func<TState, TParameter, bool> canExecute, bool awaitCompletion = true) =>
            WithResult<TResult>.CreateForTask(state, Execute(execute), Enabled(canExecute), OptionalParameter(execute), awaitCompletion);

        public static KineticCommand<TParameter, TResult> CreateForTask<TState, TResult>(
            IObservable<TState> state, Func<TState, TParameter, Task<TResult>> execute, bool awaitCompletion = true) =>
            WithResult<TResult>.CreateForTask(state, Execute(execute), EnabledAlways<TState>(), OptionalParameter(execute), awaitCompletion);

        public static KineticCommand<TParameter, TResult> CreateForTask<TState, TResult>(
            IObservable<TState> state, Func<TState, TParameter, Task<TResult>> execute, Func<TState, TParameter, bool> canExecute, bool awaitCompletion = true) =>
            WithResult<TResult>.CreateForTask(state, Execute(execute), Enabled(canExecute), OptionalParameter(execute), awaitCompletion);

        private static Unit NoState => default;

        private static ExecuteAction Execute(Action<TParameter> execute) => new(execute);
        private static ExecuteAction<TState> Execute<TState>(Action<TState, TParameter> execute) => new(execute);

        private static Function<TResult> Execute<TResult>(Func<TParameter, TResult> execute) => new(execute);
        private static Function<TState, TResult> Execute<TState, TResult>(Func<TState, TParameter, TResult> execute) => new(execute);

        private static Function<bool> Enabled(Func<TParameter, bool> enabled) => new(enabled);
        private static Function<TState, bool> Enabled<TState>(Func<TState, TParameter, bool> enabled) => new(enabled);

        private static EnabledAlwaysFunction<Unit> EnabledAlways() => default;
        private static EnabledAlwaysFunction<TState> EnabledAlways<TState>() => default;

        private readonly struct ExecuteAction : IKineticFunction<Unit, TParameter, Unit>
        {
            public readonly Action<TParameter> Execute;
            public ExecuteAction(Action<TParameter> execute) => Execute = execute;

            public Unit Invoke(Unit state, TParameter parameter)
            {
                Execute(parameter);
                return default;
            }
        }

        private readonly struct ExecuteAction<TState> : IKineticFunction<TState, TParameter, Unit>
        {
            public readonly Action<TState, TParameter> Execute;
            public ExecuteAction(Action<TState, TParameter> execute) => Execute = execute;

            public Unit Invoke(TState state, TParameter parameter)
            {
                Execute(state, parameter);
                return default;
            }
        }

        private readonly struct Function<TResult> : IKineticFunction<Unit, TParameter, TResult>
        {
            public readonly Func<TParameter, TResult> Method;
            public Function(Func<TParameter, TResult> method) => Method = method;
            public TResult Invoke(Unit state, TParameter parameter) => Method(parameter);
        }

        private readonly struct Function<TState, TResult> : IKineticFunction<TState, TParameter, TResult>
        {
            public readonly Func<TState, TParameter, TResult> Method;
            public Function(Func<TState, TParameter, TResult> method) => Method = method;
            public TResult Invoke(TState state, TParameter parameter) => Method(state, parameter);
        }

        private readonly struct EnabledAlwaysFunction<TState> : IKineticFunction<TState, TParameter, bool>
        {
            public bool Invoke(TState state, TParameter parameter) => true;
        }

        private static KineticCommand<TParameter, Unit> Create<TExecute, TEnabled, TState>(
            TState state, TExecute execute, TEnabled enabled, bool optionalParameter)
            where TExecute : struct, IKineticFunction<TState, TParameter, Unit>
            where TEnabled : struct, IKineticFunction<TState, TParameter, bool>
        {
            return new KineticCommand<TExecute, TEnabled, TState, TParameter, Unit>(
                state, execute, enabled, optionalParameter);
        }

        private static KineticCommand<TParameter, Unit> Create<TExecute, TEnabled, TState>(
            IObservable<TState> state, TExecute execute, TEnabled enabled, bool optionalParameter)
            where TExecute : struct, IKineticFunction<TState, TParameter, Unit>
            where TEnabled : struct, IKineticFunction<TState, TParameter, bool>
        {
            return new KineticCommand<TExecute, TEnabled, TState, TParameter, Unit>(
                state, execute, enabled, optionalParameter);
        }

        private static KineticCommand<TParameter, Unit> CreateForTask<TExecute, TEnabled, TState>(
            TState state, TExecute execute, TEnabled enabled, bool optionalParameter, bool awaitCompletion)
            where TExecute : struct, IKineticFunction<TState, TParameter, Task>
            where TEnabled : struct, IKineticFunction<TState, TParameter, bool>
        {
            return new KineticTaskCommand<TExecute, TEnabled, TState, TParameter>(
                state, execute, enabled, optionalParameter, awaitCompletion);
        }

        private static KineticCommand<TParameter, Unit> CreateForTask<TExecute, TEnabled, TState>(
            IObservable<TState> state, TExecute execute, TEnabled enabled, bool optionalParameter, bool awaitCompletion)
            where TExecute : struct, IKineticFunction<TState, TParameter, Task>
            where TEnabled : struct, IKineticFunction<TState, TParameter, bool>
        {
            return new KineticTaskCommand<TExecute, TEnabled, TState, TParameter>(
                state, execute, enabled, optionalParameter, awaitCompletion);
        }

        private static class WithResult<TResult>
        {
            public static KineticCommand<TParameter, TResult> Create<TExecute, TEnabled, TState>(
                TState state, TExecute execute, TEnabled enabled, bool optionalParameter)
                where TExecute : struct, IKineticFunction<TState, TParameter, TResult>
                where TEnabled : struct, IKineticFunction<TState, TParameter, bool>
            {
                return new KineticCommand<TExecute, TEnabled, TState, TParameter, TResult>(
                    state, execute, enabled, optionalParameter);
            }
            
            public static KineticCommand<TParameter, TResult> Create<TExecute, TEnabled, TState>(
                IObservable<TState> state, TExecute execute, TEnabled enabled, bool optionalParameter)
                where TExecute : struct, IKineticFunction<TState, TParameter, TResult>
                where TEnabled : struct, IKineticFunction<TState, TParameter, bool>
            {
                return new KineticCommand<TExecute, TEnabled, TState, TParameter, TResult>(
                    state, execute, enabled, optionalParameter);
            }

            public static KineticCommand<TParameter, TResult> CreateForTask<TExecute, TEnabled, TState>(
                TState state, TExecute execute, TEnabled enabled, bool optionalParameter, bool awaitCompletion)
                where TExecute : struct, IKineticFunction<TState, TParameter, Task<TResult>>
                where TEnabled : struct, IKineticFunction<TState, TParameter, bool>
            {
                return new KineticTaskCommand<TExecute, TEnabled, TState, TParameter, TResult>(
                    state, execute, enabled, optionalParameter, awaitCompletion);
            }
            
            public static KineticCommand<TParameter, TResult> CreateForTask<TExecute, TEnabled, TState>(
                IObservable<TState> state, TExecute execute, TEnabled enabled, bool optionalParameter, bool awaitCompletion)
                where TExecute : struct, IKineticFunction<TState, TParameter, Task<TResult>>
                where TEnabled : struct, IKineticFunction<TState, TParameter, bool>
            {
                return new KineticTaskCommand<TExecute, TEnabled, TState, TParameter, TResult>(
                    state, execute, enabled, optionalParameter, awaitCompletion);
            }
        }

        internal static bool OptionalParameter(Delegate method) =>
            typeof(TParameter).IsValueType
            ? default(TParameter) is null
            : KineticCommand.OptionalParameter(method);

        internal static bool UnboxParameter(object? boxed, [NotNullWhen(true)] out TParameter? unboxed, bool allowNull)
        {
            if (typeof(TParameter) == typeof(Unit))
            {
                unboxed = default!;
                return true;
            }
            if (boxed is TParameter parameter)
            {
                unboxed = parameter;
                return true;
            }
            else
            {
                unboxed = default;
                return boxed is null && allowNull;
            }
        }
    }
}
