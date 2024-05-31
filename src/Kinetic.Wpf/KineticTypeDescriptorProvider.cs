using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Kinetic.Data;

public sealed class KineticTypeDescriptorProvider : TypeDescriptionProvider
{
    public KineticTypeDescriptorProvider() :
        base(TypeDescriptor.GetProvider(typeof(ObservableObject)))
    { }

    public override ICustomTypeDescriptor? GetTypeDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, object? instance) =>
        // SAFETY: The base implementation always returns a type descriptor.
        new KineticTypeDescriptor(base.GetTypeDescriptor(objectType, instance)!);
}