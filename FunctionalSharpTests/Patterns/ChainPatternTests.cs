using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace FunctionalSharp.Patterns.Tests
{
    [TestClass()]
    public class ChainPatternTests
    {
        private class PayloadType
        {
            public int Sum { get; internal set; }
        }

        private abstract class AbstractType { }

        [TestMethod()]
        public void When_Chain_IsCreated_ExpectExecution()
        {
            var chain = GenericChain<PayloadType>.Create();

            chain
                .AddLink(data => data.Payload.Sum = 0)
                .AddLink(data => data.Payload.Sum += 10)
                .AddLink(data => data.Payload.Sum += 20)
                .OnCompleted(data => Assert.AreEqual(30, data.Sum))
                .Run();
        }

        [TestMethod()]
        public void When_Chain_IsCreatedWithPayload_ExpectRetainInstance()
        {
            var myLocalPayload = new PayloadType
            {
                Sum = 20
            };

            var chain = GenericChain<PayloadType>.Create(myLocalPayload);

            chain
                .AddLink(data => data.Payload.Sum += 10)
                .AddLink(data => data.Payload.Sum += 10)
                .AddLink(data => data.Payload.Sum += 20)
                .OnCompleted(data => Assert.AreEqual(60, data.Sum))
                .Run();

            Assert.AreEqual(60, myLocalPayload.Sum);
        }

        [TestMethod()]
        public void When_Chain_IsCreatedWithStringPayload_ExpectRetainValue()
        {
            var chain = GenericChain<string>.Create("my payload");

            chain
                .AddLink(data => data.Payload += " with more data")
                .AddLink(data => data.Payload += " that should remain")
                .AddLink(data => data.Payload += " across calls")
                .OnCompleted(data => Assert.AreEqual("my payload with more data that should remain across calls", data))
                .Run();
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentException))]
        public void When_Chain_IsCreatedWithAbstract_ExpectException()
        {
            GenericChain<AbstractType>.Create();
        }

        [TestMethod()]
        public void When_Chain_FailAndRepeat_ExpectResults()
        {
            var chain = GenericChain<int>.Create(0, 
                new GenericChain<int>
                    .Configuration(
                        stopOnFailure: false, 
                        repeatTimesOnFailure: 3
                    )
                );

            chain
                .AddLink(data => data.Payload = 10)
                .AddLink(data => {
                    data.Payload += 1;
                    //force exception
                    throw new Exception();
                })
                .OnCompleted(data => Assert.AreEqual(13, data))
                .Run();
        }

        [TestMethod()]
        public void When_Chain_IsCanceled_ExpectResults()
        {
            var chain = GenericChain<int>.Create(0);

            chain
                .AddLink(data => data.Payload = 10)
                .AddLink(data => data.Payload += 1)
                .AddLink(data => data.Cancel = true)
                .AddLink(data => data.Payload += 1)
                .OnCompleted(data => Assert.AreEqual(11, data))
                .Run();
        }
    }

}