using FunctionalSharp.Operations;

namespace FunctionalSharp.Validators
{
    /// <summary>Creates conditional operation chains from null checks.</summary>
    public static class Objects
    {
        /// <summary>
        /// Enables following execution expressions if the 
        /// current object is Null
        /// </summary>
        /// <param name="obj">Nullable object</param>
        /// <returns>Set of valid operations</returns>
        public static Operations.Operations IfNull(this object obj)
            => OperationsFactory.GetOperations(obj == null);
    }
}
