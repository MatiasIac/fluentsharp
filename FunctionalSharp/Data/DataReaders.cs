using System;
using System.Collections.Generic;
using System.Data.Common;

namespace FunctionalSharp.Data
{
    public static class DataReaders
    {
        /// <summary>
        /// Having a DbDataReader object, create an instance of <typeparamref name="T"/> for each given row
        /// mapping column names with public properties in <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns>A list of <typeparamref name="T"/></returns>
        public static List<T> ToList<T>(this DbDataReader reader) where T : new() => Read<T>(reader);

        public static (List<T1> Value1, List<T2> Value2) ToMany<T1, T2>(this DbDataReader reader)
            where T1 : new()
            where T2 : new() => (Read<T1>(reader), MoveNextAndRead<T2>(reader));

        public static (List<T1> Value1, List<T2> Value2, List<T3> Value3) ToMany<T1, T2, T3>(this DbDataReader reader)
            where T1 : new()
            where T2 : new()
            where T3 : new() =>
                (Read<T1>(reader), MoveNextAndRead<T2>(reader), MoveNextAndRead<T3>(reader));

        public static (List<T1> Value1, List<T2> Value2, List<T3> Value3, List<T4> Value4) ToMany<T1, T2, T3, T4>(this DbDataReader reader)
            where T1 : new()
            where T2 : new()
            where T3 : new()
            where T4 : new() =>
                (Read<T1>(reader), MoveNextAndRead<T2>(reader), MoveNextAndRead<T3>(reader), MoveNextAndRead<T4>(reader));

        private static List<T> Read<T>(DbDataReader reader)
        {
            var recordList = new List<T>();

            while (reader.Read())
            {
                var item = Activator.CreateInstance<T>();
                var fields = reader.FieldCount;

                for (int i = 0; i < fields; i++)
                {
                    var property = item.GetType().GetProperty(reader.GetName(i));
                    property.SetValue(item, Convert.ChangeType(reader[i], property.PropertyType));
                }

                recordList.Add(item);
            }

            return recordList;
        }

        private static List<T> MoveNextAndRead<T>(DbDataReader reader)
        {
            if (reader.NextResult()) return Read<T>(reader);

            return new List<T>();
        }
    }
}