using FunctionalSharp.Operations;

namespace FunctionalSharp.Validators
{
    public static class Booleans
    {
        public static Operations.Operations IfTrue(this bool expression)
        {
            return OperationsFactory.GetOperations(expression);
        }

        public static Operations.Operations IfFalse(this bool expression)
        {
            return OperationsFactory.GetOperations(!expression);
        }
    }
}