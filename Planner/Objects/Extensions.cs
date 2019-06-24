using System;
using System.Collections.Generic;
using System.Text;

namespace Planner.Objects
{
    /// <summary>
    /// Random utility functions.
    /// Ignore this.
    /// </summary>
    public static class Extensions
    {
        public static List<T> Randomize<T>(this IEnumerable<T> source, Random r)
        {
            var list = new List<T>();
            foreach (var item in source)
            {
                var i = r.Next(list.Count + 1);
                if (i == list.Count)
                {
                    list.Add(item);
                }
                else
                {
                    var temp = list[i];
                    list[i] = item;
                    list.Add(temp);
                }
            }
            return list;
        }

        public static T2 Get<T1,T2>(this Dictionary<T1,T2> dictionary, T1 key)
        {
            return dictionary.ContainsKey(key) ? dictionary[key] : default(T2);
        }
        public static void When(this Action action, bool condition)
        {
            if (condition) action();
        }
        public static void Then(this bool condition, Action action)
        {
            if (condition) action();
        }
        public static void Then<T>(this bool condition, Action<T> action, T param)
        {
            if (condition) action(param);
        }
        public static void Then<T>(this bool condition, Action<T,T> action, T param1, T param2)
        {
            if (condition) action(param1, param2);
        }

        public static Action Execute(Action action)
        {
            return action;
        }

        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (T item in enumerable) action(item);
        }

        public static long GreatestCommonFactor(long a, long b)
        {
            while (b != 0)
            {
                long temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }
        public static long LeastCommonMultiple(long a, long b)
        {
            return (a / GreatestCommonFactor(a, b)) * b;
        }
    }
}
