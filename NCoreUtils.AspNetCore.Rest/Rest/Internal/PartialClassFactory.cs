using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace NCoreUtils.AspNetCore.Rest.Internal
{
    public static class PartialClassFactory
    {
        private sealed class SequenceEqualityComparer : IEqualityComparer<IReadOnlyList<string>>
        {
            public bool Equals(IReadOnlyList<string>? x, IReadOnlyList<string>? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }
                if (x is null || y is null)
                {
                    return false;
                }
                return x.SequenceEqual(y, StringComparer.InvariantCulture);
            }

            public int GetHashCode(IReadOnlyList<string> obj)
            {
                if (obj is null)
                {
                    return default;
                }
                var hash = new HashCode();
                foreach (var item in obj)
                {
                    hash.Add(StringComparer.InvariantCulture.GetHashCode(item));
                }
                return hash.ToHashCode();
            }
        }

        private static readonly SequenceEqualityComparer _eq = new SequenceEqualityComparer();

        private static readonly Dictionary<Type, Dictionary<IReadOnlyList<string>, PartialClassInfo>> _cache
            = new Dictionary<Type, Dictionary<IReadOnlyList<string>, PartialClassInfo>>();

        private static readonly ConstructorInfo _partialDataCtor = typeof(PartialDataAttribute)
            .GetConstructors()
            .First(e => e.GetParameters().Length == 2);

        private static AssemblyBuilder? _assemblyBuilder;

        private static ModuleBuilder? _moduleBuilder;

        private static CustomAttributeBuilder CreatePartialDataAttribute(Type type, string[] fields)
            => new CustomAttributeBuilder(_partialDataCtor, new object[] { type, fields });

        private static PropertyInfo EmitProperty(TypeBuilder typeBuilder, PropertyInfo sourceProperty, out FieldInfo field)
        {
            var propertyName = sourceProperty.Name!;
            var propertyType = sourceProperty.PropertyType;
            field = typeBuilder.DefineField($"<{propertyName}>k__BackingField", propertyType, FieldAttributes.Private | FieldAttributes.InitOnly);
            var getter = typeBuilder.DefineMethod(
                "get_" + propertyName,
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                propertyType,
                Type.EmptyTypes
            );
            {
                var il = getter.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Ret);
            }
            // var setter = typeBuilder.DefineMethod(
            //     "set_" + propertyName,
            //     MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            //     typeof(void),
            //     new [] { propertyType }
            // );
            // {
            //     var il = setter.GetILGenerator();
            //     il.Emit(OpCodes.Ldarg_0);
            //     il.Emit(OpCodes.Ldarg_1);
            //     il.Emit(OpCodes.Stfld, field);
            //     il.Emit(OpCodes.Ret);
            // }
            var property = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            property.SetGetMethod(getter);
            // property.SetSetMethod(setter);
            // property.SetCustomAttribute(CreateJsonPropertyNameAttribute(property.Path));
            return property;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Only public properties are used.")]
        private static Type EmitPartial(
            TypeBuilder typeBuilder,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type sourceType,
            IReadOnlyList<string> fieldSelector)
        {
            // fields and properties
            var pfs = new List<(PropertyInfo prop, FieldInfo field)>(fieldSelector.Count);
            foreach (var name in fieldSelector)
            {
                var prop = sourceType.GetProperty(name!, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);
                if (prop is null)
                {
                    continue;
                }
                EmitProperty(typeBuilder, prop, out var field);
                pfs.Add((prop, field));
            }
            // ctor
            var parameters = pfs.MapToArray(pf => pf.prop.PropertyType);
            var ctor = typeBuilder.DefineConstructor(
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                callingConvention: CallingConventions.Standard,
                parameters
            );
            for (var i = 0; i < pfs.Count; ++i)
            {
                ctor.DefineParameter(i + 1, ParameterAttributes.None, "p_" + pfs[i].prop.Name);
            }
            {
                var il = ctor.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
                for (var i = 0; i < pfs.Count; ++i)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg, i + 1);
                    il.Emit(OpCodes.Stfld, pfs[i].field);
                }
                il.Emit(OpCodes.Ret);
            }
            return typeBuilder.CreateType()!;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dynamically emitted members cannot be trimmed.")]
        [UnconditionalSuppressMessage("Trimming", "IL2073", Justification = "Dynamically emitted members cannot be trimmed.")]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        private static Type DoCreatePartialClass(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type sourceType,
            IReadOnlyList<string> fieldSelector)
        {
            if (_assemblyBuilder is null)
            {
                _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                    new AssemblyName("NCoreUtils.AspNetCore.Rest.Partial"),
                    AssemblyBuilderAccess.Run
                );
            }
            if (_moduleBuilder is null)
            {
                _moduleBuilder = _assemblyBuilder.DefineDynamicModule("NCoreUtils.AspNetCore.Rest.Partial");
            }
            var partialTypeName = $"{sourceType.Name}_Partial_{string.Join(string.Empty, fieldSelector.Select(f => f[0]))}_{_eq.GetHashCode(fieldSelector)}";
            Type? type = _assemblyBuilder
                .GetTypes()
                .FirstOrDefault(t => t.Name == partialTypeName);
            if (type is null)
            {
                var typeBuilder = _moduleBuilder.DefineType(
                    partialTypeName,
                    TypeAttributes.Public | TypeAttributes.AnsiClass | TypeAttributes.AutoClass | TypeAttributes.BeforeFieldInit
                );
                typeBuilder.SetCustomAttribute(CreatePartialDataAttribute(sourceType, fieldSelector.ToArray()));
                type = EmitPartial(typeBuilder, sourceType, fieldSelector);
            }
            return type;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dynamically emitted members cannot be trimmed.")]
        [UnconditionalSuppressMessage("Trimming", "IL2080", Justification = "Dynamically emitted members cannot be trimmed.")]
        internal static PartialClassInfo CreatePartialClass(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type sourceType,
            IReadOnlyList<string> fieldSelector)
        {
            lock (_cache)
            {
                if (!_cache.TryGetValue(sourceType, out var inner))
                {
                    inner = new Dictionary<IReadOnlyList<string>, PartialClassInfo>(_eq);
                    _cache.Add(sourceType, inner);
                }
                if (!inner.TryGetValue(fieldSelector, out var info))
                {
                    var ty = DoCreatePartialClass(sourceType, fieldSelector);
                    var ctor = ty.GetConstructors()[0];
                    var ctorArgs = ctor.GetParameters();
                    var eArg = Expression.Parameter(sourceType);
                    var selector = Expression.Lambda(
                        Expression.New(
                            ctor,
                            ctorArgs.Select(ToParameter),
                            ctorArgs.Select(ToMember)
                        ),
                        eArg
                    );
                    info = new PartialClassInfo(sourceType, fieldSelector, ty, selector);
                    inner.Add(fieldSelector, info);

                    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dynamically emitted members cannot be trimmed.")]
                    [UnconditionalSuppressMessage("Trimming", "IL2080", Justification = "Dynamically emitted members cannot be trimmed.")]
                    MemberInfo ToMember(ParameterInfo e) => ty!.GetProperty(e.Name!.Substring(2), BindingFlags.Public | BindingFlags.Instance)!;

                    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dynamically emitted members cannot be trimmed.")]
                    MemberExpression ToParameter(ParameterInfo e) => Expression.Property(eArg!, e.Name!.Substring(2));
                }
                return info;
            }
        }
    }
}