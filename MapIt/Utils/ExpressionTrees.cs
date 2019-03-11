using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace MapIt.Utils
{
    public static class ExpressionTrees
    {
        /// <summary>
        /// Remaps usages of Include references in the returned lambda expression.  This allows you
        /// to re-use mappers when the reference to another entity is one-to-one (whereas for
        /// collection types, you can just use the Select operator out of the box).
        /// </summary>
        public static Expression<Func<TDbEntity, TModelEntity>> Compose<TDbEntity, TModelEntity>(Expression<Func<TDbEntity, TModelEntity>> mapper)
        {
            return MapperComposer.Compose(mapper);
        }

        /// <summary>
        /// Marker function used by the Compose method to combine one-to-one mappings between entities.
        /// </summary>
        public static TModelEntity Include<TDbParent, TDbEntity, TModelEntity>(this TDbParent parent, Expression<Func<TDbParent, TDbEntity>> dbEntity, Expression<Func<TDbEntity, TModelEntity>> mapper)
        {
            return default;
        }
    }
}
