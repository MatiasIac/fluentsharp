using FunctionalSharp.Operations;

namespace FunctionalSharp.Validators
{
    public static class Objects
    {
        public static Operations.Operations IfNull(this object obj)
        {
            return OperationsFactory.GetOperations(obj == null);
        }
    }
}