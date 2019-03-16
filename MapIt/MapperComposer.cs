using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MapIt.Utils;

namespace MapIt
{
    public class MapperComposer : ExpressionVisitor
    {
        private static readonly MethodInfo includeMethodExpression = typeof(ExpressionTrees).GetMethods().Single(x => x.Name == nameof(ExpressionTrees.Include) && x.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>));
        private static readonly MethodInfo includeMethodMapper = typeof(ExpressionTrees).GetMethods().Single(x => x.Name == nameof(ExpressionTrees.Include) && x.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(EntityMapper<,>));
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
            var isIncludeMethod = node.Method.GetGenericMethodDefinition() == includeMethodExpression;
            var isIncludeMapper = node.Method.GetGenericMethodDefinition() == includeMethodMapper;
            if (node.Method.IsGenericMethod && (isIncludeMethod || isIncludeMapper))
            {
                // The reference to the entity the included mapper applies to (passed to the Include method)
                var entity = node.Arguments[0];

                Expression checkForNullAgainst = null;// entity;  EFCore has a lot of bugs when comparing relationships against null (vs. using an Id property) -- just don't bother for now if we can't find the id property
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
                var includeMapper = isIncludeMethod ? (LambdaExpression)node.Arguments[1].Evaluate() : ((IEntityMapper)node.Arguments[1].Evaluate()).Expression;

                // Substitutes references to the entity parameter of the referenced mapper with
                // the entity supplied to the Include method
                var body = LambdaBinder.BindBody(includeMapper, entity);
                if (checkForNullAgainst != null)
                {
                    var nullExpression = Expression.Constant(null, checkForNullAgainst.Type);
                    body = Expression.Condition(Expression.NotEqual(checkForNullAgainst, nullExpression), body, Expression.Constant(null, body.Type), body.Type);
                }
                return body;
            }
            return base.VisitMethodCall(node);
        }
    }
}