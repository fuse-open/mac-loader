using System;
using System.IO;
using System.Reflection;
using Claunia.PropertyList;

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
            var assembly = Assembly.LoadFile(assemblyPath);
            var type = assembly.GetType(mainClass, true);
            return (Application)Activator.CreateInstance(type);
        }
    }
}
