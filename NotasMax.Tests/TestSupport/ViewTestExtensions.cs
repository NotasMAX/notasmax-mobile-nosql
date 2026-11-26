using System.Reflection;

namespace NotasMax.Tests.TestSupport;

public static class ViewTestExtensions
{
    private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    public static T GetFieldValue<T>(this object instance, string fieldName)
    {
        FieldInfo? field = null;
        for (var type = instance.GetType(); type is not null && field is null; type = type.BaseType)
            field = type.GetField(fieldName, InstanceMembers | BindingFlags.DeclaredOnly);

        if (field is null)
            throw new MissingFieldException(instance.GetType().FullName, fieldName);
        return (T)field.GetValue(instance)!;
    }

    public static object? InvokeMember(this object instance, string memberName, params object?[] arguments)
    {
        var method = instance.GetType().GetMethods(InstanceMembers)
            .FirstOrDefault(candidate => candidate.Name == memberName
                && candidate.DeclaringType == instance.GetType()
                && candidate.GetParameters().Length == arguments.Length)
            ?? throw new MissingMethodException(instance.GetType().FullName, memberName);
        return method.Invoke(instance, arguments);
    }
}
