using FunctionalSharp.Operations;

namespace FunctionalSharp.Validators;

/// <summary>Captures boolean values as immutable conditions.</summary>
public static class Booleans
{
    extension(bool value)
    {
        /// <summary>Enables actions when the captured value is true.</summary>
        public Condition IfTrue => new(value);

        /// <summary>Enables actions when the captured value is false.</summary>
        public Condition IfFalse => new(!value);
    }
}
