using System;

namespace FunctionalSharp.Operations
{
    public sealed class EmptyOperation : OperationsBase
    {
        public override void Throw(Exception ex)
        { }

        public override OperationsBase Then(Action predicate)
        {
            return this;
        }
    }
}