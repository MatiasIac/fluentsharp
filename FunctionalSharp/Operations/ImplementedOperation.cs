using System;

namespace FunctionalSharp.Operations
{
    internal sealed class ImplementedOperation : Operations
    {
        public override void Throw(Exception ex)
        {
            base.Throw(ex);
        }

        public override Operations Then(Action predicate)
        {
            return base.Then(predicate);
        }
    }
}