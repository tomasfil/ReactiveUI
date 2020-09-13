// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reflection;

namespace ReactiveUI.Fody.Helpers
{
    /// <summary>
    /// Extension methods for observable as property helpers.
    /// </summary>
    public static class ObservableAsPropertyExtensions
    {
        /// <summary>
        /// To the property execute.
        /// </summary>
        /// <typeparam name="TObj">The type of the object.</typeparam>
        /// <typeparam name="TRet">The type of the ret.</typeparam>
        /// <param name="item">The observable with the return value.</param>
        /// <param name="source">The source.</param>
        /// <param name="property">The property.</param>
        /// <param name="initialValue">The initial value.</param>
        /// <param name="deferSubscription">if set to <c>true</c> [defer subscription].</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <returns>An observable property helper with the specified return value.</returns>
        /// <exception cref="Exception">
        /// Could not resolve expression " + property + " into a property.
        /// or
        /// Backing field not found for " + propertyInfo.
        /// </exception>
        [Obsolete("Use ObservableAsPropertyExtensions.ToFodyProperty()")]
        public static ObservableAsPropertyHelper<TRet> ToPropertyEx<TObj, TRet>(this IObservable<TRet> item, TObj source, Expression<Func<TObj, TRet>> property, TRet initialValue = default, bool deferSubscription = false, IScheduler? scheduler = null)
            where TObj : ReactiveObject
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            var result = item.ToProperty(source, property, initialValue, deferSubscription, scheduler);

            // Now assign the field via reflection.
            var propertyInfo = property.GetPropertyInfo();
            if (propertyInfo == null)
            {
                throw new Exception("Could not resolve expression " + property + " into a property.");
            }

            var field = propertyInfo.DeclaringType.GetTypeInfo().GetDeclaredField("$" + propertyInfo.Name);
            if (field == null)
            {
                throw new Exception("Backing field not found for " + propertyInfo);
            }

            field.SetValue(source, result);

            return result;
        }

        /// <summary>
        /// A placeholder for the Fody system.
        /// It will replace a property, and this call to match ToProperty() on the <seealso cref="OAPHCreationHelperMixin" />,
        /// and the property to be ObservableAsPropertyHelper.
        /// </summary>
        /// <typeparam name="TObj">The object type.</typeparam>
        /// <typeparam name="TRet">The result type.</typeparam>
        /// <param name="target">
        /// The observable to convert to an ObservableAsPropertyHelper.
        /// </param>
        /// <param name="source">
        /// The ReactiveObject that has the property.
        /// </param>
        /// <param name="property">
        /// A Func that points towards the property that will reflect the observable value.
        /// </param>
        /// <param name="deferSubscription">
        /// A value indicating whether the <see cref="ObservableAsPropertyHelper{T}"/>
        /// should defer the subscription to the <paramref name="target"/> source
        /// until the first call to <see cref="ObservableAsPropertyHelper{T}.Value"/>,
        /// or if it should immediately subscribe to the <paramref name="target"/> source.
        /// </param>
        /// <param name="scheduler">
        /// The scheduler that the notifications will be provided on - this should normally
        /// be a Dispatcher-based scheduler.
        /// </param>
        public static void ToFodyProperty<TObj, TRet>(this IObservable<TRet> target, TObj source, Func<TObj, TRet> property, bool deferSubscription = false, IScheduler? scheduler = null)
            where TObj : class, IReactiveObject
        {
            throw new NotImplementedException("This should be replaced by the FODY.");
        }

        /// <summary>
        /// To the property execute.
        /// </summary>
        /// <param name="property">The property name.</param>
        /// <typeparam name="TObj">The type of the source object.</typeparam>
        public static void ToFodyProperty<TObj>(Func<TObj, object> property)
        {
            throw new NotImplementedException("This should be replaced by the FODY.");
        }

        private static PropertyInfo GetPropertyInfo(this LambdaExpression expression)
        {
            var current = expression.Body;
            if (current is UnaryExpression unary)
            {
                current = unary.Operand;
            }

            var call = (MemberExpression)current;
            return (PropertyInfo)call.Member;
        }
    }
}
