using System;
using System.Reflection.Emit;

namespace Simple1C.Impl.Helpers.MemberAccessor
{
	public abstract class Caster
	{
		protected readonly Type memberType;
		protected readonly Type outputType;

		protected Caster(Type outputType, Type memberType)
		{
			this.outputType = outputType;
			this.memberType = memberType;
		}

		protected abstract void EmitNullableCast(ILGenerator ilGenerator, Type nullableType);

		protected abstract void EmitValueTypeCast(ILGenerator ilGenerator);

		public void EmitCast(ILGenerator ilGenerator)
		{
			if (outputType == memberType)
				return;

			if (!outputType.IsAssignableFrom(memberType))
				throw new TypeMismatchException(outputType, memberType);

			if (memberType.IsValueType && !outputType.IsValueType)
				EmitValueTypeCast(ilGenerator);

			if (outputType.IsNullableOf(memberType))
				EmitNullableCast(ilGenerator, outputType);
		}
	}
}