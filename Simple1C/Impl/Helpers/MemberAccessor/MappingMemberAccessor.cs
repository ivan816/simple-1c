namespace Simple1C.Impl.Helpers.MemberAccessor
{
	internal class MappingMemberAccessor: IAccessMember
	{
		private readonly IAccessMember parent;
		private readonly IAccessMember child;

		public MappingMemberAccessor(IAccessMember parent, IAccessMember child)
		{
			this.parent = parent;
			this.child = child;
		}

		public void Set(object entity, object value)
		{
			var parentValue = parent.Get(entity);
			if (parentValue != null)
				child.Set(parentValue, value);
		}

		public object Get(object entity)
		{
			var parentValue = parent.Get(entity);
			return parentValue == null ? null : child.Get(parentValue);
		}
	}
}