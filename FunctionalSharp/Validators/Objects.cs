using FunctionalSharp.Operations;

namespace FunctionalSharp.Validators
{
    public static class Objects
    {
        /// <summary>
        /// Enables following execution expressions if the 
        /// current object is Null
        /// </summary>
        /// <param name="obj">Nullable object</param>
        /// <returns>Set of valid operations</returns>
        public static Operations.Operations IfNull(this object obj)
        {
            return OperationsFactory.GetOperations(obj == null);
        }
    }
}