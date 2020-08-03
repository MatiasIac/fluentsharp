using FunctionalSharp.Operations;

namespace FunctionalSharp.Validators
{
    public static class Booleans
    {
        /// <summary>
        /// Enables following execution expressions if the 
        /// current evaluated expression is True
        /// </summary>
        /// <param name="expression">Boolean expression</param>
        /// <returns>Set of valid operations</returns>
        public static Operations.Operations IfTrue(this bool expression)
        {
            return OperationsFactory.GetOperations(expression);
        }

        /// <summary>
        /// Enables following execution expressions if the 
        /// current evaluated expression is False
        /// </summary>
        /// <param name="expression">Boolean expression</param>
        /// <returns>Set of valid operations</returns>
        public static Operations.Operations IfFalse(this bool expression)
        {
            return OperationsFactory.GetOperations(!expression);
        }
    }
}