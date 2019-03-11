using System;
using System.Linq.Expressions;
using System.Reflection;
using MapIt.Utils;

namespace MapIt
{
    public class MapperComposer : ExpressionVisitor
    {
        private static readonly MethodInfo includeMethod = typeof(ExpressionTrees).GetMethod(nameof(ExpressionTrees.Include));

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
                    var entityType = entityMember.Expression.Type;
                    var idProperty = entityType.GetProperty(entityMember.Member.Name + "Id");
                    if (idProperty != null && Nullable.GetUnderlyingType(idProperty.PropertyType) != null)
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