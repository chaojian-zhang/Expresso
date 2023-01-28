using System.Reflection;
using System.Xml.Linq;

namespace NamedFunctionCall
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Current loaded assemblies: ");
            foreach (var name in AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(a => a.GetName())
                .OrderBy(n => n.Name))
                Console.WriteLine($"{name.Name}: {name.FullName}");

            while (true)
            {
                Console.Write("Enter (full) function name: ");
                string fullName = Console.ReadLine();
                FindFullyQualifiedStaticMethod(fullName);
            }
        }

        private static FunctionDefinition FindFullyQualifiedStaticMethod(string fullName)
        {
            string typeName = fullName.Substring(0, fullName.LastIndexOf('.'));
            string methodName = fullName.Substring(typeName.Length + 1);
            string typeNamespace = typeName.Substring(0, typeName.LastIndexOf('.'));

            if (!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName == typeNamespace))
            {
                Console.WriteLine($"{typeNamespace} is not loaded.");
                Assembly.Load(typeNamespace);
            }

            var type = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.Namespace == typeNamespace)
                .SingleOrDefault(t => t.FullName == typeName);
            if (type != null)
            {
                try
                {
                    return GetUniqueFunctionDefinition(methodName, type);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return null;
                }
                
            }
            else
            {
                Console.WriteLine("cannot find.");
                return null;
            }
        }

        private static FunctionDefinition GetUniqueFunctionDefinition(string methodName, Type type)
        {
            var staticMethod = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
            if (staticMethod.IsStatic)
                Console.WriteLine(staticMethod.Name);
            else
                Console.WriteLine("Method is not static.");

            return new FunctionDefinition()
            {
                Name = staticMethod.Name,
                Namespace = type.Namespace,
                Class = type.Name,
                Inputs = staticMethod.GetParameters()
                        .Where(p => !p.IsOut)
                        .Select(p => new FunctionDefinition.InputOutput()
                        {
                            Name = p.Name,
                            Type = p.ParameterType
                        }).ToArray(),
                Outputs = new FunctionDefinition.InputOutput[]
                        {
                                staticMethod.ReturnType == typeof(void)
                                ? null
                                : new FunctionDefinition.InputOutput()
                                {
                                    Name = "ReturnValue",
                                    Type = staticMethod.ReturnType
                                }
                        }.Where(io => io != null)
                        .Concat(staticMethod.GetParameters()
                        .Where(p => p.IsOut)
                        .Select(p => new FunctionDefinition.InputOutput()
                        {
                            Name = p.Name,
                            Type = p.ParameterType
                        })).ToArray(),
            };
        }
    }

    internal class FunctionDefinition
    {
        public class InputOutput
        {
            public Type Type { get; set; }
            public string Name { get; set; }
        }

        public string Name { get; set; }
        public string Namespace { get; set; }
        public string Class { get; set; }
        public InputOutput[] Inputs { get; set; }
        public InputOutput[] Outputs { get; set; }
    }
}