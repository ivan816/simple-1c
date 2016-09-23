using System;
using Simple1C.Impl.Sql.SqlAccess.Syntax;
using Simple1C.Impl.Sql.Translation;

internal class GenericVisitor<T> : SqlVisitor
    where T: class, ISqlElement
{
    private readonly Func<T, T> visit;

    public GenericVisitor(Func<T, T> visit)
    {
        this.visit = visit;
    }

    public override ISqlElement Visit(ISqlElement element)
    {
        var sqlElement = base.Visit(element);
        var typedElement = element as T;
        if (typedElement != null)
            return visit(typedElement);
        return sqlElement;
    }
}