using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace QueryFlow.Expressions.Internal;

/// <summary>
/// Emits and caches lightweight runtime classes with public auto-properties matching a
/// requested field selection. Because these are real CLR types (not dictionaries), EF Core and
/// other LINQ providers can translate a <c>MemberInit</c> projection against them into a SQL
/// query that only selects the requested columns — the same mechanism that makes anonymous-type
/// projections push down to SQL.
/// </summary>
internal static class DynamicTypeBuilder
{
    private static readonly ModuleBuilder Module;
    private static readonly ConcurrentDictionary<string, Type> Cache = new();
    private static int _typeCounter;

    static DynamicTypeBuilder()
    {
        var assemblyName = new AssemblyName("QueryFlow.DynamicTypes");
        var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        Module = assembly.DefineDynamicModule("QueryFlow.DynamicTypes.Module");
    }

    public static Type GetOrCreate(IReadOnlyList<(string Name, Type Type)> members)
    {
        var key = string.Join('|', members.Select(m => m.Name + ":" + m.Type.FullName));
        return Cache.GetOrAdd(key, _ => Emit(members));
    }

    private static Type Emit(IReadOnlyList<(string Name, Type Type)> members)
    {
        var typeName = $"QueryFlowProjection_{Interlocked.Increment(ref _typeCounter)}";
        var typeBuilder = Module.DefineType(
            typeName,
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);

        foreach (var (name, type) in members)
        {
            var field = typeBuilder.DefineField($"_{name}", type, FieldAttributes.Private);

            var property = typeBuilder.DefineProperty(name, PropertyAttributes.None, type, null);

            var getter = typeBuilder.DefineMethod(
                $"get_{name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                type,
                Type.EmptyTypes);
            var getterIl = getter.GetILGenerator();
            getterIl.Emit(OpCodes.Ldarg_0);
            getterIl.Emit(OpCodes.Ldfld, field);
            getterIl.Emit(OpCodes.Ret);

            var setter = typeBuilder.DefineMethod(
                $"set_{name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                null,
                [type]);
            var setterIl = setter.GetILGenerator();
            setterIl.Emit(OpCodes.Ldarg_0);
            setterIl.Emit(OpCodes.Ldarg_1);
            setterIl.Emit(OpCodes.Stfld, field);
            setterIl.Emit(OpCodes.Ret);

            property.SetGetMethod(getter);
            property.SetSetMethod(setter);
        }

        return typeBuilder.CreateType();
    }
}
