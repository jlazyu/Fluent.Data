using System;
using System.Collections.Generic;
using System.Linq;

namespace Fluent.Data.Extensions
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Extension method that returns a collection of <see cref="Type"/> that are decorated with an <see cref="Attribute"/>
        /// of type <see cref="TAttribute"/>.
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<Type> GetTypesWithCustomAttribute<TAttribute>(this Type type)
        {
            return AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Where(x => x.FullName.StartsWith("Fluent.Data"))
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract &&
                            p.GetCustomAttributes(true).OfType<TAttribute>().Any());
        }

        /// <summary>
        /// Extension method that returns the first <see cref="Type"/> found in the <paramref name="types"/> that is found using
        /// the <paramref name="valueSelector"/> and is of type <see cref="TAttribute"/> and has a value of <paramref name="value"/>
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <typeparam name="TAttributeValue"></typeparam>
        /// <param name="types"></param>
        /// <param name="valueSelector"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Type GetTypeWithCustomAttributeValue<TAttribute, TAttributeValue>(this IEnumerable<Type> types, Func<TAttribute, TAttributeValue> valueSelector, TAttributeValue value)
        {
            return types
                .FirstOrDefault(x =>
                    valueSelector(x.GetCustomAttributes(false).OfType<TAttribute>().FirstOrDefault()).Equals(value));
        }
    }
}
