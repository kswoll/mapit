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

                // The mapper to be used to map the entity
                var includeMapper = (LambdaExpression)node.Arguments[1].Evaluate();

                // Substitutes references to the entity parameter of the referenced mapper with
                // the entity supplied to the Include method
                var body = LambdaBinder.BindBody(includeMapper, entity);
                body = Expression.Condition(Expression.NotEqual(entity, Expression.Constant(null)), body, Expression.Constant(null, body.Type), body.Type);
                return body;
            }
            return base.VisitMethodCall(node);
        }
    }
}