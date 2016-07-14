using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Simple1C.Impl.Helpers.MemberAccessor
{
	internal class MemberAccessor<TOutput>: IMemberAccessor
	{
		private readonly Func<object, TOutput> getter;
		private readonly Action<object, TOutput> setter;

		public MemberAccessor(Func<object, TOutput> getter, Action<object, TOutput> setter, Type memberType)
		{
			this.getter = getter;
			this.setter = setter;
			MemberType = memberType;
		}

		public void Set(object target, TOutput value)
		{
			if (setter == null)
				throw new InvalidOperationException();
			setter(target, value);
		}

		public void Set(object target, object value)
		{
			Set(target, (TOutput) value);
		}

		object IAccessMember.Get(object entity)
		{
			return Get(entity);
		}

		public bool CanGet
		{
			get { return getter != null; }
		}

		public bool CanSet
		{
			get { return setter != null; }
		}

		public Type MemberType { get; private set; }

		public TOutput Get(object target)
		{
			if (getter == null)
				throw new InvalidOperationException();
			return getter(target);
		}

		private static readonly ConcurrentDictionary<MemberInfo, MemberAccessor<TOutput>> cache =
			new ConcurrentDictionary<MemberInfo, MemberAccessor<TOutput>>();

		public static MemberAccessor<TOutput> Get(MemberInfo memberInfo)
		{
			return cache.GetOrAdd(memberInfo, m => MemberAccessorFactory<TOutput>.Create(ToDeclaring(m)).CreateAccessor());
		}

		private static MemberInfo ToDeclaring(MemberInfo memberInfo)
		{
			return memberInfo.DeclaringType
							 .GetMember(memberInfo.Name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
							 .Single(x => x.DeclaringType == memberInfo.DeclaringType && x.MemberType == memberInfo.MemberType);
		}
	}
}