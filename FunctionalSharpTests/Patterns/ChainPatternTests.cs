using FunctionalSharp.Decorators;
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

        public class MyOwnAction : LinkBase<int>
        {
            public override void OnExecute(DataCargo<int> data)
            {
                data.Payload += 1;
            }
        }

        [TestMethod()]
        public void When_Chain_IsCreated_ExpectExecution()
        {
            var chain = GenericChain<PayloadType>.Create();
            PayloadType completedPayload = null;
            var completedCalls = 0;

            chain
                .AddLink(data => data.Payload.Sum = 0)
                .AddLink(data => data.Payload.Sum += 10)
                .AddLink(data => data.Payload.Sum += 20)
                .OnCompleted(data => { completedPayload = data; completedCalls++; })
                .Run();

            Assert.AreEqual(1, completedCalls);
            Assert.IsNotNull(completedPayload);
            Assert.AreEqual(30, completedPayload.Sum);
        }

        [TestMethod()]
        public void When_Chain_IsCreatedWithPayload_ExpectRetainInstance()
        {
            var myLocalPayload = new PayloadType
            {
                Sum = 20
            };

            var chain = GenericChain<PayloadType>.Create(myLocalPayload);
            PayloadType completedPayload = null;
            var completedCalls = 0;

            chain
                .AddLink(data => data.Payload.Sum += 10)
                .AddLink(data => data.Payload.Sum += 10)
                .AddLink(data => data.Payload.Sum += 20)
                .OnCompleted(data => { completedPayload = data; completedCalls++; })
                .Run();

            Assert.AreEqual(1, completedCalls);
            Assert.AreSame(myLocalPayload, completedPayload);
            Assert.AreEqual(60, myLocalPayload.Sum);
        }

        [TestMethod()]
        public void When_Chain_IsCreatedWithStringPayload_ExpectRetainValue()
        {
            var chain = GenericChain<string>.Create("my payload");
            string completedPayload = null;
            var completedCalls = 0;

            chain
                .AddLink(data => data.Payload += " with more data")
                .AddLink(data => data.Payload += " that should remain")
                .AddLink(data => data.Payload += " across calls")
                .OnCompleted(data => { completedPayload = data; completedCalls++; })
                .Run();

            Assert.AreEqual(1, completedCalls);
            Assert.AreEqual("my payload with more data that should remain across calls", completedPayload);
        }

        [TestMethod()]
        public void When_Chain_IsCreatedWithAbstract_ExpectException()
        {
            Assert.Throws<ArgumentException>(() => GenericChain<AbstractType>.Create());
        }

        [TestMethod()]
        public void When_Chain_FailAndRepeat_ExpectResults()
        {
            var chain = GenericChain<int>.Create(0, 
                new Configuration(
                        stopOnFailure: false, 
                        maxAttempts: 3
                    )
                );
            var completedPayload = 0;
            var completedCalls = 0;
            var errors = 0;

            chain
                .AddLink(data => data.Payload = 10)
                .AddLink(data => {
                    data.Payload += 1;
                    //force exception
                    throw new Exception();
                })
                .OnError((data, error) => errors++)
                .OnCompleted(data => { completedPayload = data; completedCalls++; })
                .Run();

            Assert.AreEqual(3, errors);
            Assert.AreEqual(1, completedCalls);
            Assert.AreEqual(13, completedPayload);
        }

        [TestMethod()]
        public void When_Chain_IsCanceled_ExpectResults()
        {
            var chain = GenericChain<int>.Create(0);
            DataCargo<int> cancelledCargo = null;
            var laterCalls = 0;
            var completedCalls = 0;

            chain
                .AddLink(data => data.Payload = 10)
                .AddLink(data => data.Payload += 1)
                .AddLink(data => { cancelledCargo = data; data.Cancel = true; })
                .AddLink(data => { laterCalls++; data.Payload += 1; })
                .OnCompleted(data => completedCalls++)
                .Run();

            Assert.IsNotNull(cancelledCargo);
            Assert.AreEqual(11, cancelledCargo.Payload);
            Assert.IsTrue(cancelledCargo.Cancel);
            Assert.AreEqual(0, laterCalls);
            Assert.AreEqual(0, completedCalls);
        }

        [TestMethod()]
        public void When_Chain_UsesCustomLinkType_ExpectResults()
        {
            var chain = GenericChain<int>.Create(0);
            var completedPayload = 0;
            var completedCalls = 0;

            chain
                .AddLink(new MyOwnAction())
                .AddLink(new MyOwnAction())
                .AddLink(new MyOwnAction())
                .OnCompleted(data => { completedPayload = data; completedCalls++; })
                .Run();

            Assert.AreEqual(1, completedCalls);
            Assert.AreEqual(3, completedPayload);
        }

        [TestMethod()]
        public void When_Chain_Fail_ExpectError()
        {
            var chain = GenericChain<int>.Create(0, new Configuration(stopOnFailure: true));
            var failure = new Exception("Chain exception");
            var errorPayload = 0;
            var errorCalls = 0;
            Exception reportedError = null;

            var thrown = Assert.ThrowsExactly<Exception>(() =>
                chain
                    .AddLink(data => data.Payload = 10)
                    .AddLink(data => {
                        data.Payload += 1;
                        throw failure;
                    })
                    .OnError((data, ex) => 
                    {
                        errorPayload = data;
                        reportedError = ex;
                        errorCalls++;
                        throw ex;
                    })
                    .Run()
            );

            Assert.AreEqual(1, errorCalls);
            Assert.AreEqual(11, errorPayload);
            Assert.AreSame(failure, reportedError);
            Assert.AreSame(failure, thrown);
        }

        [TestMethod()]
        public void When_Chain_FailAndStopOnFailureIsSet_OnCompleted_IsNotExecuted()
        {
            var chain = GenericChain<int>.Create(0, new Configuration(stopOnFailure: true));
            var completedCalls = 0;
            var laterCalls = 0;
            var errorCalls = 0;

            chain
                .AddLink(data => data.Payload = 10)
                .AddLink(data => throw new Exception())
                .AddLink(data => laterCalls++)
                .OnError((data, error) => errorCalls++)
                .OnCompleted(data => completedCalls++)
                .Run();

            Assert.AreEqual(1, errorCalls);
            Assert.AreEqual(0, laterCalls);
            Assert.AreEqual(0, completedCalls);
        }

        [Link("MyCustomLink")]
        public class CustomLink : LinkBase<int>
        {
            public override void OnExecute(DataCargo<int> data)
            {
                data.Payload++;
            }
        }

        [Link("MyStringCustomLink")]
        public class StringCustomLink : LinkBase<string>
        {
            public override void OnExecute(DataCargo<string> data)
            {
                data.Payload += "injected value";
            }
        }

        [TestMethod()]
        public void When_Chain_AddLinkByAttribute_ExpectResults()
        {
            var chain = GenericChain<int>.Create(0, new Configuration(stopOnFailure: true));
            var completedPayload = 0;
            var completedCalls = 0;

            chain
                .AddDecoratedLink("MyCustomLink")
                .OnCompleted(data => { completedPayload = data; completedCalls++; })
                .Run();

            Assert.AreEqual(1, completedCalls);
            Assert.AreEqual(1, completedPayload);
        }
    }

}
