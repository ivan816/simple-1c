using System;
using System.Reflection.Emit;

namespace Simple1C.Impl.Helpers.MemberAccessor
{
	internal class UnboxingCaster: Caster
	{
		public UnboxingCaster(Type outputType, Type memberType): base(outputType, memberType)
		{
		}

		protected override void EmitNullableCast(ILGenerator ilGenerator, Type nullableType)
		{
			ilGenerator.DeclareLocal(outputType);
			ilGenerator.Emit(OpCodes.Stloc_0);
			ilGenerator.Emit(OpCodes.Ldloca_S, 0);
			ilGenerator.Emit(OpCodes.Call, nullableType.GetProperty("Value").GetGetMethod());
		}

		protected override void EmitValueTypeCast(ILGenerator ilGenerator)
		{
			ilGenerator.Emit(OpCodes.Unbox_Any, memberType);
		}
	}
}