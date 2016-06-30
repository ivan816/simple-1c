namespace Simple1C.Impl.Helpers.MemberAccessor
{
	public interface IAccessMember
	{
		void Set(object entity, object value);
		object Get(object entity);
	}
}