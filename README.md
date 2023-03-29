## What is Kinetic?

Kinetic is an alternative implementation of the Reactive framework focused on performance and lesser memory allocations.

## Features

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

### Collections

Instead of the standard `ObservableCollection<T>` and other custom types implementing `INotifyCollectionChanged` Kinetic has two built-in collections, `ObservableList<T>` and `ReadOnlyObservableList<T>` which use the same notification model as the rest of the framework. Both of them expose an `IObservable<ListChange<T>>` through  the `Changed` property.

In contradiction to .NET and DynamicData, there are no multiple element changes to avoid memory allocations on notifications and code complexity for views. Therefore, each `ListChange<T>` can represent one of the following actions:

* `RemoveAll`
* `Remove`
* `Insert`
* `Replace`
* `Move`

Other than that Kinetic collections looks pretty similar to what the .NET ecosystem has.

For possible scenarios of making views using LINQ operators please refer to tests. Currently only `Where` and `OrderBy` are supported, but more options and operators should come to Kinetic.

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

## LINQ

The project provides a subset of LINQ extensions, which are contained by `Kinetic.Linq` package. The key idea of it is to build a single state machine for a chain of extension method call to minimize memory occupied by the resulting observer, and to avoid many interface calls which happen in Reactive.

## Integration with UI

Since all observable properties should be defined as `Property<T>` or `ReadOnlyProperty<T>`, there's a limitation of Kinetic usage. It's supported only by Avalonia at the moment thanfully to the extensible binding system, but a general solution to support any XAML framework will come later.

To make Avalonia recognize Kinetic properties, the `Kinetic.Avalonia` package should be added and one line of code at startup as well:

Collection bindings automaticlly produce a proxy which translates `ListChange<T>` to `INotifyCollectionChanged` events.

```csharp
using Avalonia.Data.Core;
using Kinetic.Data;

// Adds the accessor for Kinetic properties before the CLR property accessor  
ExpressionObserver.PropertyAccessors.Insert(2, new KineticPropertyAccessor());
```

> This approach is incompatible with compiled bindings since XAMLIL has no idea about Kinetic properties and treats them as usual properties. Ability to create compiled bindings in XAML will come in one of next releases, but it's already available in code behind using `OneWay` and `TwoWay` methods of `Binding` type in `Kinetic.Data`.