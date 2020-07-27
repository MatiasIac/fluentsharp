using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionalSharp.Operations
{
    internal static class OperationsFactory
    {
        public static OperationsBase GetOperations(bool resolution)
        {
            if (resolution) return new ImplementedOperation();
            return new EmptyOperation();
        }
    }
}