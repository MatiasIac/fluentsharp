using System;

namespace FunctionalSharp.Operations
{
    public sealed class EmptyOperation : Operations
    {
        public override void Throw(Exception ex)
        { }

        public override Operations Then(Action predicate) => this;
    }
}