using DIRegistrator.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DIRegistrator.Registrator
{
    public static class Registrator
    {
        public static void RegisterInterfaces(IServiceCollection services, string environmentName)
        {
            var parentDirectory = Directory.GetParent(Directory.GetCurrentDirectory());
            var directories = parentDirectory.GetDirectories();
            var assemblies = GetAssembliesFromDirectories(directories, environmentName);

            RegisterServiceFromAssemblies(services, assemblies);
        }

        private static List<Assembly> GetAssembliesFromDirectories(DirectoryInfo[] directories, string environmentName)
        {
            var assemblies = new List<Assembly>();
            var environmentPath = GetEnvironmenthPath(environmentName);

            foreach (var directory in directories)
            {
                if (directory.FullName != Directory.GetCurrentDirectory())
                {
                    var dllLocation = directory.FullName + @"\bin\" + environmentPath + @"\netstandard2.0\" + directory.Name + ".dll";
                    var assemblyDirectory = Assembly.LoadFrom(dllLocation);
                    assemblies.Add(assemblyDirectory);
                }
            }

            return assemblies;
        }

        private static string GetEnvironmenthPath(string environmentName)
        {
            var environmentPath = environmentName == "Development" ? "Debug" : "Release";

            return environmentPath;
        }

        private static void RegisterServiceFromAssemblies(IServiceCollection services, List<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetExportedTypes();
                var interfaces = types.Where(t => t.IsInterface);

                foreach (var exportedInterface in interfaces)
                {
                    RegisterService(services, exportedInterface, types);
                }
            }
        }

        private static void RegisterService(IServiceCollection services, Type exportedInterface, Type[] exportedTypes)
        {
            var implementedType = exportedTypes.FirstOrDefault(t => !t.IsInterface && exportedInterface.IsAssignableFrom(t));

            if (implementedType != null)
            {
                var registerType = implementedType.GetCustomAttribute(typeof(Service));

                switch (registerType)
                {
                    case TransientService transient:
                        services.AddTransient(exportedInterface, implementedType);
                        break;
                    case ScopedService scoped:
                        services.AddScoped(exportedInterface, implementedType);
                        break;
                    case SingletonService singleton:
                        services.AddSingleton(exportedInterface, implementedType);
                        break;
                }
            }
        }
    }
}