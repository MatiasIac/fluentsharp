using FunctionalSharp.Operations;

namespace FunctionalSharp.Validators
{
    public static class Booleans
    {
        public static OperationsBase IfTrue(this bool expression)
        {
            return OperationsFactory.GetOperations(expression);
        }

        public static OperationsBase IfFalse(this bool expression)
        {
            return OperationsFactory.GetOperations(!expression);
        }
    }
}