using System;
using ActressMas;

namespace Reactive;

public sealed class MasEnvSingleton
{
    private static readonly Lazy<EnvironmentMas> _lazy = new(() => new EnvironmentMas(0, 50));

    private MasEnvSingleton()
    {
    }

    public static EnvironmentMas Instance
    {
        get => _lazy.Value;
        set { }
    }
}