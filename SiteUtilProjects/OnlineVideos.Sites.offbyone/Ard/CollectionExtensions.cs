using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace OnlineVideos.Sites.Ard
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// If an enumerable is null, and it has a default constructor, return an empty collection
        /// by calling the default constructor.
        /// </summary>
        /// <typeparam name="TEnumerable">The type of the Enumerable</typeparam>
        /// <param name="source">A sequence of values to return when not null</param>
        /// <returns>The source sequence, or a new empty one if source was null.</returns>
        public static IEnumerable AsEmptyIfNull(this IEnumerable source) => source ?? Enumerable.Empty<object>();

        /// <summary>
        /// Returns an empty System.Collections.Generic.IEnumerable`1 if the supplied source
        /// is null.
        /// </summary>
        /// <typeparam name="T">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values to return when not null.</param>
        /// <returns>The source sequence, or a new empty one if source was null.</returns>
        public static IEnumerable<T> AsEmptyIfNull<T>(this IEnumerable<T> source) => source ?? Enumerable.Empty<T>();
        //public static IReadOnlyCollection<T> EmptyIfNull<T>(this IReadOnlyCollection<T> enumerable) => enumerable ?? Array.Empty<T>();

        /// <summary>
        /// Checks a sequence for null or empty
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(this IEnumerable? enumerable)
            => !enumerable?.GetEnumerator().MoveNext() ?? true;

        /// <summary>
        /// Checks a sequence for null or empty
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T>? enumerable)
            => !enumerable?.Any() ?? true;

        /// <summary>
        /// removes default/null values in a sequence
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<T> ExceptDefault<T>(this IEnumerable<T> source) => source.Where(e => !Equals(e, default));

        /// <summary>
        /// Dictionary Extension for getting a value and not throwing <see cref="KeyNotFoundException"/>
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }
    }
}
