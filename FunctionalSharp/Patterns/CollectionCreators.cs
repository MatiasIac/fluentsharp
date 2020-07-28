using System;
using System.Collections.Generic;

namespace FunctionalSharp.Patterns
{
    public static class CollectionCreators
    {

        public static class Creator<T> where T : struct, IComparable<T>
        {
            public static ICollection<T> Range(T start, T end = default(T))
            {
                throw new NotImplementedException();
            }
        }
    }
}