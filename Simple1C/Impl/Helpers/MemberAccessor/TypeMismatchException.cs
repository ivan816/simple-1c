using System;

namespace Simple1C.Impl.Helpers.MemberAccessor
{
	internal class TypeMismatchException : Exception
	{
		public TypeMismatchException(Type first, Type second)
			: base(string.Format("Типы '{0}' и '{1}' несовместимы", first.FullName, second.FullName))
		{
		}
	}
}