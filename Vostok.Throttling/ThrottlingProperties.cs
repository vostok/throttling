using JetBrains.Annotations;

namespace Vostok.Throttling
{
    public struct ThrottlingProperties
    {
        public ThrottlingProperties(params Property[] properties)
        {
            Properties = properties;
        }

        [CanBeNull]
        public readonly Property[] Properties;

        [CanBeNull]
        public Property? this[string key]
        {
            get
            {
                if (Properties == null)
                    return null;

                for (int i = 0; i < Properties.Length; i++)
                {
                    var property = Properties[i];
                    if (property.Key == key)
                        return property;
                }

                return null;
            }
        }
    }
}