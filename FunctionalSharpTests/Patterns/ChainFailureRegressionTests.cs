using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;

namespace FunctionalSharp.Patterns.Tests
{
    [TestClass]
    public class ChainFailureRegressionTests
    {
        [TestMethod]
        public void DefaultConfiguration_StopsAfterOneAttempt()
        {
            var configuration = new Configuration();

            Assert.AreEqual(1, configuration.MaxAttempts);
            Assert.IsTrue(configuration.StopOnFailure);
        }

        [TestMethod]
        [DataRow(true, 0)]
        [DataRow(false, 0)]
        [DataRow(true, -1)]
        [DataRow(false, -1)]
        public void Configuration_RejectsNonPositiveAttemptLimits(bool stopOnFailure, int maxAttempts)
        {
            var error = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                new Configuration(stopOnFailure, maxAttempts));

            Assert.AreEqual("maxAttempts", error.ParamName);
            Assert.AreEqual(maxAttempts, error.ActualValue);
        }

        [TestMethod]
        [DataRow(true, 1)]
        [DataRow(false, 1)]
        [DataRow(true, 3)]
        [DataRow(false, 3)]
        public void ExhaustedFailure_RespectsAttemptLimitAndStopPolicy(bool stopOnFailure, int maxAttempts)
        {
            var attempts = 0;
            var laterCalls = 0;
            var completedCalls = 0;
            var completedPayload = -1;
            var failure = new InvalidOperationException("Link failure");
            var errors = new List<Exception>();
            var errorPayloads = new List<int>();

            GenericChain<int>.Create(0, new Configuration(stopOnFailure, maxAttempts))
                .AddLink(data => { attempts++; data.Payload++; throw failure; })
                .AddLink(data => { laterCalls++; data.Payload += 10; })
                .OnError((payload, error) => { errorPayloads.Add(payload); errors.Add(error); })
                .OnCompleted(payload => { completedCalls++; completedPayload = payload; })
                .Run();

            Assert.AreEqual(maxAttempts, attempts);
            Assert.AreEqual(maxAttempts, errors.Count);
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                Assert.AreSame(failure, errors[attempt]);
                Assert.AreEqual(attempt + 1, errorPayloads[attempt]);
            }
            Assert.AreEqual(stopOnFailure ? 0 : 1, laterCalls);
            Assert.AreEqual(stopOnFailure ? 0 : 1, completedCalls);
            Assert.AreEqual(stopOnFailure ? -1 : maxAttempts + 10, completedPayload);
        }

        [TestMethod]
        [DataRow(true, 1)]
        [DataRow(false, 1)]
        [DataRow(true, 2)]
        [DataRow(false, 2)]
        [DataRow(true, 3)]
        [DataRow(false, 3)]
        public void Success_EndsRetries_AndContinuesInOrder(bool stopOnFailure, int successfulAttempt)
        {
            var attempts = 0;
            var completedCalls = 0;
            var events = new List<string>();
            var expected = new List<string>();
            for (var attempt = 1; attempt <= successfulAttempt; attempt++)
            {
                expected.Add($"attempt:{attempt}");
                if (attempt < successfulAttempt) expected.Add($"error:{attempt}");
            }
            expected.Add("next");
            expected.Add("complete");

            GenericChain<int>.Create(0, new Configuration(stopOnFailure, maxAttempts: 3))
                .AddLink(data =>
                {
                    attempts++;
                    events.Add($"attempt:{attempts}");
                    if (attempts < successfulAttempt) throw new InvalidOperationException();
                })
                .AddLink(data => events.Add("next"))
                .OnError((payload, error) => events.Add($"error:{attempts}"))
                .OnCompleted(payload => { completedCalls++; events.Add("complete"); })
                .Run();

            Assert.AreEqual(successfulAttempt, attempts);
            Assert.AreEqual(1, completedCalls);
            CollectionAssert.AreEqual(expected, events);
        }

        [TestMethod]
        public void Failure_ContinuesWithoutRetries_WhenStopOnFailureIsFalse()
        {
            var attempts = 0;
            var completedCalls = 0;
            var completedPayload = 0;
            var failure = new InvalidOperationException("Link failure");
            var errors = new List<Exception>();

            GenericChain<int>.Create(0, new Configuration(stopOnFailure: false))
                .AddLink(data => { attempts++; data.Payload++; throw failure; })
                .AddLink(data => data.Payload += 10)
                .OnError((payload, error) => errors.Add(error))
                .OnCompleted(payload => { completedCalls++; completedPayload = payload; })
                .Run();

            Assert.AreEqual(1, attempts);
            CollectionAssert.AreEqual(new[] { failure }, errors);
            Assert.AreEqual(1, completedCalls);
            Assert.AreEqual(11, completedPayload);
        }

        [TestMethod]
        public void CancellationRequestedByFailingLink_StopsWithoutRetryOrCompletion()
        {
            var attempts = 0;
            var errors = 0;
            var laterCalls = 0;
            var completedCalls = 0;
            DataCargo<int> cargo = null;

            GenericChain<int>.Create(0, new Configuration(false, 3))
                .AddLink(data =>
                {
                    cargo = data;
                    attempts++;
                    data.Cancel = true;
                    throw new InvalidOperationException();
                })
                .AddLink(data => laterCalls++)
                .OnError((payload, error) => errors++)
                .OnCompleted(payload => completedCalls++)
                .Run();

            Assert.AreEqual(1, attempts);
            Assert.AreEqual(1, errors);
            Assert.IsNotNull(cargo);
            Assert.IsTrue(cargo.Cancel);
            Assert.AreEqual(0, laterCalls);
            Assert.AreEqual(0, completedCalls);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void OperationCanceledException_PropagatesWithoutRetriesOrErrorCallbacks(bool stopOnFailure)
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var failure = new OperationCanceledException(cancellation.Token);
            var attempts = 0;
            var errors = 0;
            var laterCalls = 0;
            var completedCalls = 0;
            var chain = GenericChain<int>.Create(0, new Configuration(stopOnFailure, 3))
                .AddLink(data => { attempts++; throw failure; })
                .AddLink(data => laterCalls++)
                .OnError((payload, error) => errors++)
                .OnCompleted(payload => completedCalls++);

            var thrown = Assert.ThrowsExactly<OperationCanceledException>(() => chain.Run());

            Assert.AreSame(failure, thrown);
            Assert.AreEqual(cancellation.Token, thrown.CancellationToken);
            Assert.AreEqual(1, attempts);
            Assert.AreEqual(0, errors);
            Assert.AreEqual(0, laterCalls);
            Assert.AreEqual(0, completedCalls);
        }

        [TestMethod]
        public void ErrorCallbackFailure_OnLaterAttempt_PropagatesWithoutReenteringHandler()
        {
            var attempts = 0;
            var errors = 0;
            var laterCalls = 0;
            var completedCalls = 0;
            var handlerFailure = new ArgumentException("Error callback failed");
            var chain = GenericChain<int>.Create(0, new Configuration(false, 3))
                .AddLink(data => { attempts++; throw new InvalidOperationException(); })
                .AddLink(data => laterCalls++)
                .OnError((payload, error) =>
                {
                    errors++;
                    if (errors >= 2) throw handlerFailure;
                })
                .OnCompleted(payload => completedCalls++);

            var thrown = Assert.ThrowsExactly<ArgumentException>(() => chain.Run());

            Assert.AreSame(handlerFailure, thrown);
            Assert.AreEqual(2, attempts);
            Assert.AreEqual(2, errors);
            Assert.AreEqual(0, laterCalls);
            Assert.AreEqual(0, completedCalls);
        }

        [TestMethod]
        public void CompletionCallbackFailure_PropagatesWithoutRetryOrErrorCallback()
        {
            var attempts = 0;
            var errors = 0;
            var completedCalls = 0;
            var handlerFailure = new ArgumentException("Completion callback failed");
            var chain = GenericChain<int>.Create(0, new Configuration(false, 3))
                .AddLink(data => attempts++)
                .OnError((payload, error) => errors++)
                .OnCompleted(payload => { completedCalls++; throw handlerFailure; });

            var thrown = Assert.ThrowsExactly<ArgumentException>(() => chain.Run());

            Assert.AreSame(handlerFailure, thrown);
            Assert.AreEqual(1, attempts);
            Assert.AreEqual(0, errors);
            Assert.AreEqual(1, completedCalls);
        }

        [TestMethod]
        public void EachLinkAndRun_ReceivesItsOwnAttemptBudget()
        {
            var firstAttempts = 0;
            var secondAttempts = 0;
            var completedCalls = 0;
            var errors = 0;
            var completedPayloads = new List<int>();
            var chain = GenericChain<int>.Create(0, new Configuration(false, maxAttempts: 2))
                .AddLink(data => { firstAttempts++; data.Payload++; throw new InvalidOperationException(); })
                .AddLink(data => { secondAttempts++; data.Payload += 10; throw new InvalidOperationException(); })
                .OnError((payload, error) => errors++)
                .OnCompleted(payload => { completedCalls++; completedPayloads.Add(payload); });

            chain.Run();
            chain.Run();

            Assert.AreEqual(4, firstAttempts);
            Assert.AreEqual(4, secondAttempts);
            Assert.AreEqual(8, errors);
            Assert.AreEqual(2, completedCalls);
            CollectionAssert.AreEqual(new[] { 22, 44 }, completedPayloads);
        }

        [TestMethod]
        public void RunAfterCancellation_ResetsFlag_AndRetainsPayload()
        {
            var linkCalls = 0;
            var laterCalls = 0;
            var completedCalls = 0;
            var completedPayload = 0;
            var cancelledAtEntry = new List<bool>();
            var chain = GenericChain<int>.Create(0)
                .AddLink(data =>
                {
                    cancelledAtEntry.Add(data.Cancel);
                    linkCalls++;
                    data.Payload++;
                    if (linkCalls == 1) data.Cancel = true;
                })
                .AddLink(data => { laterCalls++; data.Payload += 10; })
                .OnCompleted(payload => { completedCalls++; completedPayload = payload; });

            chain.Run();
            Assert.AreEqual(0, laterCalls);
            Assert.AreEqual(0, completedCalls);

            chain.Run();
            Assert.AreEqual(2, linkCalls);
            Assert.AreEqual(1, laterCalls);
            Assert.AreEqual(1, completedCalls);
            Assert.AreEqual(12, completedPayload);
            CollectionAssert.AreEqual(new[] { false, false }, cancelledAtEntry);
        }

        [TestMethod]
        public void EmptyChain_CompletesOnce_WithOriginalPayload()
        {
            var completedCalls = 0;
            var completedPayload = 0;

            GenericChain<int>.Create(42)
                .OnCompleted(payload => { completedCalls++; completedPayload = payload; })
                .Run();

            Assert.AreEqual(1, completedCalls);
            Assert.AreEqual(42, completedPayload);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void FailurePolicy_WorksWithoutCallbacks(bool stopOnFailure)
        {
            var attempts = 0;
            var laterCalls = 0;

            GenericChain<int>.Create(0, new Configuration(stopOnFailure, maxAttempts: 2))
                .AddLink(data => { attempts++; throw new InvalidOperationException(); })
                .AddLink(data => laterCalls++)
                .Run();

            Assert.AreEqual(2, attempts);
            Assert.AreEqual(stopOnFailure ? 0 : 1, laterCalls);
        }

        [TestMethod]
        public void LargeAttemptBudget_CompletesWithoutRecursiveStackGrowth()
        {
            const int maxAttempts = 10000;
            var attempts = 0;
            var errors = 0;
            var completedCalls = 0;
            var failure = new InvalidOperationException();

            GenericChain<int>.Create(0, new Configuration(false, maxAttempts))
                .AddLink(data => { attempts++; throw failure; })
                .OnError((payload, error) => errors++)
                .OnCompleted(payload => completedCalls++)
                .Run();

            Assert.AreEqual(maxAttempts, attempts);
            Assert.AreEqual(maxAttempts, errors);
            Assert.AreEqual(1, completedCalls);
        }
    }
}
