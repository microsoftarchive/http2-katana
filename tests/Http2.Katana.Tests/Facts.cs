using Xunit;

namespace Http2.Katana.Tests
{
    class StandardFact : FactAttribute
    {
        public StandardFact()
        {
            Timeout = 15000;
        }
    }
    class LongTaskFact : FactAttribute
    {
        public LongTaskFact()
        {
            Timeout = 90000;
        }
    }
    class VeryLongTaskFact : FactAttribute
    {
        public VeryLongTaskFact()
        {
            Timeout = 180000;
        }
    }
}
