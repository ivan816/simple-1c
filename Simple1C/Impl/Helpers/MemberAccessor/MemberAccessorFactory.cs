using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Simple1C.Impl.Helpers.MemberAccessor
{
	public abstract class MemberAccessorFactory<TOutput>
	{
		private readonly MemberInfo member;

		protected MemberAccessorFactory(MemberInfo member)
		{
			this.member = member;
		}

		protected static void EmitBoxingCast(Type memberType, ILGenerator ilGenerator)
		{
			var caster = new BoxingCaster(typeof (TOutput), memberType);
			caster.EmitCast(ilGenerator);
		}

		protected static void EmitUnboxingCast(Type memberType, ILGenerator ilGenerator)
		{
			var caster = new UnboxingCaster(typeof (TOutput), memberType);
			caster.EmitCast(ilGenerator);
		}

		public static MemberAccessorFactory<TOutput> Create(MemberInfo memberInfo)
		{
			if (memberInfo is FieldInfo)
				return new FieldAccessorFactory<TOutput>(memberInfo as FieldInfo);
			if (memberInfo is PropertyInfo)
				return new PropertyAccessorFactory<TOutput>(memberInfo as PropertyInfo);
			throw new Exception("Invalid member info");
		}

		protected static void EmitLoadTarget(ILGenerator ilGenerator, MemberInfo member)
		{
			if (member.IsStatic())
				return;
			ilGenerator.Emit(OpCodes.Ldarg_0);
			var declaringType = member.DeclaringType;
			if (!declaringType.IsValueType)
				return;
			ilGenerator.Emit(OpCodes.Unbox_Any, declaringType);
			ilGenerator.DeclareLocal(declaringType);
			ilGenerator.Emit(OpCodes.Stloc_0);
			ilGenerator.Emit(OpCodes.Ldloca_S, 0);
		}

		public MemberAccessor<TOutput> CreateAccessor()
		{
			return new MemberAccessor<TOutput>(CreateGetter(), CreateSetter(), member.MemberType());
		}

		private Action<object, TOutput> CreateSetter()
		{
			var method = CreateSettingMethod();
			return TryEmitSet(method.GetILGenerator()) ? CreateSettingDelegate(method) : null;
		}

		private Func<object, TOutput> CreateGetter()
		{
			var method = CreateGettingMethod();
			return TryEmitGet(method.GetILGenerator()) ? CreateGettingDelegate(method) : null;
		}

		#region Protected interface

		protected abstract bool TryEmitSet(ILGenerator ilGenerator);
		protected abstract bool TryEmitGet(ILGenerator ilGenerator);

		#endregion

		#region Helpers

		private static Action<object, TOutput> CreateSettingDelegate(DynamicMethod dynamicMethod)
		{
			return (Action<object, TOutput>) dynamicMethod.CreateDelegate(typeof (Action<object, TOutput>));
		}

		private static Func<object, TOutput> CreateGettingDelegate(DynamicMethod dynamicMethod)
		{
			return (Func<object, TOutput>) dynamicMethod.CreateDelegate(typeof (Func<object, TOutput>));
		}

		private static DynamicMethod CreateSettingMethod()
		{
			return new DynamicMethod("",
									 null,
									 new[] { typeof (object), typeof (TOutput) },
									 typeof (MemberAccessor<TOutput>),
									 true);
		}

		private static DynamicMethod CreateGettingMethod()
		{
			return new DynamicMethod("",
									 typeof (TOutput),
									 new[] { typeof (object) },
									 typeof (MemberAccessor<TOutput>),
									 true);
		}

		#endregion
	}
}