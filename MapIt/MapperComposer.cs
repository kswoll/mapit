﻿using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using MapIt.Utils;

namespace MapIt
{
    public class MapperComposer : ExpressionVisitor
    {
        private static readonly MethodInfo includeMethod = typeof(ExpressionTrees).GetMethod(nameof(ExpressionTrees.Include));
        private static readonly ConcurrentDictionary<MemberInfo, (PropertyInfo, bool)> nullableIdPropertiesByEntityProperty = new ConcurrentDictionary<MemberInfo, (PropertyInfo, bool)>();

        /// <summary>
        /// Takes an expression tree and injects other mappers when usages of ExpressionTrees.Include
        /// are found.
        /// </summary>
        public static Expression<Func<TDbEntity, TModelEntity>> Compose<TDbEntity, TModelEntity>(Expression<Func<TDbEntity, TModelEntity>> mapper)
        {
            var composer = new MapperComposer();
            var composed = composer.Visit(mapper);
            return (Expression<Func<TDbEntity, TModelEntity>>)composed;
        }

        private MapperComposer()
        {
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // Whenever we find a call to ExpressionTrees.Include, replace it with the body
            // of the referenced mapper, replacing references to the entity parameter of the
            // included mapper with the expression passed as the first argument.
            if (node.Method.IsGenericMethod && node.Method.GetGenericMethodDefinition() == includeMethod)
            {
                // The reference to the entity the included mapper applies to (passed to the Include method)
                var entity = node.Arguments[0];

                Expression checkForNullAgainst = entity;
                if (entity is MemberExpression entityMember)
                {
//                    var entityType = entityMember.Expression.Type;
                    var (idProperty, isNullable) = nullableIdPropertiesByEntityProperty.GetOrAdd(entityMember.Member, x =>
                    {
                        var property = x.DeclaringType.GetProperty(x.Name + "Id");
                        if (property != null)
                        {
                            var propertyIsNullable = Nullable.GetUnderlyingType(property.PropertyType) != null;
                            return (property, propertyIsNullable);
                        }
                        else
                        {
                            return ((PropertyInfo)null, false);
                        }
                    });

                    // Work around EF core bug:
                    // https://github.com/aspnet/EntityFrameworkCore/issues/14987
                    // It doesn't like comparing the relationship to null but is happy comparing the corresponding
                    // FooId property.
                    if (idProperty != null && isNullable)
                    {
                        checkForNullAgainst = Expression.Property(entityMember.Expression, idProperty);
                    }
                }

                // The mapper to be used to map the entity
                var includeMapper = (LambdaExpression)node.Arguments[1].Evaluate();

                // Substitutes references to the entity parameter of the referenced mapper with
                // the entity supplied to the Include method
                var body = LambdaBinder.BindBody(includeMapper, entity);
                var nullExpression = Expression.Constant(null, checkForNullAgainst.Type);
                body = Expression.Condition(Expression.NotEqual(checkForNullAgainst, nullExpression), body, Expression.Constant(null, body.Type), body.Type);
                return body;
            }
            return base.VisitMethodCall(node);
        }
    }
}