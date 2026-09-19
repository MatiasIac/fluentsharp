using System.Reflection;
using System.Runtime.CompilerServices;
using FunctionalSharp.Operations;

if (args.Length != 2 || args[0] is not ("--write" or "--verify"))
    throw new ArgumentException("Usage: ApiSnapshot --write|--verify path/to/PUBLIC-API.txt");

var lines = new List<string> { "# FluentSharp 2 CLR API baseline (C# extension syntax and nullability also checked by the packaged consumer)." };
var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
foreach (var type in typeof(Condition).Assembly.ExportedTypes.OrderBy(type => type.FullName, StringComparer.Ordinal))
{
    // Extension marker types are compiler implementation details. Their public static accessors
    // on the containing extension class are included below, and consumers verify property syntax.
    if (type.IsDefined(typeof(CompilerGeneratedAttribute)) || type.Name.StartsWith('<')) continue;
    lines.Add($"type {type.FullName} [{type.Attributes}] : {type.BaseType}");
    foreach (var parameter in type.GetGenericArguments()) AddConstraint(parameter);
    foreach (var contract in type.GetInterfaces().OrderBy(value => value.ToString(), StringComparer.Ordinal)) lines.Add($"  implements {contract}");
    foreach (var member in type.GetMembers(flags).Where(member => member is not Type).OrderBy(member => member.ToString(), StringComparer.Ordinal))
    {
        var signature = $"  {member.MemberType} {member}";
        if (member is MethodBase method)
        {
            signature += $" [{method.Attributes}]";
            signature += " parameters(" + string.Join(", ", method.GetParameters().Select(parameter =>
                parameter.Name + (parameter.IsOptional ? " = " + (parameter.DefaultValue ?? "null") : ""))) + ")";
        }
        if (member is FieldInfo field) signature += $" [{field.Attributes}]" + (field.IsLiteral ? $" = {field.GetRawConstantValue()}" : "");
        lines.Add(signature);
        if (member is MethodInfo generic && generic.IsGenericMethodDefinition)
            foreach (var parameter in generic.GetGenericArguments()) AddConstraint(parameter);
    }
}
var snapshot = string.Join('\n', lines.Select(line => line.TrimEnd())) + "\n";
if (args[0] == "--write") File.WriteAllText(args[1], snapshot);
else if (File.ReadAllText(args[1]).Replace("\r\n", "\n") != snapshot)
    throw new InvalidOperationException("The public API changed. Review the difference and deliberately regenerate the baseline.");
Console.WriteLine($"API baseline {args[0][2..]} passed ({lines.Count} lines).");

void AddConstraint(Type parameter)
{
    lines.Add($"    generic {parameter.Name} [{parameter.GenericParameterAttributes}] " +
        string.Join(", ", parameter.GetGenericParameterConstraints().Select(value => value.ToString())));
}
