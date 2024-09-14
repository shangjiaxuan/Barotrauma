using FarseerPhysics.Collision.Shapes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LuaDocsGenerator
{
    public class DocsGenerator
    {
        private static HashSet<string> removed = new HashSet<string>();

        public static string NormalizeGenericTypeName(string s)
        {
            var idx = s.LastIndexOf('`');

            if (idx != -1)
            {
                return s[..idx];
            }

            return s;
        }

        public static string TypeToString(Type type, bool useLuaTypes = true)
        {
            // Return "T" for unresolved type params
            if (type.IsGenericParameter)
            {
                return type.Name;
            }

            var genericType = type.IsGenericType
              ? type.GetGenericTypeDefinition()
              : null;

            if (type == typeof(bool))
            {
                return "bool";
            }

            if (type == typeof(string))
            {
                return "string";
            }

            if (useLuaTypes)
            {
                if (type == typeof(sbyte)
                  || type == typeof(byte)
                  || type == typeof(short)
                  || type == typeof(ushort)
                  || type == typeof(int)
                  || type == typeof(uint)
                  || type == typeof(long)
                  || type == typeof(ulong)
                  || type == typeof(float)
                  || type == typeof(double))
                {
                    return "number";
                }

                if (genericType == typeof(List<>)
                  || genericType == typeof(Dictionary<,>))
                {
                    return "table";
                }

                if (genericType == typeof(Action<,>)
                  || genericType == typeof(Func<,>))
                {
                    return "function";
                }
            }

            var nsToRemove = new[] {
                "Barotrauma",
                "System",
                "System.Collections",
                "System.Collections.Generic",
            };

            string Namespaced(string typeName)
            {
                if (type.Namespace == null)
                {
                    return typeName;
                }

                // Full namespace match
                if (nsToRemove.Contains(type.Namespace))
                {
                    return typeName;
                }

                // Partial namespace match
                foreach (var ns in nsToRemove)
                {
                    if (ns == type.Namespace)
                    {
                        return typeName;
                    }

                    if (type.Namespace.StartsWith(ns + "."))
                    {
                        var shortNs = type.Namespace.Remove(0, ns.Length + 1);
                        return $"{shortNs}.{typeName}";
                    }
                }

                return $"{type.Namespace}.{typeName}";
            }

            string Impl(string? ns)
            {
                if (type.IsGenericType)
                {
                    var genericTypeDef = type.GetGenericTypeDefinition();
                    var genericTypeName = NormalizeGenericTypeName(genericTypeDef.Name);

                    var genericArgs = type.GetGenericArguments();

                    // Use the `T?` notation instead of Nullable<T>
                    if (genericTypeDef == typeof(Nullable<>))
                    {
                        // ldoc supports the "?string" notation, which expands to "?|nil|string"
                        if (useLuaTypes)
                        {
                            return Namespaced("?" + TypeToString(genericArgs[0], useLuaTypes: false));
                        }
                        else
                        {
                            return Namespaced(TypeToString(genericArgs[0], useLuaTypes: false) + "?");
                        }
                    }

                    var sb = new StringBuilder();
                    sb.Append(genericTypeName);
                    sb.Append("<");
                    foreach (var genericArgType in genericArgs)
                    {
                        sb.Append(TypeToString(genericArgType, useLuaTypes: false));
                        sb.Append(",");
                    }
                    // Remove the last separator
                    sb.Length--;
                    sb.Append(">");

                    return Namespaced(sb.ToString());
                }

                return Namespaced(type.Name);
            }

            return Impl(type.Namespace);
        }

        public static (bool Success, string Output, string Error) TryRunGitCommand(string args)
        {
            static string? GetGitBinary()
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process)
                        ?.Split(';')
                        .Select(x => Path.Join(x, "git.exe"))
                        .FirstOrDefault(File.Exists);
                }
                else
                {
                    return Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process)
                        ?.Split(':')
                        .Select(x => Path.Join(x, "git"))
                        .FirstOrDefault(File.Exists);
                }
            }

            var gitBinary = GetGitBinary();
            if (gitBinary == null)
            {
                throw new InvalidOperationException("Failed to find git binary in PATH");
            }

            using var process = Process.Start(new ProcessStartInfo(gitBinary, args)
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            });

            if (process == null)
                throw new InvalidOperationException($"Failed to run git command: {args}");

            process.Start();

            var stdOut = process.StandardOutput.ReadToEndAsync();
            var stdErr = process.StandardError.ReadToEndAsync();
            Task.WhenAll(stdOut, stdErr).GetAwaiter().GetResult();
            process.WaitForExit();

            return (process.ExitCode == 0, stdOut.Result.TrimEnd('\r', '\n'), stdErr.Result);
        }

        private static string EscapeName(string n)
        {
            return n switch
            {
                "end" => "endparam",
                _ => n
            };
        }

        private static string? ConvertAnnotation(Type type, MemberInfo member, string realm)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Constructor:
                    return ConvertAnnotation(type, (ConstructorInfo)member, realm);
                case MemberTypes.Method:
                    return ConvertAnnotation(type, (MethodInfo)member, realm);
                case MemberTypes.Field:
                    return ConvertAnnotation(type, (FieldInfo)member, realm);
                case MemberTypes.Property:
                    return ConvertAnnotation(type, (PropertyInfo)member, realm);
            }

            return null;
        }

        private static string? ConvertAnnotation(Type type, ConstructorInfo method, string realm)
        {
            StringBuilder sb = new StringBuilder();

            var paramNames = new StringBuilder();
            foreach (var parameter in method.GetParameters())
            {
                paramNames.Append(EscapeName(parameter.Name!));
                paramNames.Append(", ");
            }
            if (paramNames.Length > 0)
            {
                // Remove the last separator
                paramNames.Length -= 2;
            }

            string functionDecoration = $"function {type.Name}({paramNames}) end";


            if (removed.Contains("-- @remove " + functionDecoration))
            {
                Console.WriteLine($"removed {functionDecoration}");
                return null;
            }

            Console.WriteLine($"  - CONSTRUCTOR: {method}");

            sb.AppendLine($"--- {type.Name}");
            sb.AppendLine($"-- @realm {realm}");

            foreach (var parameter in method.GetParameters())
            {
                sb.AppendLine($"-- @tparam {TypeToString(parameter.ParameterType)} {EscapeName(parameter.Name!)}");
            }

            sb.AppendLine(functionDecoration);

            return sb.ToString();
        }

        private static string? ConvertAnnotation(Type type, MethodInfo method, string realm)
        {
            StringBuilder sb = new StringBuilder();

            // Exclude property getters/setters
            if (method.IsSpecialName)
            {
                return null;
            }

            var paramNames = new StringBuilder();
            foreach (var parameter in method.GetParameters())
            {
                paramNames.Append(EscapeName(parameter.Name!));
                paramNames.Append(", ");
            }
            if (paramNames.Length > 0)
            {
                // Remove the last separator
                paramNames.Length -= 2;
            }

            string functionDecoration;
            if (method.IsStatic)
            {
                functionDecoration = $"function {type.Name}.{method.Name}({paramNames}) end";
            }
            else
            {
                functionDecoration = $"function {method.Name}({paramNames}) end";
            }

            if (removed.Contains("-- @remove " + functionDecoration))
            {
                Console.WriteLine($"removed {functionDecoration}");
                return null;
            }

            Console.WriteLine($"  - METHOD: {method}");

            sb.AppendLine($"--- {method.Name}");
            sb.AppendLine($"-- @realm {realm}");

            foreach (var parameter in method.GetParameters())
            {
                sb.AppendLine($"-- @tparam {TypeToString(parameter.ParameterType)} {EscapeName(parameter.Name!)}");
            }

            if (method.ReturnType != typeof(void))
            {
                sb.AppendLine($"-- @treturn {TypeToString(method.ReturnType)}");
            }

            sb.AppendLine(functionDecoration);

            return sb.ToString();
        }

        private static string? ConvertAnnotation(Type type, FieldInfo field, string realm)
        {
            StringBuilder sb = new StringBuilder();

            var name = EscapeName(field.Name);
            var returnName = TypeToString(field.FieldType);

            if (field.IsStatic)
            {
                name = type.Name + "." + field.Name;
            }

            if (removed.Contains("-- @remove " + name))
            {
                Console.WriteLine($"removed {name}");
                return null;
            }

            Console.WriteLine($"  - FIELD: {name}");

            sb.AppendLine("---");
            sb.Append("-- ");
            sb.Append(name);
            sb.AppendLine($", field of type {returnName}");
            sb.AppendLine($"-- @realm {realm}");
            sb.AppendLine($"-- @field {name}");

            return sb.ToString();
        }

        public static string? ConvertAnnotation(Type type, PropertyInfo property, string realm)
        {
            StringBuilder sb = new StringBuilder();

            var name = EscapeName(property.Name);
            var returnName = TypeToString(property.PropertyType);

            if (property.GetGetMethod()?.IsStatic == true || property.GetSetMethod()?.IsStatic == true)
            {
                name = type.Name + "." + property.Name;
            }

            if (removed.Contains("-- @remove " + name))
            {
                Console.WriteLine($"removed {name}");
                return null;
            }

            Console.WriteLine($"  - PROPERTY: {name}");

            sb.AppendLine("---");
            sb.Append("-- ");
            sb.Append(name);
            sb.AppendLine($", field of type {returnName}");
            sb.AppendLine($"-- @realm {realm}");
            sb.AppendLine($"-- @field {name}");

            return sb.ToString();
        }

        private static string? GetSignature(MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Method:
                    return GetSignature((MethodInfo)member);
                case MemberTypes.Field:
                    return GetSignature((FieldInfo)member);
                case MemberTypes.Property:
                    return GetSignature((PropertyInfo)member);
            }

            return null;
        }

        private static string GetSignature(FieldInfo field)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(field.FieldType.Name);
            sb.Append(" ");
            sb.Append(field.Name);

            return sb.ToString();
        }

        private static string GetSignature(PropertyInfo property)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(property.PropertyType.Name);
            sb.Append(" ");
            sb.Append(property.Name);

            return sb.ToString();
        }

        private static string GetSignature(MethodInfo method)
        {
            StringBuilder sb = new StringBuilder();
            bool firstParam = true;

            sb.Append(method.ReturnType.Name);
            sb.Append(' ');
            sb.Append(method.Name);

            // Add method generics
            if (method.IsGenericMethod)
            {
                sb.Append("<");
                foreach (var g in method.GetGenericArguments())
                {
                    if (firstParam)
                    {
                        firstParam = false;
                    }
                    else
                    {
                        sb.Append(", ");
                    }
                    sb.Append(g.Name);
                }
                sb.Append(">");
            }
            sb.Append("(");
            firstParam = true;
            foreach (var param in method.GetParameters())
            {
                if (firstParam)
                {
                    firstParam = false;
                    if (method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
                    {
                        sb.Append("this ");
                    }
                }

                sb.Append(", ");

                if (param.ParameterType.IsByRef)
                {
                    sb.Append("ref ");
                }
                else if (param.IsOut)
                {
                    sb.Append("out ");
                }

                sb.Append(param.ParameterType.Name);
                sb.Append(' ');

                sb.Append(param.Name);
            }
            sb.Append(")");
            return sb.ToString();
        }

        private static string GenerateBaseDoc(Type type, string categoryName, string baseFile)
        {
            string baseLuaText;
            try
            {
                baseLuaText = File.ReadAllText(baseFile);
            }
            catch (FileNotFoundException)
            {
                baseLuaText = @$"-- luacheck: ignore 111

--[[--
{type.FullName}
]]
-- @code {categoryName}
-- @pragma nostrip
local {type.Name} = {{}}".ReplaceLineEndings("\n");

                File.WriteAllText(baseFile, baseLuaText);
            }

            removed = new HashSet<string>();
            var removeTagPattern = new Regex("^-- @remove (.*)$", RegexOptions.Multiline);
            var matches = removeTagPattern.Matches(baseLuaText);

            foreach (var match in matches.Cast<Match>())
            {
                removed.Add(match.Value);
            }

            return baseLuaText;
        }

        public static void GenerateDocs(Type type, string baseFile, string outFile, string? categoryName = null, string realm = "shared")
        {
            categoryName ??= type.Name;
            var sb = new StringBuilder();

            Console.WriteLine($"Generating docs for {type}");

            string baseDoc = GenerateBaseDoc(type, categoryName, baseFile);

            sb.Append(baseDoc);
            sb.AppendLine();
            sb.AppendLine();

            var members = type.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            foreach (var member in members)
            {
                switch (member.MemberType)
                {
                    case MemberTypes.Constructor:
                    {
                        sb.Append(ConvertAnnotation(type, (ConstructorInfo)member, realm));
                        sb.AppendLine();
                        break;
                    }

                    case MemberTypes.Method:
                    {
                        sb.Append(ConvertAnnotation(type, (MethodInfo)member, realm));
                        sb.AppendLine();
                        break;
                    }

                    case MemberTypes.Field:
                    {
                        sb.Append(ConvertAnnotation(type, (FieldInfo)member, realm));
                        sb.AppendLine();
                        break;
                    }

                    case MemberTypes.Property:
                    {
                        sb.Append(ConvertAnnotation(type, (PropertyInfo)member, realm));
                        sb.AppendLine();
                        break;
                    }
                }
            }

            new FileInfo(outFile).Directory.Create();
            File.WriteAllText(outFile, sb.ToString());
        }

        public static void GenerateEnum(Type enumType, string outFile, string categoryName = null, string realm = "shared")
        {
            StringBuilder sb = new StringBuilder();

            categoryName = categoryName ?? enumType.Name;

            sb.AppendLine($@"--[[--
{enumType.Name} enum.
]]
-- @enum {categoryName}");

            sb.AppendLine();

            FieldInfo[] fields = enumType.GetFields();

            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].Name.Equals("value__")) { continue; }

                sb.AppendLine("---");
                sb.AppendLine($"-- {categoryName}.{fields[i].Name} = {fields[i].GetRawConstantValue()}");
                sb.AppendLine($"-- @realm {realm}");
                sb.AppendLine($"-- @number {categoryName}.{fields[i].Name}");
                sb.AppendLine();
            }

            new FileInfo(outFile).Directory.Create();
            File.WriteAllText(outFile, sb.ToString());
        }

        public static void GenerateDocs(Type clientType, Type serverType, string baseFile, string outFile, string? categoryName = null)
        {
            categoryName ??= clientType.Name;
            var sb = new StringBuilder();

            Console.WriteLine($"Generating docs for {clientType} and {serverType}");

            sb.Append(GenerateBaseDoc(clientType, categoryName, baseFile));
            sb.AppendLine();
            sb.AppendLine();

            List<(string?, MemberInfo)> clientTypes = new List<(string?, MemberInfo)>();
            List<(string?, MemberInfo)> serverTypes = new List<(string?, MemberInfo)>();

            var clientMembers = clientType.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            var serverMembers = serverType.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

            foreach (var member in clientMembers)
            {
                clientTypes.Add((GetSignature(member), member));
            }

            foreach (var member in serverMembers)
            {
                serverTypes.Add((GetSignature(member), member));
            }

            var ids = clientTypes.Select(x => x.Item1).Intersect(serverTypes.Select(x => x.Item1));
            List<(string?, MemberInfo)> sharedTypes = clientTypes.Where(x => ids.Contains(x.Item1)).ToList();

            foreach (var type in sharedTypes)
            {
                string? result = ConvertAnnotation(clientType, type.Item2, "shared");
                if (result != null)
                {
                    sb.Append(result);
                    sb.AppendLine();
                }
            }

            foreach (var type in clientTypes)
            {
                string? result = ConvertAnnotation(clientType, type.Item2, "client");

                if (result != null && !sharedTypes.Select(x => x.Item1).Contains(type.Item1))
                {
                    sb.Append(result);
                    sb.AppendLine();
                }
            }

            foreach (var type in serverTypes)
            {
                string? result = ConvertAnnotation(clientType, type.Item2, "server");

                if (result != null && !sharedTypes.Select(x => x.Item1).Contains(type.Item1))
                {
                    sb.Append(result);
                    sb.AppendLine();
                }
            }

            new FileInfo(outFile).Directory.Create();
            File.WriteAllText(outFile, sb.ToString());
        }
    }
}
