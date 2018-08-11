namespace Vostok.Throttling
{
    public class KnownProperties
    {
        public const string ConsumerIdKey = "ConsumerId";

        public static Property ConsumerId(string value)
        {
            return new Property(ConsumerIdKey, value);
        }
    }
}