## What is Kinetic?

Kinetic is an alternative implementation of the Reactive framework focused on performance and lesser memory allocations.

## Features

> The project is in progress and doesn't provide replacement for `System.Reactive.Linq` yet, but it's planned in future releases. For please use Reactive for that part as it's demonstrated in tests.

### Objects

To achive the goal Kinetic doesn't support the `INotifyPropertyChanged` interface and fully relies on `IObservable<T>`. To make it work an observable property should via `Property<T>` or `ReadOnlyProperty<T>` structures which bundle a getter, a setter and an observable. Calling the `Set` method on a property sets the corresponind field and notifyies observers about the change. The `Changed` property returns an observable for the property which is a cached and reused, and no LINQ expressions allocated as it happens when `WhenAnyValue` is used from Reactive.

```csharp
private sealed class Some : Object
{
    private int _number;
    private string _text = string.Empty;

    public Some() => Number.Changed.Subscribe(value => Set(Text, value.ToString()));

    public Property<int> Number => Property(ref _number);
    public ReadOnlyProperty<string> Text => Property(ref _text);

    public static void Usage()
    {
        var some = new Some();
        var numberExplicit = some.Number.Get();
        int numberImplicit = some.Number;

        some.Number.Set(42);
    }
}
```

### Commands

Kinetic provides an implementation of the `ICommand` interface which produces a result on completion by implementing `IObservable<T>` too. Any command can have a state object passed to it on creation or received from the provided observable object.

Kinetic commands in contradistinction to Reactive support parameter validation on `CanExecute` and `Execute`, and even does nullable reference type validation.

```csharp
// Nullable reference parameter
var command = Command<string?>.Create(p => p);

command.CanExecute(null); // returns true
command.CanExecute("text"); // returns true

// Non-nullable reference parameter
var command = Command<string>.Create(p => p);

command.CanExecute(null); // returns false
command.CanExecute("text"); // returns true
```

## Integration with UI

Since all observable properties should be defined as `Property<T>` or `ReadOnlyProperty<T>`, there's a limitation on usage of Kinetic. It's supported only by Avalonia at the moment thanfully to the extensible binding system, but a general solution to support any XAML framework will come later.

To make Avalonia recognize Kinetic properties, the `Kinetic.Avalonia` package should be added and one line of code at startup as well:

```
using Avalonia.Data.Core;
using Kinetic.Avalonia;

ExpressionObserver.PropertyAccessors.Insert(2, new PropertyAccessor());
```