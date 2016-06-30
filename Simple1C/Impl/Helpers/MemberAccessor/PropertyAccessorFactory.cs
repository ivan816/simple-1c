using System.Reflection;
using System.Reflection.Emit;

namespace Simple1C.Impl.Helpers.MemberAccessor
{
	public class PropertyAccessorFactory<TOutput>: MemberAccessorFactory<TOutput>
	{
		private readonly PropertyInfo propertyInfo;

		public PropertyAccessorFactory(PropertyInfo propertyInfo): base(propertyInfo)
		{
			this.propertyInfo = propertyInfo;
		}

		protected override bool TryEmitSet(ILGenerator ilGenerator)
		{
			var setter = propertyInfo.GetSetMethod(true);
			if (setter == null)
				return false;
			if (!propertyInfo.IsStatic())
				ilGenerator.Emit(OpCodes.Ldarg_0);
			ilGenerator.Emit(OpCodes.Ldarg_1);
			EmitUnboxingCast(propertyInfo.PropertyType, ilGenerator);
			ilGenerator.Emit(OpCodes.Call, setter);
			ilGenerator.Emit(OpCodes.Ret);
			return true;
		}

		protected override bool TryEmitGet(ILGenerator ilGenerator)
		{
			var getter = propertyInfo.GetGetMethod(true);
			if (getter == null)
				return false;
			EmitLoadTarget(ilGenerator, propertyInfo);
			ilGenerator.Emit(OpCodes.Call, getter);
			EmitBoxingCast(propertyInfo.PropertyType, ilGenerator);
			ilGenerator.Emit(OpCodes.Ret);
			return true;
		}
	}
}