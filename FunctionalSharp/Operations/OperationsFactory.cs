namespace FunctionalSharp.Operations
{
    internal static class OperationsFactory
    {
        public static Operations GetOperations(bool resolution)
        {
            if (resolution) return new ImplementedOperation();
            return new EmptyOperation();
        }
    }
}