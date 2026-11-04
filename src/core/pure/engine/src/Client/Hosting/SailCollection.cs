using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using saiflow.engine.Client;

namespace saiflow.engine.Hosting;

public sealed class SailCollection : List<SailFeatureDescriptor>, ISailCollection
{
    public IDictionary<object, object?> Properties { get; } =
        new Dictionary<object, object?>();

    public IServiceCollection? ServiceCollection { get; set; }

    public ISailCollection AddFeature(SailFeatureDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (string.IsNullOrWhiteSpace(descriptor.Name))
            throw new ArgumentException("SailFeatureDescriptor.Name is required.", nameof(descriptor));

        for (var i = 0; i < Count; i++)
        {
            if (string.Equals(this[i].Name, descriptor.Name, StringComparison.Ordinal)
                && this[i].Phase == descriptor.Phase)
            {
                this[i] = descriptor;
                return this;
            }
        }

        base.Add(descriptor);
        return this;
    }
}
