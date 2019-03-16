using System;
using System.Linq.Expressions;

namespace MapIt.Utils
{
    public class EntityMapper<TDbEntity, TModelEntity> : IEntityMapper
    {
        public Expression<Func<TDbEntity, TModelEntity>> Expression { get; }
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