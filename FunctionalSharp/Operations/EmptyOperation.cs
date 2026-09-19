using System;

namespace FunctionalSharp.Operations
{
    /// <summary>Represents an unmatched condition; subsequent actions and exceptions are ignored.</summary>
    public sealed class EmptyOperation : Operations
    {
        /// <summary>Ignores the supplied exception.</summary>
        /// <param name="ex">The exception that would be thrown for a matching condition.</param>
        public override void Throw(Exception ex)
        { }

        /// <summary>Skips the action and returns this inactive operation chain.</summary>
        /// <param name="predicate">The action that would run for a matching condition.</param>
        /// <returns>This operation chain.</returns>
        public override Operations Then(Action predicate) => this;
    }
}
