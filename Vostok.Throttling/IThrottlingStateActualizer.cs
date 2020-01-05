using JetBrains.Annotations;

namespace Vostok.Throttling
{
    internal interface IThrottlingStateActualizer
    {
        void Actualize([NotNull] ThrottlingState state);
    }
}
