using System;
using System.ComponentModel;
using System.Linq;

namespace Kinetic.Data;

internal sealed class KineticTypeDescriptor : ICustomTypeDescriptor
{
    private readonly ICustomTypeDescriptor _parent;

    public KineticTypeDescriptor(ICustomTypeDescriptor parent) =>
        _parent = parent;

    public AttributeCollection GetAttributes() =>
        _parent.GetAttributes();

    public string? GetClassName() =>
        _parent.GetClassName();

    public string? GetComponentName() =>
        _parent.GetComponentName();

    public TypeConverter? GetConverter() =>
        _parent.GetConverter();

    public EventDescriptor? GetDefaultEvent() =>
        _parent.GetDefaultEvent();

    public PropertyDescriptor? GetDefaultProperty() =>
        _parent.GetDefaultProperty();

    public object? GetEditor(Type editorBaseType) =>
        _parent.GetEditor(editorBaseType);

    public EventDescriptorCollection GetEvents() =>
        _parent.GetEvents();

    public EventDescriptorCollection GetEvents(Attribute[]? attributes) =>
        _parent.GetEvents(attributes);

    public PropertyDescriptorCollection GetProperties() =>
        GetProperties(null);

    public PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
    {
        var properties = _parent
            .GetProperties(null)
            .Cast<PropertyDescriptor>()
            .Select(property => KineticPropertyDescriptor.TryCreate(property) ?? property)
            .ToArray();

        return new PropertyDescriptorCollection(properties);
    }

    public object? GetPropertyOwner(PropertyDescriptor? pd) =>
        _parent.GetPropertyOwner(pd);
}