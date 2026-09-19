using System;
using FunctionalSharp.Decorators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunctionalSharp.Patterns.Tests;

[TestClass]
public class DiscoveryValidationTests
{
    [TestMethod]
    public void DuplicateNames_AreDiagnosedBeforeConstructingEitherLink()
    {
        var chain = GenericChain<DuplicatePayload>.Create(new DuplicatePayload());
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => chain.AddDecoratedLink("duplicate"));
        StringAssert.Contains(error.Message, "duplicate");
        StringAssert.Contains(error.Message, "Multiple links");
    }

    [TestMethod]
    public void Discovery_ConstructsOnlyTheRequestedType()
    {
        var calls = 0;
        GenericChain<SelectivePayload>.Create(new SelectivePayload()).AddDecoratedLink("selected")
            .OnCompleted(_ => calls++).Run();
        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public void GenericChain_RejectsNullRegistrationsAndCallbacks()
    {
        var chain = GenericChain<int>.Create(0);
        Assert.ThrowsExactly<ArgumentNullException>(() => chain.AddLink((Action<DataCargo<int>>)null));
        Assert.ThrowsExactly<ArgumentNullException>(() => chain.AddLink((LinkBase<int>)null));
        Assert.ThrowsExactly<ArgumentNullException>(() => chain.OnCompleted(null));
        Assert.ThrowsExactly<ArgumentNullException>(() => chain.OnError(null));
        Assert.ThrowsExactly<ArgumentException>(() => chain.AddDecoratedLink(" "));
    }

    public sealed class DuplicatePayload { }
    public sealed class SelectivePayload { }

    [Link("duplicate")]
    public sealed class FirstDuplicate : LinkBase<DuplicatePayload>
    {
        public FirstDuplicate() => throw new Exception("Duplicate validation must happen before construction.");
        public override void OnExecute(DataCargo<DuplicatePayload> data) { }
    }

    [Link("duplicate")]
    public sealed class SecondDuplicate : LinkBase<DuplicatePayload>
    {
        public SecondDuplicate() => throw new Exception("Duplicate validation must happen before construction.");
        public override void OnExecute(DataCargo<DuplicatePayload> data) { }
    }

    [Link("selected")]
    public sealed class SelectedLink : LinkBase<SelectivePayload>
    {
        public override void OnExecute(DataCargo<SelectivePayload> data) { }
    }

    [Link("unselected")]
    public sealed class UnselectedLink : LinkBase<SelectivePayload>
    {
        public UnselectedLink() => throw new Exception("An unrequested constructor must not run.");
        public override void OnExecute(DataCargo<SelectivePayload> data) { }
    }
}
