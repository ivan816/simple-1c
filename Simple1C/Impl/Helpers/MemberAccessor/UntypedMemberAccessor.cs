using System.Reflection;

namespace Simple1C.Impl.Helpers.MemberAccessor
{
	internal class UntypedMemberAccessor
	{
		public static IMemberAccessor Create(MemberInfo memberInfo)
		{
			return MemberAccessor<object>.Get(memberInfo);
		}
	}
}