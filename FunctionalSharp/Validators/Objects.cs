using FunctionalSharp.Operations;

namespace FunctionalSharp.Validators
{
    public static class Objects
    {
        public static OperationsBase IfNull(this object obj)
        {
            return OperationsFactory.GetOperations(obj == null);
        }
    }
}