using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MapIt.Utils
{
    public static class ExpressionTrees
    {
        /// <summary>
        /// Converts a function that takes one or more arguments and returns a function that takes fewer arguments.  The
        /// length of the `replacementParameters` array should be equal to the number of parameters defined for `func`.
        /// If an element in the array is non-null it will be substituted for the equivalent parameter in `func`.  If the
        /// element is null then it will be left alone and surface as a parameter (in logical order) in the return value.
        /// </summary>
        public static Expression PartialApplication(this LambdaExpression func, params Expression[] replacementParameters)
        {
            var originalParameters = func.Parameters;
            if (originalParameters.Count != replacementParameters.Length)
                throw new Exception("func (" + func + ") defines " + originalParameters.Count + " parameters, but there were " + replacementParameters.Length + " parameters in replacementParameters");

            var newParameters = new List<ParameterExpression>();
            var bindingParameters = new List<Expression>();
            for (var i = 0; i < replacementParameters.Length; i++)
            {
                var originalParameter = originalParameters[i];
                var replacementParameter = replacementParameters[i];
                if (replacementParameter == null)
                {
                    newParameters.Add(originalParameter);
                    bindingParameters.Add(originalParameter);
                }
                else
                {
                    bindingParameters.Add(replacementParameter);
                }
            }

            var newBody = LambdaBinder.BindBody(func, bindingParameters.ToArray());

            return newBody;
        }

        /// <summary>
        /// Remaps usages of Include references in the returned lambda expression.  This allows you
        /// to re-use mappers when the reference to another entity is one-to-one (whereas for
        /// collection types, you can just use the Select operator out of the box).
        /// </summary>
        public static EntityMapper<TDbEntity, TModelEntity> Compose<TDbEntity, TModelEntity>(Expression<Func<TDbEntity, TModelEntity>> mapper)
        {
            return new EntityMapper<TDbEntity, TModelEntity>(MapperComposer.Compose(mapper), mapper.Compile());
        }

        /// <summary>
        /// Marker function used by the Compose method to combine one-to-one mappings between entities.
        /// </summary>
        public static TModelEntity Include<TDbEntity, TModelEntity>(TDbEntity dbEntity, Expression<Func<TDbEntity, TModelEntity>> mapper)
            where TDbEntity : class
            where TModelEntity : class
        {
            return dbEntity == null ? null : mapper.Compile()(dbEntity);
        }

        /// <summary>
        /// Marker function used by the Compose method to combine one-to-one mappings between entities.
        /// </summary>
        public static TModelEntity Include<TDbEntity, TModelEntity>(TDbEntity dbEntity, EntityMapper<TDbEntity, TModelEntity> mapper)
            where TDbEntity : class
            where TModelEntity : class
        {
            return dbEntity == null ? null : mapper.Function(dbEntity);
        }
    }
}
