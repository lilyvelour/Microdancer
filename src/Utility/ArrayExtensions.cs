using System.Linq.Expressions;
using System.Reflection;
using System;

namespace Microdancer
{
    public static class ArrayExtensions
    {
        public static T? CreateJaggedArray<T>(params int[] lengths) where T : class
        {
            return InitializeJaggedArray(typeof(T).GetElementType(), 0, lengths) as T;
        }

        public static object? InitializeJaggedArray(Type? type, int index, int[] lengths)
        {
            if (type == null) return null;

            var array = Array.CreateInstance(type, lengths[index]);
            var elementType = type.GetElementType();

            if (elementType != null)
            {
                for (int i = 0; i < lengths[index]; i++)
                {
                    array.SetValue(
                        InitializeJaggedArray(elementType, index + 1, lengths), i);
                }
            }

            return array;
        }
    }
}
