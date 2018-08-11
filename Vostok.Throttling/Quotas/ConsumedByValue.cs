namespace Vostok.Throttling.Quotas
{
    public struct ConsumedByValue
    {
        public readonly string Value;
        public readonly int Consumed;

        public ConsumedByValue(string value, int consumed)
        {
            Value = value;
            Consumed = consumed;
        }
    }
}