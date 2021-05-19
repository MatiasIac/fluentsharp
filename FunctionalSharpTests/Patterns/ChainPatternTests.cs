using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunctionalSharp.Patterns.Tests
{
    [TestClass()]
    public class ChainPatternTests
    {
        private class PayloadType
        {
            public int Sum { get; internal set; }
        }

        [TestMethod()]
        public void When_Chain_IsCreated_ExpectExecution()
        {
            var chain = Chain<PayloadType>.Create();

            chain
                .AddLink(data => data.Payload.Sum = 0)
                .AddLink(data => data.Payload.Sum += 10)
                .AddLink(data => data.Payload.Sum += 20)
                .OnComplete(data => Assert.AreEqual(30, data.Payload.Sum))
                .Run();
        }

        [TestMethod()]
        public void When_Chain_IsCreatedWithPayload_ExpectRetainInstance()
        {
            var myLocalPayload = new PayloadType
            {
                Sum = 20
            };

            var chain = Chain<PayloadType>.Create(myLocalPayload);

            chain
                .AddLink(data => data.Payload.Sum += 10)
                .AddLink(data => data.Payload.Sum += 10)
                .AddLink(data => data.Payload.Sum += 20)
                .OnComplete(data => Assert.AreEqual(60, data.Payload.Sum))
                .Run();

            Assert.AreEqual(60, myLocalPayload.Sum);
        }
    }

}