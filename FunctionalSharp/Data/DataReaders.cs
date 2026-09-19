using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace FunctionalSharp.Data
{
    /// <summary>Maps data reader result sets to lists of objects using public property names.</summary>
    /// <remarks>
    /// The caller owns the reader. Each column must uniquely match a public writable instance
    /// property. Database nulls map to null for reference and nullable value types. Other conversions
    /// use invariant culture. Invalid schemas and mapping failures throw DataException with context.
    /// </remarks>
    public static class DataReaders
    {
        /// <summary>Maps the current result set with an explicit row mapper, without reflection or constructor constraints.</summary>
        /// <remarks>The mapper receives each current row exactly once and must not advance or dispose the reader.
        /// Mapper errors propagate unchanged. The caller owns the reader, including after failure.</remarks>
        public static List<T> ToList<T>(this DbDataReader reader, Func<DbDataReader, T> map)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(map);
            var result = new List<T>();
            while (reader.Read()) result.Add(map(reader));
            return result;
        }

        /// <summary>
        /// Having a DbDataReader object, create an instance of <typeparamref name="T"/> for each given row
        /// mapping column names with public properties in <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">The destination type, with a public parameterless constructor.</typeparam>
        /// <param name="reader">The reader positioned before the rows to map.</param>
        /// <param name="ignoreCase">Whether column and property names are matched without case sensitivity.</param>
        /// <returns>A list of <typeparamref name="T"/></returns>
        /// <exception cref="ArgumentNullException">reader is null.</exception>
        /// <exception cref="DataException">The schema is invalid or a value cannot be mapped.</exception>
        [RequiresUnreferencedCode("Automatic mapping reflects over destination properties. Use the explicit row-mapper overload instead.")]
        public static List<T> ToList<T>(this DbDataReader reader, bool ignoreCase = false) where T : new() => Read<T>(reader, ignoreCase);

        /// <summary>Maps the current and next result sets to two typed lists.</summary>
        /// <remarks>A missing subsequent result set produces an empty list. The reader is not disposed.</remarks>
        [RequiresUnreferencedCode("Automatic mapping reflects over destination properties. Use explicit row mappers and NextResult instead.")]
        public static (List<T1> Value1, List<T2> Value2) ToMany<T1, T2>(this DbDataReader reader, bool ignoreCase = false)
            where T1 : new()
            where T2 : new() => (Read<T1>(reader, ignoreCase), MoveNextAndRead<T2>(reader, ignoreCase));

        /// <summary>Maps the current and next two result sets to three typed lists.</summary>
        /// <remarks>Missing subsequent result sets produce empty lists. The reader is not disposed.</remarks>
        [RequiresUnreferencedCode("Automatic mapping reflects over destination properties. Use explicit row mappers and NextResult instead.")]
        public static (List<T1> Value1, List<T2> Value2, List<T3> Value3) ToMany<T1, T2, T3>(this DbDataReader reader, bool ignoreCase = false)
            where T1 : new()
            where T2 : new()
            where T3 : new() =>
                (Read<T1>(reader, ignoreCase), MoveNextAndRead<T2>(reader, ignoreCase), MoveNextAndRead<T3>(reader, ignoreCase));

        /// <summary>Maps the current and next three result sets to four typed lists.</summary>
        /// <remarks>Missing subsequent result sets produce empty lists. The reader is not disposed.</remarks>
        [RequiresUnreferencedCode("Automatic mapping reflects over destination properties. Use explicit row mappers and NextResult instead.")]
        public static (List<T1> Value1, List<T2> Value2, List<T3> Value3, List<T4> Value4) ToMany<T1, T2, T3, T4>(this DbDataReader reader, bool ignoreCase = false)
            where T1 : new()
            where T2 : new()
            where T3 : new()
            where T4 : new() =>
                (Read<T1>(reader, ignoreCase), MoveNextAndRead<T2>(reader, ignoreCase), MoveNextAndRead<T3>(reader, ignoreCase), MoveNextAndRead<T4>(reader, ignoreCase));

        private sealed class ColumnMapping
        {
            public string Name { get; }
            public PropertyInfo Property { get; }
            public Type ValueType { get; }
            public bool AllowsNull { get; }

            public ColumnMapping(string name, PropertyInfo property)
            {
                Name = name;
                Property = property;
                var nullableType = Nullable.GetUnderlyingType(property.PropertyType);
                ValueType = nullableType ?? property.PropertyType;
                AllowsNull = nullableType != null || !property.PropertyType.IsValueType;
            }
        }

        [RequiresUnreferencedCode("Reflects over destination properties.")]
        private static List<T> Read<T>(DbDataReader reader, bool ignoreCase) where T : new()
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            // Resolve once for this result set; the next result may have a different schema.
            var mappings = CreateMappings<T>(reader, ignoreCase);
            var recordList = new List<T>();
            var rowNumber = 0;

            while (reader.Read())
            {
                rowNumber++;
                // Keep one box for a struct so every setter updates the instance we return.
                object item = new T();

                for (var ordinal = 0; ordinal < mappings.Length; ordinal++)
                {
                    var mapping = mappings[ordinal];
                    var value = reader.GetValue(ordinal);
                    try
                    {
                        mapping.Property.SetValue(item, ConvertValue(value, mapping));
                    }
                    catch (Exception ex) when (ex is InvalidCastException || ex is FormatException
                        || ex is OverflowException || ex is ArgumentException || ex is TargetInvocationException)
                    {
                        throw new DataException(
                            $"Cannot map column '{mapping.Name}' (ordinal {ordinal}) at row {rowNumber} " +
                            $"to property '{typeof(T).FullName}.{mapping.Property.Name}' " +
                            $"of type '{mapping.Property.PropertyType}'.",
                            ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex);
                    }
                }

                recordList.Add((T)item);
            }

            return recordList;
        }

        [RequiresUnreferencedCode("Reflects over destination properties.")]
        private static ColumnMapping[] CreateMappings<T>(DbDataReader reader, bool ignoreCase)
        {
            var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var mappings = new ColumnMapping[reader.FieldCount];
            var assignedProperties = new HashSet<PropertyInfo>();

            for (var ordinal = 0; ordinal < mappings.Length; ordinal++)
            {
                var name = reader.GetName(ordinal);
                PropertyInfo? match = null;
                foreach (var property in properties)
                {
                    if (!string.Equals(property.Name, name, comparison)
                        || property.GetSetMethod() == null || property.GetIndexParameters().Length != 0)
                        continue;

                    if (match != null)
                        throw new DataException(
                            $"Column '{name}' (ordinal {ordinal}) matches multiple writable properties on '{typeof(T).FullName}'.");

                    match = property;
                }

                if (match == null)
                    throw new DataException(
                        $"Column '{name}' (ordinal {ordinal}) has no matching public writable instance property on '{typeof(T).FullName}'.");

                if (!assignedProperties.Add(match))
                    throw new DataException(
                        $"Column '{name}' (ordinal {ordinal}) maps to property '{typeof(T).FullName}.{match.Name}', which is already mapped by another column.");

                mappings[ordinal] = new ColumnMapping(name, match);
            }

            return mappings;
        }

        private static object? ConvertValue(object? value, ColumnMapping mapping)
        {
            if (value == null || value == DBNull.Value)
            {
                if (mapping.AllowsNull) return null;
                throw new InvalidCastException($"A database null cannot be assigned to '{mapping.Property.PropertyType}'.");
            }

            var targetType = mapping.ValueType;
            if (targetType.IsInstanceOfType(value)) return value;

            if (targetType.IsEnum)
            {
                if (value is string text) return Enum.Parse(targetType, text, ignoreCase: false);
                var underlyingValue = Convert.ChangeType(value, Enum.GetUnderlyingType(targetType), CultureInfo.InvariantCulture);
                return Enum.ToObject(targetType, underlyingValue);
            }

            if (targetType == typeof(Guid) && value is string guidText) return Guid.Parse(guidText);

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        [RequiresUnreferencedCode("Reflects over destination properties.")]
        private static List<T> MoveNextAndRead<T>(DbDataReader reader, bool ignoreCase) where T : new()
        {
            if (reader.NextResult()) return Read<T>(reader, ignoreCase);

            return new List<T>();
        }
    }
}
