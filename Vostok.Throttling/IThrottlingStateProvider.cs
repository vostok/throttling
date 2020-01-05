using JetBrains.Annotations;

namespace Vostok.Throttling
{
    internal interface IThrottlingStateProvider
    {
        [NotNull]
        ThrottlingState ObtainState();
    }
}
