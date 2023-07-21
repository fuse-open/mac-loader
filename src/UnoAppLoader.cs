using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Claunia.PropertyList;
using ObjCRuntime;

namespace Uno.AppLoader
{
    public class UnoAppLoader
    {
        public static Application Load()
        {
            var loader = typeof(UnoAppLoader).Assembly;
            var assemblyDir = Path.GetDirectoryName(loader.Location);
            var contents = Path.GetDirectoryName(assemblyDir);
            var plistFile = Path.Combine(contents, "Info.plist");
            var plist = LoadPropertyList(plistFile);
            var assembly = plist.ObjectForKey("UAAssembly").ToString();
            var mainClass = plist.ObjectForKey("UAMainClass").ToString();
            var title = plist.ObjectForKey("UATitle").ToString();
            var app = string.IsNullOrEmpty(assembly) || string.IsNullOrEmpty(mainClass)
                ? new DummyUnoApp()
                : LoadUnoApp(Path.Combine(assemblyDir, assembly), mainClass);
            app.Window.Title = title;
            return app;
        }

        static NSDictionary LoadPropertyList(string filename)
        {
            var file = new FileInfo(filename);
            return (NSDictionary)PropertyListParser.Parse(file);
        }

        static Application LoadUnoApp(string assemblyPath, string mainClass)
        {
            // Set DllImportResolver for loading native libraries used in generated assemblies
            AppDomain.CurrentDomain.AssemblyLoad += (sender, ev) => {
                var dir1 = Path.GetDirectoryName(ev.LoadedAssembly.Location);
                var dir2 = Path.GetDirectoryName(assemblyPath);

                // Skip assemblies not included in the build output
                if (dir1 != dir2)
                    return;

                try
                {
                    NativeLibrary.SetDllImportResolver(ev.LoadedAssembly, (libraryName, assembly, searchPath) => {
                        if (!libraryName.Contains(".dll") || libraryName.StartsWith("/"))
                            return IntPtr.Zero;

                        var dir = Path.GetDirectoryName(assembly.Location);
                        var lib = Path.Combine(dir, "lib" + libraryName.Replace(".dll", ".dylib"));

                        if (File.Exists(lib))
                            return Dlfcn.dlopen(lib, 0);

                        Console.Error.WriteLine("Native library not found: " + lib);
                        return IntPtr.Zero;
                    });
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("NativeLibrary.SetDllImportResolver: " + e);
                }
            };

            // Load the main generated assembly
            var assembly = Assembly.LoadFile(assemblyPath);
            var type = assembly.GetType(mainClass, true);
            return (Application)Activator.CreateInstance(type);
        }
    }
}
