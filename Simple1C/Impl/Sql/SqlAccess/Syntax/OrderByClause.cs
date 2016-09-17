using System.Collections.Generic;
using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
	internal class OrderByClause : ISqlElement
	{
		public List<OrderingElement> Expressions { get; set; }

		public ISqlElement Accept(SqlVisitor visitor)
		{
			return visitor.VisitOrderBy(this);
		}

		public class OrderingElement : ISqlElement
		{
			public ISqlElement Expression { get; set; }
			public bool IsAsc { get; set; }

			public ISqlElement Accept(SqlVisitor visitor)
			{
				return visitor.VisitOrderingElement(this);
			}
		}
	}
}