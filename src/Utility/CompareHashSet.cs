using System;
using System.Collections.Generic;

namespace Microdancer
{
    public static class CompareHashSet<T>
    {
        private class Wrapper<TValue> : IEqualityComparer<T>
        {
            private readonly Func<T, TValue> _func;
            private readonly IEqualityComparer<TValue> _comparer;

            public Wrapper(Func<T, TValue> func, IEqualityComparer<TValue>? comparer)
            {
                _func = func;
                _comparer = comparer ?? EqualityComparer<TValue>.Default;
            }

            public bool Equals(T? x, T? y)
            {
                if (x is null || y is null)
                {
                    return false;
                }

                return _comparer.Equals(_func(x), _func(y));
            }

            public int GetHashCode(T obj)
            {
                var key = _func(obj);
                if (key is null)
                {
                    return 0;
                }

                return _comparer.GetHashCode(key);
            }
        }

        public static HashSet<T> Create<TValue>(Func<T, TValue> func)
        {
            return new HashSet<T>(new Wrapper<TValue>(func, null));
        }

        public static HashSet<T> Create<TValue>(Func<T, TValue> func, IEqualityComparer<TValue> comparer)
        {
            return new HashSet<T>(new Wrapper<TValue>(func, comparer));
        }
    }
}
