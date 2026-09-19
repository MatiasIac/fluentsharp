using FunctionalSharp.Decorators;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace FunctionalSharp.Patterns.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class DecoratedLinkDiscoveryTests
    {
        [TestMethod]
        public void Discovery_OnlyConstructsLinksForTheRequestedPayloadType()
        {
            OtherPayloadLink.ConstructorCalls = 0;
            UnrelatedDecoratedClass.ConstructorCalls = 0;
            var payload = new DiscoveryPayload();
            var completedCalls = 0;

            GenericChain<DiscoveryPayload>.Create(payload)
                .AddDecoratedLink("Discovery.PayloadLink")
                .OnCompleted(value => completedCalls++)
                .Run();

            Assert.AreEqual(1, completedCalls);
            Assert.AreEqual(1, payload.Value);
            Assert.AreEqual(0, OtherPayloadLink.ConstructorCalls);
            Assert.AreEqual(0, UnrelatedDecoratedClass.ConstructorCalls);
        }

        [TestMethod]
        [DataRow("Discovery.OtherPayloadLink")]
        [DataRow("Discovery.UnrelatedClass")]
        [DataRow("Discovery.UnrelatedStruct")]
        [DataRow("Discovery.OpenGeneric")]
        [DataRow("Discovery.NoDefaultConstructor")]
        [DataRow("Discovery.PrivateConstructor")]
        [DataRow("Discovery.Abstract")]
        public void IncompatibleDecoratedType_IsReportedAsMissingInsteadOfANullLink(string name)
        {
            var chain = GenericChain<DiscoveryPayload>.Create(new DiscoveryPayload());

            var error = Assert.ThrowsExactly<System.Collections.Generic.KeyNotFoundException>(() => chain.AddDecoratedLink(name));

            StringAssert.Contains(error.Message, name);
            StringAssert.Contains(error.Message, "not found");
        }

        [TestMethod]
        public void SameName_CanBeUsedByLinksForDifferentPayloadTypes()
        {
            var firstPayload = new DiscoveryPayload();
            var secondPayload = new OtherPayload();
            var completedCalls = 0;

            GenericChain<DiscoveryPayload>.Create(firstPayload)
                .AddDecoratedLink("Discovery.PayloadLink")
                .OnCompleted(value => completedCalls++)
                .Run();
            GenericChain<OtherPayload>.Create(secondPayload)
                .AddDecoratedLink("Discovery.PayloadLink")
                .OnCompleted(value => completedCalls++)
                .Run();

            Assert.AreEqual(2, completedCalls);
            Assert.AreEqual(1, firstPayload.Value);
            Assert.AreEqual(10, secondPayload.Value);
        }

        [TestMethod]
        public void ConcreteLink_CanInheritItsDecoration_FromAnAbstractBase()
        {
            var payload = new DiscoveryPayload();
            var completedCalls = 0;

            GenericChain<DiscoveryPayload>.Create(payload)
                .AddDecoratedLink("Discovery.Inherited")
                .OnCompleted(value => completedCalls++)
                .Run();

            Assert.AreEqual(1, completedCalls);
            Assert.AreEqual(20, payload.Value);
        }

        public sealed class DiscoveryPayload
        {
            public int Value { get; set; }
        }

        public sealed class OtherPayload
        {
            public int Value { get; set; }
        }

        [Link("Discovery.PayloadLink")]
        public sealed class PayloadLink : LinkBase<DiscoveryPayload>
        {
            public override void OnExecute(DataCargo<DiscoveryPayload> data) => data.Payload.Value++;
        }

        [Link("Discovery.OtherPayloadLink")]
        public sealed class OtherPayloadLink : LinkBase<OtherPayload>
        {
            public static int ConstructorCalls;

            public OtherPayloadLink() => ConstructorCalls++;

            public override void OnExecute(DataCargo<OtherPayload> data) => data.Payload.Value++;
        }

        [Link("Discovery.UnrelatedClass")]
        public sealed class UnrelatedDecoratedClass
        {
            public static int ConstructorCalls;

            public UnrelatedDecoratedClass() => ConstructorCalls++;
        }

        [Link("Discovery.UnrelatedStruct")]
        public struct UnrelatedDecoratedStruct { }

        [Link("Discovery.PayloadLink")]
        public sealed class SameNameOtherPayloadLink : LinkBase<OtherPayload>
        {
            public override void OnExecute(DataCargo<OtherPayload> data) => data.Payload.Value += 10;
        }

        [Link("Discovery.Abstract")]
        public abstract class AbstractLink : LinkBase<DiscoveryPayload> { }

        [Link("Discovery.OpenGeneric")]
        public sealed class OpenGenericLink<TUnused> : LinkBase<DiscoveryPayload>
        {
            public override void OnExecute(DataCargo<DiscoveryPayload> data) => data.Payload.Value++;
        }

        [Link("Discovery.NoDefaultConstructor")]
        public sealed class NoDefaultConstructorLink : LinkBase<DiscoveryPayload>
        {
            public NoDefaultConstructorLink(int requiredValue) { }

            public override void OnExecute(DataCargo<DiscoveryPayload> data) => data.Payload.Value++;
        }

        [Link("Discovery.PrivateConstructor")]
        public sealed class PrivateConstructorLink : LinkBase<DiscoveryPayload>
        {
            private PrivateConstructorLink() => throw new InvalidOperationException("Must not be constructed.");

            public override void OnExecute(DataCargo<DiscoveryPayload> data) => data.Payload.Value++;
        }

        [Link("Discovery.Inherited")]
        public abstract class DecoratedBaseLink : LinkBase<DiscoveryPayload> { }

        public sealed class InheritedLink : DecoratedBaseLink
        {
            public override void OnExecute(DataCargo<DiscoveryPayload> data) => data.Payload.Value += 20;
        }

        public sealed class UndecoratedLink : LinkBase<DiscoveryPayload>
        {
            public UndecoratedLink() => throw new InvalidOperationException("Must not be constructed.");

            public override void OnExecute(DataCargo<DiscoveryPayload> data) => data.Payload.Value++;
        }
    }
}
