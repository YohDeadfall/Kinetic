using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Kinetic;

public abstract class Command<TParameter, TResult> : ICommand, IObservableInternal<TResult>
{
    private readonly bool _optionalParameter;
    private ObservableSubscriptions<TResult> _subscriptions;

    private protected Command(bool optionalParameter) =>
        _optionalParameter = optionalParameter;

    public sealed override string ToString() =>
        nameof(Command) + "<" + typeof(TParameter) + ", " + typeof(TResult) + ">";

    public event EventHandler? CanExecuteChanged;

    public abstract bool CanExecute(TParameter parameter);

    public abstract void Execute(TParameter parameter);

    public abstract void Dispose();

    bool ICommand.CanExecute(object? parameter)
    {
        return
            Command<TParameter>.UnboxParameter(parameter, out var unboxed, _optionalParameter) &&
            CanExecute(unboxed);
    }

    void ICommand.Execute(object? parameter)
    {
        if (Command<TParameter>.UnboxParameter(parameter, out var unboxed, _optionalParameter))
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

    public IDisposable Subscribe(IObserver<TResult> observer) =>
        _subscriptions.Subscribe(observer, this);

    void IObservableInternal<TResult>.Subscribe(ObservableSubscription<TResult> subscription) =>
        _subscriptions.Subscribe(subscription, this);

    void IObservableInternal<TResult>.Unsubscribe(ObservableSubscription<TResult> subscription) =>
        _subscriptions.Unsubscribe(subscription);

    private protected void OnNext(TResult value) =>
        _subscriptions.OnNext(value);

    private protected void OnError(Exception error) =>
        _subscriptions.OnError(error);

    private protected void OnCanExecuteChanged() =>
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

internal interface ICommandFunction<T1, T2, TResult>
{
    TResult Invoke(T1 value1, T2 value2);
}

internal abstract class CommandBase<TExecute, TEnabled, TState, TParameter, TResult>
    : Command<TParameter, TResult>, IObserver<TState>
    where TExecute : struct
    where TEnabled : struct, ICommandFunction<TState, TParameter, bool>
{
    protected readonly TExecute _execute;
    protected readonly TEnabled _enabled;
    protected TState _state;

    private IDisposable? _subscription;
    private bool _canExecute;

    protected CommandBase(TState state, TExecute execute, TEnabled enabled, bool optionalParameter)
        : base(optionalParameter)
    {
        _execute = execute;
        _enabled = enabled;
        _state = state;
        _canExecute = true;
    }

    protected CommandBase(IObservable<TState>? state, TExecute execute, TEnabled enabled, bool optionalParameter)
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

internal sealed class Command<TExecute, TEnabled, TState, TParameter, TResult>
    : CommandBase<TExecute, TEnabled, TState, TParameter, TResult>
    where TExecute : struct, ICommandFunction<TState, TParameter, TResult>
    where TEnabled : struct, ICommandFunction<TState, TParameter, bool>
{
    public Command(TState state, TExecute execute, TEnabled enabled, bool optionalParameter)
        : base(state, execute, enabled, optionalParameter) { }

    public Command(IObservable<TState>? state, TExecute execute, TEnabled enabled, bool optionalParameter)
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

internal sealed class TaskCommand<TExecute, TEnabled, TState, TParameter>
    : CommandBase<TExecute, TEnabled, TState, TParameter, ValueTuple>
    where TExecute : struct, ICommandFunction<TState, TParameter, Task>
    where TEnabled : struct, ICommandFunction<TState, TParameter, bool>
{
    private readonly bool _awaitCompletion;

    public TaskCommand(TState state, TExecute execute, TEnabled enabled, bool optionalParameter, bool awaitCompletion)
        : base(state, execute, enabled, optionalParameter) => _awaitCompletion = awaitCompletion;

    public TaskCommand(IObservable<TState>? state, TExecute execute, TEnabled enabled, bool optionalParameter, bool awaitCompletion)
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
                        var command = Unsafe.As<TaskCommand<TExecute, TEnabled, TState, TParameter>>(state!);
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

internal sealed class TaskCommand<TExecute, TEnabled, TState, TParameter, TResult>
    : CommandBase<TExecute, TEnabled, TState, TParameter, TResult>
    where TExecute : struct, ICommandFunction<TState, TParameter, Task<TResult>>
    where TEnabled : struct, ICommandFunction<TState, TParameter, bool>
{
    private readonly bool _awaitCompletion;

    public TaskCommand(TState state, TExecute execute, TEnabled enabled, bool optionalParameter, bool awaitCompletion)
        : base(state, execute, enabled, optionalParameter) => _awaitCompletion = awaitCompletion;

    public TaskCommand(IObservable<TState>? state, TExecute execute, TEnabled enabled, bool optionalParameter, bool awaitCompletion)
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
                        var command = Unsafe.As<TaskCommand<TExecute, TEnabled, TState, TParameter, TResult>>(state!);
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

public static class Command
{
    private static readonly ConcurrentDictionary<MethodInfo, bool> OptionalParameters = new();

    public static Command<ValueTuple, ValueTuple> Create(
        Action execute) =>
        Create(NoState, Execute(execute), EnabledAlways());

    public static Command<ValueTuple, ValueTuple> Create(
        Action execute, Func<bool> canExecute) =>
        Create(NoState, Execute(execute), Enabled(canExecute));

    public static Command<ValueTuple, ValueTuple> Create<TState>(
        TState state, Action<TState> execute) =>
        Create(state, Execute(execute), EnabledAlways<TState>());

    public static Command<ValueTuple, ValueTuple> Create<TState>(
        TState state, Action<TState> execute, Func<TState, bool> canExecute) =>
        Create(state, Execute(execute), Enabled(canExecute));

    public static Command<ValueTuple, ValueTuple> Create<TState>(
        IObservable<TState> state, Action<TState> execute) =>
        Create(state, Execute(execute), EnabledAlways<TState>());

    public static Command<ValueTuple, ValueTuple> Create<TState>(
        IObservable<TState> state, Action<TState> execute, Func<TState, bool> canExecute) =>
        Create(state, Execute(execute), Enabled(canExecute));

    public static Command<ValueTuple, TResult> Create<TResult>(
        Func<TResult> execute) =>
        WithResult<TResult>.Create(NoState, Execute(execute), EnabledAlways<ValueTuple>());

    public static Command<ValueTuple, TResult> Create<TResult>(
        Func<TResult> execute, Func<bool> canExecute) =>
        WithResult<TResult>.Create(NoState, Execute(execute), Enabled(canExecute));

    public static Command<ValueTuple, TResult> Create<TState, TResult>(
        TState state, Func<TState, TResult> execute) =>
        WithResult<TResult>.Create(state, Execute(execute), EnabledAlways<TState>());

    public static Command<ValueTuple, TResult> Create<TState, TResult>(
        TState state, Func<TState, TResult> execute, Func<TState, bool> canExecute) =>
        WithResult<TResult>.Create(state, Execute(execute), Enabled(canExecute));

    public static Command<ValueTuple, TResult> Create<TState, TResult>(
        IObservable<TState> state, Func<TState, TResult> execute) =>
        WithResult<TResult>.Create(state, Execute(execute), EnabledAlways<TState>());

    public static Command<ValueTuple, TResult> Create<TState, TResult>(
        IObservable<TState> state, Func<TState, TResult> execute, Func<TState, bool> canExecute) =>
        WithResult<TResult>.Create(state, Execute(execute), Enabled(canExecute));

    public static Command<ValueTuple, ValueTuple> CreateForTask(
        Func<Task> execute, bool awaitCompletion = true) =>
        CreateForTask(NoState, Execute(execute), EnabledAlways(), awaitCompletion);

    public static Command<ValueTuple, ValueTuple> CreateForTask(
        Func<Task> execute, Func<bool> canExecute, bool awaitCompletion = true) =>
        CreateForTask(NoState, Execute(execute), Enabled(canExecute), awaitCompletion);

    public static Command<ValueTuple, ValueTuple> CreateForTask<TState>(
        TState state, Func<TState, Task> execute, bool awaitCompletion = true) =>
        CreateForTask(state, Execute(execute), EnabledAlways<TState>(), awaitCompletion);

    public static Command<ValueTuple, ValueTuple> CreateForTask<TState>(
        TState state, Func<TState, Task> execute, Func<TState, bool> canExecute, bool awaitCompletion = true) =>
        CreateForTask(state, Execute(execute), Enabled(canExecute), awaitCompletion);

    public static Command<ValueTuple, ValueTuple> CreateForTask<TState>(
        IObservable<TState> state, Func<TState, Task> execute, bool awaitCompletion = true) =>
        CreateForTask(state, Execute(execute), EnabledAlways<TState>(), awaitCompletion);

    public static Command<ValueTuple, ValueTuple> CreateForTask<TState>(
        IObservable<TState> state, Func<TState, Task> execute, Func<TState, bool> canExecute, bool awaitCompletion = true) =>
        CreateForTask(state, Execute(execute), Enabled(canExecute), awaitCompletion);

    public static Command<ValueTuple, TResult> CreateForTask<TResult>(
        Func<Task<TResult>> execute, bool awaitCompletion = true) =>
        WithResult<TResult>.CreateForTask(NoState, Execute(execute), EnabledAlways<ValueTuple>(), awaitCompletion);

    public static Command<ValueTuple, TResult> CreateForTask<TResult>(
        Func<Task<TResult>> execute, Func<bool> canExecute, bool awaitCompletion = true) =>
        WithResult<TResult>.CreateForTask(NoState, Execute(execute), Enabled(canExecute), awaitCompletion);

    public static Command<ValueTuple, TResult> CreateForTask<TState, TResult>(
        TState state, Func<TState, Task<TResult>> execute, bool awaitCompletion = true) =>
        WithResult<TResult>.CreateForTask(state, Execute(execute), EnabledAlways<TState>(), awaitCompletion);

    public static Command<ValueTuple, TResult> CreateForTask<TState, TResult>(
        TState state, Func<TState, Task<TResult>> execute, Func<TState, bool> canExecute, bool awaitCompletion = true) =>
        WithResult<TResult>.CreateForTask(state, Execute(execute), Enabled(canExecute), awaitCompletion);

    public static Command<ValueTuple, TResult> CreateForTask<TState, TResult>(
        IObservable<TState> state, Func<TState, Task<TResult>> execute, bool awaitCompletion = true) =>
        WithResult<TResult>.CreateForTask(state, Execute(execute), EnabledAlways<TState>(), awaitCompletion);

    public static Command<ValueTuple, TResult> CreateForTask<TState, TResult>(
        IObservable<TState> state, Func<TState, Task<TResult>> execute, Func<TState, bool> canExecute, bool awaitCompletion = true) =>
        WithResult<TResult>.CreateForTask(state, Execute(execute), Enabled(canExecute), awaitCompletion);

    private static ValueTuple NoState => default;

    private static ExecuteAction Execute(Action execute) => new(execute);
    private static ExecuteAction<TState> Execute<TState>(Action<TState> execute) => new(execute);

    private static Function<TResult> Execute<TResult>(Func<TResult> execute) => new(execute);
    private static Function<TState, TResult> Execute<TState, TResult>(Func<TState, TResult> execute) => new(execute);

    private static Function<bool> Enabled(Func<bool> enabled) => new(enabled);
    private static Function<TState, bool> Enabled<TState>(Func<TState, bool> enabled) => new(enabled);

    private static EnabledAlwaysFunction<ValueTuple> EnabledAlways() => default;
    private static EnabledAlwaysFunction<TState> EnabledAlways<TState>() => default;

    private readonly struct ExecuteAction : ICommandFunction<ValueTuple, ValueTuple, ValueTuple>
    {
        public readonly Action Execute;
        public ExecuteAction(Action execute) => Execute = execute;

        public ValueTuple Invoke(ValueTuple state, ValueTuple parameter)
        {
            Execute();
            return default;
        }
    }

    private readonly struct ExecuteAction<TState> : ICommandFunction<TState, ValueTuple, ValueTuple>
    {
        public readonly Action<TState> Execute;
        public ExecuteAction(Action<TState> execute) => Execute = execute;

        public ValueTuple Invoke(TState state, ValueTuple parameter)
        {
            Execute(state);
            return default;
        }
    }

    private readonly struct Function<TResult> : ICommandFunction<ValueTuple, ValueTuple, TResult>
    {
        public readonly Func<TResult> Method;
        public Function(Func<TResult> method) => Method = method;
        public TResult Invoke(ValueTuple state, ValueTuple parameter) => Method();
    }

    private readonly struct Function<TState, TResult> : ICommandFunction<TState, ValueTuple, TResult>
    {
        public readonly Func<TState, TResult> Method;
        public Function(Func<TState, TResult> method) => Method = method;
        public TResult Invoke(TState state, ValueTuple parameter) => Method(state);
    }

    private readonly struct EnabledAlwaysFunction<TState> : ICommandFunction<TState, ValueTuple, bool>
    {
        public bool Invoke(TState state, ValueTuple parameter) => true;
    }

    private static Command<ValueTuple, ValueTuple> Create<TExecute, TEnabled, TState>(
        TState state, TExecute execute, TEnabled enabled)
        where TExecute : struct, ICommandFunction<TState, ValueTuple, ValueTuple>
        where TEnabled : struct, ICommandFunction<TState, ValueTuple, bool>
    {
        return new Command<TExecute, TEnabled, TState, ValueTuple, ValueTuple>(
            state, execute, enabled, optionalParameter: false);
    }

    private static Command<ValueTuple, ValueTuple> Create<TExecute, TEnabled, TState>(
        IObservable<TState> state, TExecute execute, TEnabled enabled)
        where TExecute : struct, ICommandFunction<TState, ValueTuple, ValueTuple>
        where TEnabled : struct, ICommandFunction<TState, ValueTuple, bool>
    {
        return new Command<TExecute, TEnabled, TState, ValueTuple, ValueTuple>(
            state, execute, enabled, optionalParameter: false);
    }

    private static Command<ValueTuple, ValueTuple> CreateForTask<TExecute, TEnabled, TState>(
        TState state, TExecute execute, TEnabled enabled, bool awaitCompletion)
        where TExecute : struct, ICommandFunction<TState, ValueTuple, Task>
        where TEnabled : struct, ICommandFunction<TState, ValueTuple, bool>
    {
        return new TaskCommand<TExecute, TEnabled, TState, ValueTuple>(
            state, execute, enabled, optionalParameter: false, awaitCompletion);
    }

    private static Command<ValueTuple, ValueTuple> CreateForTask<TExecute, TEnabled, TState>(
        IObservable<TState> state, TExecute execute, TEnabled enabled, bool awaitCompletion)
        where TExecute : struct, ICommandFunction<TState, ValueTuple, Task>
        where TEnabled : struct, ICommandFunction<TState, ValueTuple, bool>
    {
        return new TaskCommand<TExecute, TEnabled, TState, ValueTuple>(
            state, execute, enabled, optionalParameter: false, awaitCompletion);
    }

    private static class WithResult<TResult>
    {
        public static Command<ValueTuple, TResult> Create<TExecute, TEnabled, TState>(
            TState state, TExecute execute, TEnabled enabled)
            where TExecute : struct, ICommandFunction<TState, ValueTuple, TResult>
            where TEnabled : struct, ICommandFunction<TState, ValueTuple, bool>
        {
            return new Command<TExecute, TEnabled, TState, ValueTuple, TResult>(
                state, execute, enabled, optionalParameter: false);
        }

        public static Command<ValueTuple, TResult> Create<TExecute, TEnabled, TState>(
            IObservable<TState> state, TExecute execute, TEnabled enabled)
            where TExecute : struct, ICommandFunction<TState, ValueTuple, TResult>
            where TEnabled : struct, ICommandFunction<TState, ValueTuple, bool>
        {
            return new Command<TExecute, TEnabled, TState, ValueTuple, TResult>(
                state, execute, enabled, optionalParameter: false);
        }

        public static Command<ValueTuple, TResult> CreateForTask<TExecute, TEnabled, TState>(
            TState state, TExecute execute, TEnabled enabled, bool awaitCompletion)
            where TExecute : struct, ICommandFunction<TState, ValueTuple, Task<TResult>>
            where TEnabled : struct, ICommandFunction<TState, ValueTuple, bool>
        {
            return new TaskCommand<TExecute, TEnabled, TState, ValueTuple, TResult>(
                state, execute, enabled, optionalParameter: false, awaitCompletion);
        }

        public static Command<ValueTuple, TResult> CreateForTask<TExecute, TEnabled, TState>(
            IObservable<TState> state, TExecute execute, TEnabled enabled, bool awaitCompletion)
            where TExecute : struct, ICommandFunction<TState, ValueTuple, Task<TResult>>
            where TEnabled : struct, ICommandFunction<TState, ValueTuple, bool>
        {
            return new TaskCommand<TExecute, TEnabled, TState, ValueTuple, TResult>(
                state, execute, enabled, optionalParameter: false, awaitCompletion);
        }
    }

    public static bool CanExecute<TResult>(this Command<ValueTuple, TResult> command) =>
        command.CanExecute(default);

    public static void Execute<TResult>(this Command<ValueTuple, TResult> command) =>
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
                .FirstOrDefault();

            return
                argument?.Value is byte nullability &&
                nullability == 2;
        });
}

public static class Command<TParameter>
{
    public static Command<TParameter, ValueTuple> Create(
        Action<TParameter> execute) =>
        Create(NoState, Execute(execute), EnabledAlways(), OptionalParameter(execute));

    public static Command<TParameter, ValueTuple> Create(
        Action<TParameter> execute, Func<TParameter, bool> canExecute) =>
        Create(NoState, Execute(execute), Enabled(canExecute), OptionalParameter(execute));

    public static Command<TParameter, ValueTuple> Create<TState>(
        TState state, Action<TState, TParameter> execute) =>
        Create(state, Execute(execute), EnabledAlways<TState>(), OptionalParameter(execute));

    public static Command<TParameter, ValueTuple> Create<TState>(
        TState state, Action<TState, TParameter> execute, Func<TState, TParameter, bool> canExecute) =>
        Create(state, Execute(execute), Enabled(canExecute), OptionalParameter(execute));

    public static Command<TParameter, ValueTuple> Create<TState>(
        IObservable<TState> state, Action<TState, TParameter> execute) =>
        Create(state, Execute(execute), EnabledAlways<TState>(), OptionalParameter(execute));

    public static Command<TParameter, ValueTuple> Create<TState>(
        IObservable<TState> state, Action<TState, TParameter> execute, Func<TState, TParameter, bool> canExecute) =>
        Create(state, Execute(execute), Enabled(canExecute), OptionalParameter(execute));

    public static Command<TParameter, TResult> Create<TResult>(
        Func<TParameter, TResult> execute) =>
        WithResult<TResult>.Create(NoState, Execute(execute), EnabledAlways(), OptionalParameter(execute));

    public static Command<TParameter, TResult> Create<TResult>(
        Func<TParameter, TResult> execute, Func<TParameter, bool> canExecute) =>
        WithResult<TResult>.Create(NoState, Execute(execute), Enabled(canExecute), OptionalParameter(execute));

    public static Command<TParameter, TResult> Create<TState, TResult>(
        TState state, Func<TState, TParameter, TResult> execute) =>
        WithResult<TResult>.Create(state, Execute(execute), EnabledAlways<TState>(), OptionalParameter(execute));

    public static Command<TParameter, TResult> Create<TState, TResult>(
        TState state, Func<TState, TParameter, TResult> execute, Func<TState, TParameter, bool> canExecute) =>
        WithResult<TResult>.Create(state, Execute(execute), Enabled(canExecute), OptionalParameter(execute));

    public static Command<TParameter, TResult> Create<TState, TResult>(
        IObservable<TState> state, Func<TState, TParameter, TResult> execute) =>
        WithResult<TResult>.Create(state, Execute(execute), EnabledAlways<TState>(), OptionalParameter(execute));

    public static Command<TParameter, TResult> Create<TState, TResult>(
        IObservable<TState> state, Func<TState, TParameter, TResult> execute, Func<TState, TParameter, bool> canExecute) =>
        WithResult<TResult>.Create(state, Execute(execute), Enabled(canExecute), OptionalParameter(execute));

    public static Command<TParameter, ValueTuple> CreateForTask(
        Func<TParameter, Task> execute, bool awaitCompletion = true) =>
        CreateForTask(NoState, Execute(execute), EnabledAlways(), OptionalParameter(execute), awaitCompletion);

    public static Command<TParameter, ValueTuple> CreateForTask(
        Func<TParameter, Task> execute, Func<TParameter, bool> canExecute, bool awaitCompletion = true) =>
        CreateForTask(NoState, Execute(execute), Enabled(canExecute), OptionalParameter(execute), awaitCompletion);

    public static Command<TParameter, ValueTuple> CreateForTask<TState>(
        TState state, Func<TState, TParameter, Task> execute, bool awaitCompletion = true) =>
        CreateForTask(state, Execute(execute), EnabledAlways<TState>(), OptionalParameter(execute), awaitCompletion);

    public static Command<TParameter, ValueTuple> CreateForTask<TState>(
        TState state, Func<TState, TParameter, Task> execute, Func<TState, TParameter, bool> canExecute, bool awaitCompletion = true) =>
        CreateForTask(state, Execute(execute), Enabled(canExecute), OptionalParameter(execute), awaitCompletion);

    public static Command<TParameter, ValueTuple> CreateForTask<TState>(
        IObservable<TState> state, Func<TState, TParameter, Task> execute, bool awaitCompletion = true) =>
        CreateForTask(state, Execute(execute), EnabledAlways<TState>(), OptionalParameter(execute), awaitCompletion);

    public static Command<TParameter, ValueTuple> CreateForTask<TState>(
        IObservable<TState> state, Func<TState, TParameter, Task> execute, Func<TState, TParameter, bool> canExecute, bool awaitCompletion = true) =>
        CreateForTask(state, Execute(execute), Enabled(canExecute), OptionalParameter(execute), awaitCompletion);

    public static Command<TParameter, TResult> CreateForTask<TResult>(
        Func<TParameter, Task<TResult>> execute, bool awaitCompletion = true) =>
        WithResult<TResult>.CreateForTask(NoState, Execute(execute), EnabledAlways(), OptionalParameter(execute), awaitCompletion);

    public static Command<TParameter, TResult> CreateForTask<TResult>(
        Func<TParameter, Task<TResult>> execute, Func<TParameter, bool> canExecute, bool awaitCompletion = true) =>
        WithResult<TResult>.CreateForTask(NoState, Execute(execute), Enabled(canExecute), OptionalParameter(execute), awaitCompletion);

    public static Command<TParameter, TResult> CreateForTask<TState, TResult>(
        TState state, Func<TState, TParameter, Task<TResult>> execute, bool awaitCompletion = true) =>
        WithResult<TResult>.CreateForTask(state, Execute(execute), EnabledAlways<TState>(), OptionalParameter(execute), awaitCompletion);

    public static Command<TParameter, TResult> CreateForTask<TState, TResult>(
        TState state, Func<TState, TParameter, Task<TResult>> execute, Func<TState, TParameter, bool> canExecute, bool awaitCompletion = true) =>
        WithResult<TResult>.CreateForTask(state, Execute(execute), Enabled(canExecute), OptionalParameter(execute), awaitCompletion);

    public static Command<TParameter, TResult> CreateForTask<TState, TResult>(
        IObservable<TState> state, Func<TState, TParameter, Task<TResult>> execute, bool awaitCompletion = true) =>
        WithResult<TResult>.CreateForTask(state, Execute(execute), EnabledAlways<TState>(), OptionalParameter(execute), awaitCompletion);

    public static Command<TParameter, TResult> CreateForTask<TState, TResult>(
        IObservable<TState> state, Func<TState, TParameter, Task<TResult>> execute, Func<TState, TParameter, bool> canExecute, bool awaitCompletion = true) =>
        WithResult<TResult>.CreateForTask(state, Execute(execute), Enabled(canExecute), OptionalParameter(execute), awaitCompletion);

    private static ValueTuple NoState => default;

    private static ExecuteAction Execute(Action<TParameter> execute) => new(execute);
    private static ExecuteAction<TState> Execute<TState>(Action<TState, TParameter> execute) => new(execute);

    private static Function<TResult> Execute<TResult>(Func<TParameter, TResult> execute) => new(execute);
    private static Function<TState, TResult> Execute<TState, TResult>(Func<TState, TParameter, TResult> execute) => new(execute);

    private static Function<bool> Enabled(Func<TParameter, bool> enabled) => new(enabled);
    private static Function<TState, bool> Enabled<TState>(Func<TState, TParameter, bool> enabled) => new(enabled);

    private static EnabledAlwaysFunction<ValueTuple> EnabledAlways() => default;
    private static EnabledAlwaysFunction<TState> EnabledAlways<TState>() => default;

    private readonly struct ExecuteAction : ICommandFunction<ValueTuple, TParameter, ValueTuple>
    {
        public readonly Action<TParameter> Execute;
        public ExecuteAction(Action<TParameter> execute) => Execute = execute;

        public ValueTuple Invoke(ValueTuple state, TParameter parameter)
        {
            Execute(parameter);
            return default;
        }
    }

    private readonly struct ExecuteAction<TState> : ICommandFunction<TState, TParameter, ValueTuple>
    {
        public readonly Action<TState, TParameter> Execute;
        public ExecuteAction(Action<TState, TParameter> execute) => Execute = execute;

        public ValueTuple Invoke(TState state, TParameter parameter)
        {
            Execute(state, parameter);
            return default;
        }
    }

    private readonly struct Function<TResult> : ICommandFunction<ValueTuple, TParameter, TResult>
    {
        public readonly Func<TParameter, TResult> Method;
        public Function(Func<TParameter, TResult> method) => Method = method;
        public TResult Invoke(ValueTuple state, TParameter parameter) => Method(parameter);
    }

    private readonly struct Function<TState, TResult> : ICommandFunction<TState, TParameter, TResult>
    {
        public readonly Func<TState, TParameter, TResult> Method;
        public Function(Func<TState, TParameter, TResult> method) => Method = method;
        public TResult Invoke(TState state, TParameter parameter) => Method(state, parameter);
    }

    private readonly struct EnabledAlwaysFunction<TState> : ICommandFunction<TState, TParameter, bool>
    {
        public bool Invoke(TState state, TParameter parameter) => true;
    }

    private static Command<TParameter, ValueTuple> Create<TExecute, TEnabled, TState>(
        TState state, TExecute execute, TEnabled enabled, bool optionalParameter)
        where TExecute : struct, ICommandFunction<TState, TParameter, ValueTuple>
        where TEnabled : struct, ICommandFunction<TState, TParameter, bool>
    {
        return new Command<TExecute, TEnabled, TState, TParameter, ValueTuple>(
            state, execute, enabled, optionalParameter);
    }

    private static Command<TParameter, ValueTuple> Create<TExecute, TEnabled, TState>(
        IObservable<TState> state, TExecute execute, TEnabled enabled, bool optionalParameter)
        where TExecute : struct, ICommandFunction<TState, TParameter, ValueTuple>
        where TEnabled : struct, ICommandFunction<TState, TParameter, bool>
    {
        return new Command<TExecute, TEnabled, TState, TParameter, ValueTuple>(
            state, execute, enabled, optionalParameter);
    }

    private static Command<TParameter, ValueTuple> CreateForTask<TExecute, TEnabled, TState>(
        TState state, TExecute execute, TEnabled enabled, bool optionalParameter, bool awaitCompletion)
        where TExecute : struct, ICommandFunction<TState, TParameter, Task>
        where TEnabled : struct, ICommandFunction<TState, TParameter, bool>
    {
        return new TaskCommand<TExecute, TEnabled, TState, TParameter>(
            state, execute, enabled, optionalParameter, awaitCompletion);
    }

    private static Command<TParameter, ValueTuple> CreateForTask<TExecute, TEnabled, TState>(
        IObservable<TState> state, TExecute execute, TEnabled enabled, bool optionalParameter, bool awaitCompletion)
        where TExecute : struct, ICommandFunction<TState, TParameter, Task>
        where TEnabled : struct, ICommandFunction<TState, TParameter, bool>
    {
        return new TaskCommand<TExecute, TEnabled, TState, TParameter>(
            state, execute, enabled, optionalParameter, awaitCompletion);
    }

    private static class WithResult<TResult>
    {
        public static Command<TParameter, TResult> Create<TExecute, TEnabled, TState>(
            TState state, TExecute execute, TEnabled enabled, bool optionalParameter)
            where TExecute : struct, ICommandFunction<TState, TParameter, TResult>
            where TEnabled : struct, ICommandFunction<TState, TParameter, bool>
        {
            return new Command<TExecute, TEnabled, TState, TParameter, TResult>(
                state, execute, enabled, optionalParameter);
        }

        public static Command<TParameter, TResult> Create<TExecute, TEnabled, TState>(
            IObservable<TState> state, TExecute execute, TEnabled enabled, bool optionalParameter)
            where TExecute : struct, ICommandFunction<TState, TParameter, TResult>
            where TEnabled : struct, ICommandFunction<TState, TParameter, bool>
        {
            return new Command<TExecute, TEnabled, TState, TParameter, TResult>(
                state, execute, enabled, optionalParameter);
        }

        public static Command<TParameter, TResult> CreateForTask<TExecute, TEnabled, TState>(
            TState state, TExecute execute, TEnabled enabled, bool optionalParameter, bool awaitCompletion)
            where TExecute : struct, ICommandFunction<TState, TParameter, Task<TResult>>
            where TEnabled : struct, ICommandFunction<TState, TParameter, bool>
        {
            return new TaskCommand<TExecute, TEnabled, TState, TParameter, TResult>(
                state, execute, enabled, optionalParameter, awaitCompletion);
        }

        public static Command<TParameter, TResult> CreateForTask<TExecute, TEnabled, TState>(
            IObservable<TState> state, TExecute execute, TEnabled enabled, bool optionalParameter, bool awaitCompletion)
            where TExecute : struct, ICommandFunction<TState, TParameter, Task<TResult>>
            where TEnabled : struct, ICommandFunction<TState, TParameter, bool>
        {
            return new TaskCommand<TExecute, TEnabled, TState, TParameter, TResult>(
                state, execute, enabled, optionalParameter, awaitCompletion);
        }
    }

    internal static bool OptionalParameter(Delegate method) =>
        typeof(TParameter).IsValueType
        ? default(TParameter) is null
        : Command.OptionalParameter(method);

    internal static bool UnboxParameter(object? boxed, [NotNullWhen(true)] out TParameter? unboxed, bool allowNull)
    {
        if (typeof(TParameter) == typeof(ValueTuple))
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