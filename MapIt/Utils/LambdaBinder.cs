using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MapIt.Utils
{
    /// <summary>
    /// Provides a facility to inject expressions into a lambda's body taking the
    /// place of some or all of the original parameter expressions.  This can be
    /// useful when the semantics of the lambda body is what you want, but you want
    /// to use other expressions in place of the lambda's parameters when performing
    /// transformations of expression trees.
    /// </summary>
	public class LambdaBinder : ExpressionVisitor
    {
        private ParameterExpression[] targetParameters;
        private HashSet<ParameterExpression> targetParametersSet;
        private Expression[] replacementParameters;
        private Dictionary<Expression, Expression> replacementParameterByTargetParameter;

        private LambdaBinder()
        {
        }

        private T InternalBind<T>(LambdaExpression lambda, Func<LambdaExpression, T> target, params Expression[] parameters)
            where T : Expression
        {
            if (lambda.Parameters.Count != parameters.Length)
                throw new ArgumentException("parameter counts for the lambda expression and the passed-in parameters must match.");

            // Store the lambda parameters for the target (for readability via the variable name)
            targetParameters = lambda.Parameters.ToArray();

            // Convert it into a HasSet for perf
            targetParametersSet = new HashSet<ParameterExpression>(targetParameters);

            // Store the replacement parameters (for readability via the variable name)
            replacementParameters = parameters;

            // Create a dictionary to efficiently fetch the replacement parameter based on the target parameter
            replacementParameterByTargetParameter = targetParameters.Zip(replacementParameters, (x, y) => new { x, y }).ToDictionary(x => (Expression)x.x, x => x.y);

            // This uses the visitor pattern to walk the expression nodes and replace any reference to the target
            // parameter with the replacement parameter
            return (T)base.Visit(target(lambda));
        }

        /// <summary>
        /// Substitutes the parameters of the lambda with those specified.  Correlating the lambda
        /// parameters with the provided parameters is positional, so they need to have the same
        /// number of elements in addition to being located in the proper slot so that they
        /// end up representing the same parameter.  This version returns a new LambdaExpression
        /// with the specified parameter substitutions.
        /// </summary>
        /// <param name="lambda">The lambda expression to bind to</param>
        /// <param name="parameters">The parameters that should be used as substitutions for the
        /// parameters referenced by the original lambda body</param>
        /// <returns>A new version of the LambdaExpression with the specified substitutions</returns>
        public static LambdaExpression Bind(LambdaExpression lambda, params Expression[] parameters)
        {
            return new LambdaBinder().InternalBind(lambda, x => x, parameters);
        }

        /// <summary>
        /// Substitutes the parameters of the lambda with those specified.  Correlating the lambda
        /// parameters with the provided parameters is positional, so they need to have the same
        /// number of elements in addition to being located in the proper slot so that they
        /// end up representing the same parameter.  This version returns the associated
        /// expression that should be associated with the lambdas body.
        /// </summary>
        /// <param name="lambda">The lambda expression to bind to</param>
        /// <param name="parameters">The parameters that should be used as substitutions for the
        /// parameters referenced by the original lambda body</param>
        /// <returns>A new version of the lambda body with the specified substitutions</returns>
        public static Expression BindBody(LambdaExpression lambda, params Expression[] parameters)
        {
            return new LambdaBinder().InternalBind(lambda, x => x.Body, parameters);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return Substitute(node) ?? base.VisitParameter(node);
        }

        public override Expression Visit(Expression node)
        {
            return Substitute(node) ?? base.Visit(node);
        }

        private Expression Substitute(Expression node)
        {
            if (targetParametersSet.Contains(node))
                return replacementParameterByTargetParameter[node];
            else
                return null;
        }
    }
}