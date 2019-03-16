using System;
using System.Linq.Expressions;

namespace MapIt.Utils
{
    public interface IEntityMapper
    {
        LambdaExpression Expression { get; }
        Delegate Function { get; }
    }
}