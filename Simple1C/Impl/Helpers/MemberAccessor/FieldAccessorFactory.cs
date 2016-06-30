using System.Reflection;
using System.Reflection.Emit;

namespace Simple1C.Impl.Helpers.MemberAccessor
{
	public class FieldAccessorFactory<TOutput>: MemberAccessorFactory<TOutput>
	{
		private readonly FieldInfo fieldInfo;

		public FieldAccessorFactory(FieldInfo fieldInfo): base(fieldInfo)
		{
			this.fieldInfo = fieldInfo;
		}

		protected override bool TryEmitSet(ILGenerator ilGenerator)
		{
			if (!fieldInfo.IsStatic)
				ilGenerator.Emit(OpCodes.Ldarg_0);
			ilGenerator.Emit(OpCodes.Ldarg_1);
			EmitUnboxingCast(fieldInfo.FieldType, ilGenerator);
			ilGenerator.Emit(fieldInfo.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, fieldInfo);
			ilGenerator.Emit(OpCodes.Ret);
			return true;
		}

		protected override bool TryEmitGet(ILGenerator ilGenerator)
		{
			EmitLoadTarget(ilGenerator, fieldInfo);
			ilGenerator.Emit(fieldInfo.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, fieldInfo);
			EmitBoxingCast(fieldInfo.FieldType, ilGenerator);
			ilGenerator.Emit(OpCodes.Ret);
			return true;
		}
	}
}