using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionalSharp.Operations
{
    public abstract class OperationsBase
    {
        public virtual void Throw(Exception ex)
        {
            throw ex;
        }

        public virtual OperationsBase Then(Action predicate)
        {
            predicate();
            return this;
        }
    }
}