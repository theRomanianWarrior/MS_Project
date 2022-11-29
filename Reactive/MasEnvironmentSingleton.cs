using System;
using ActressMas;

namespace Reactive
{
    public sealed class MasEnvironmentSingleton
    {
        private static readonly Lazy<EnvironmentMas> Lazy = new(() => new EnvironmentMas(0, 100));

        private MasEnvironmentSingleton()
        {
        }

        public static EnvironmentMas Instance => Lazy.Value;
    }
}