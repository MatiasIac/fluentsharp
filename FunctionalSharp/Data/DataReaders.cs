using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;

namespace FunctionalSharp.Data
{
    /// <summary>Maps data reader result sets to lists of objects using public property names.</summary>
    /// <remarks>
    /// The caller owns the reader. Each column must match a writable property and contain a value
    /// convertible to its type. DBNull and nullable property types require caller-side handling.
    /// </remarks>
    public static class DataReaders
    {
        /// <summary>
        /// Having a DbDataReader object, create an instance of <typeparamref name="T"/> for each given row
        /// mapping column names with public properties in <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">The destination type, with a public parameterless constructor.</typeparam>
        /// <param name="reader">The reader positioned before the rows to map.</param>
        /// <param name="ignoreCase">Whether column and property names are matched without case sensitivity.</param>
        /// <returns>A list of <typeparamref name="T"/></returns>
        public static List<T> ToList<T>(this DbDataReader reader, bool ignoreCase = false) where T : new() => Read<T>(reader, ignoreCase);

        /// <summary>Maps the current and next result sets to two typed lists.</summary>
        /// <remarks>A missing subsequent result set produces an empty list. The reader is not disposed.</remarks>
        public static (List<T1> Value1, List<T2> Value2) ToMany<T1, T2>(this DbDataReader reader, bool ignoreCase = false)
            where T1 : new()
            where T2 : new() => (Read<T1>(reader, ignoreCase), MoveNextAndRead<T2>(reader, ignoreCase));

        /// <summary>Maps the current and next two result sets to three typed lists.</summary>
        /// <remarks>Missing subsequent result sets produce empty lists. The reader is not disposed.</remarks>
        public static (List<T1> Value1, List<T2> Value2, List<T3> Value3) ToMany<T1, T2, T3>(this DbDataReader reader, bool ignoreCase = false)
            where T1 : new()
            where T2 : new()
            where T3 : new() =>
                (Read<T1>(reader, ignoreCase), MoveNextAndRead<T2>(reader, ignoreCase), MoveNextAndRead<T3>(reader, ignoreCase));

        /// <summary>Maps the current and next three result sets to four typed lists.</summary>
        /// <remarks>Missing subsequent result sets produce empty lists. The reader is not disposed.</remarks>
        public static (List<T1> Value1, List<T2> Value2, List<T3> Value3, List<T4> Value4) ToMany<T1, T2, T3, T4>(this DbDataReader reader, bool ignoreCase = false)
            where T1 : new()
            where T2 : new()
            where T3 : new()
            where T4 : new() =>
                (Read<T1>(reader, ignoreCase), MoveNextAndRead<T2>(reader, ignoreCase), MoveNextAndRead<T3>(reader, ignoreCase), MoveNextAndRead<T4>(reader, ignoreCase));

        private static List<T> Read<T>(DbDataReader reader, bool ignoreCase)
        {
            var recordList = new List<T>();

            var bindingFlags = ignoreCase ? 
                (BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase) : 
                (BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);

            while (reader.Read())
            {
                var item = Activator.CreateInstance<T>();
                var fields = reader.FieldCount;

                for (int i = 0; i < fields; i++)
                {
                    var property = item.GetType().GetProperty(reader.GetName(i), bindingFlags);
                    property.SetValue(item, Convert.ChangeType(reader[i], property.PropertyType));
                }

                recordList.Add(item);
            }

            return recordList;
        }

        private static List<T> MoveNextAndRead<T>(DbDataReader reader, bool ignoreCase)
        {
            if (reader.NextResult()) return Read<T>(reader, ignoreCase);

            return new List<T>();
        }
    }
}
