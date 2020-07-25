using System;

namespace FunctionalSharp.Validators
{
    public static class Booleans
    {
        public static void ThrowExceptionIfTrue(this bool expression)
        {
            expression.ThrowExceptionIfTrue(new Exception());
        }

        public static void ThrowExceptionIfTrue(this bool expression, Exception exception)
        {
            if (expression) throw exception;
        }
    }
}