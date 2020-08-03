using System;

namespace FunctionalSharp.Operations
{
    public abstract class Operations
    {
        /// <summary>
        /// Throws an exception
        /// </summary>
        /// <param name="ex">Exception to be thrown</param>
        public virtual void Throw(Exception ex)
        {
            throw ex;
        }

        /// <summary>
        /// Execute the defined predicate and enables
        /// other operations
        /// </summary>
        /// <param name="predicate">Action to be executed</param>
        /// <returns>Set of valid operations</returns>
        public virtual Operations Then(Action predicate)
        {
            predicate();
            return this;
        }
    }
}