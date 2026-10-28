using System.Reflection;

namespace NotasMax.Tests.TestSupport;

public static class ViewTestExtensions
{
    private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    public static T GetFieldValue<T>(this object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, InstanceMembers)
            ?? throw new MissingFieldException(instance.GetType().FullName, fieldName);
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
