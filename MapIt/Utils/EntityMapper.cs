using System;
using System.Linq.Expressions;

namespace MapIt.Utils
{
    /// <summary>
    /// Contains both the expression tree form and the compiled functional form of the mapping.
    /// </summary>
    public class EntityMapper<TDbEntity, TModelEntity> : IEntityMapper
    {
        /// <summary>
        /// The expression tree form for use with Queryable
        /// </summary>
        public Expression<Func<TDbEntity, TModelEntity>> Expression { get; }

        /// <summary>
        /// The functional form for use with Enumereable
        /// </summary>
        public Func<TDbEntity, TModelEntity> Function { get; }

        public EntityMapper(Expression<Func<TDbEntity, TModelEntity>> expression, Func<TDbEntity, TModelEntity> function)
        {
            Expression = expression;
            Function = function;
        }

        public static implicit operator Expression<Func<TDbEntity, TModelEntity>>(EntityMapper<TDbEntity, TModelEntity> mapper)
        {
            return mapper.Expression;
        }

        LambdaExpression IEntityMapper.Expression => Expression;
        Delegate IEntityMapper.Function => Function;
    }
}