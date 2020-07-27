using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionalSharp.Operations
{
    internal sealed class ImplementedOperation : OperationsBase
    {
        public override void Throw(Exception ex)
        {
            base.Throw(ex);
        }

        public override OperationsBase Then(Action predicate)
        {
            return base.Then(predicate);
        }
    }
}