using System;

namespace FunctionalSharp.Operations
{
    public abstract class Operations
    {
        public virtual void Throw(Exception ex)
        {
            throw ex;
        }

        public virtual Operations Then(Action predicate)
        {
            predicate();
            return this;
        }
    }
}