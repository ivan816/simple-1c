using System;

namespace Simple1C.Impl.Helpers.MemberAccessor
{
	public interface IMemberAccessor: IAccessMember
	{
		bool CanGet { get; }
		bool CanSet { get; }
		Type MemberType { get; }
	}
}