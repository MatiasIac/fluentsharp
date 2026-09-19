using FunctionalSharp.Operations;

namespace FunctionalSharp.Validators;

/// <summary>Captures null checks without losing the value's type.</summary>
public static class Objects
{
    extension<T>(T? value) where T : class
    {
        /// <summary>Enables actions when the reference is null.</summary>
        public Condition IfNull => new(value is null);

        /// <summary>Passes the captured non-null reference to typed callbacks.</summary>
        public ValueCondition<T> IfNotNull => new(value!, value is not null);
    }

    extension<T>(T? value) where T : struct
    {
        /// <summary>Enables actions when the nullable value has no value.</summary>
        public Condition IfNull => new(!value.HasValue);

        /// <summary>Passes the underlying value to typed callbacks when present.</summary>
        public ValueCondition<T> IfNotNull => new(value.GetValueOrDefault(), value.HasValue);
    }
}
