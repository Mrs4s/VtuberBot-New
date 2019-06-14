using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using VtuberBot.Bot;
using VtuberBot.Core;
using VtuberBot.Core.Extensions;

namespace VtuberBot.Plugin
{
    public class PluginManager
    {
        public static PluginManager Manager { get; set; } = new PluginManager();

        public List<PluginBase> Plugins { get; } = new List<PluginBase>();

        public void LoadPlugins(string path, VtuberBotObserver observer)
        {
            if (!Directory.Exists(path))
                return;
            if (Directory.Exists(Path.Combine(path, "Assembly")))
            {
                foreach (var lib in Directory.GetFiles(Path.Combine(path, "Assembly")).Where(v=>Path.GetExtension(v)==".dll"))
                {
                    LogHelper.Info("Load library: " + lib);
                    var bytes = File.ReadAllBytes(lib);
                    Assembly.Load(bytes);
                }
            }
            foreach (var file in Directory.GetFiles(path))
            {
                if (Path.GetExtension(file) != ".dll") continue;
                var plugin = LoadPlugin(file, observer);
                if (plugin == null)
                    LogHelper.Error("Cannot load plugin " + file);
                else
                    LogHelper.Info("Loaded plugin: " + plugin.PluginName);
            }
        }
        public PluginBase GetPlugin(string pluginName)
        {
            return Plugins.FirstOrDefault(v => v.PluginName.EqualsIgnoreCase(pluginName));
        }

        public void LoadPlugins(VtuberBotObserver observer)
        {
            var pluginPath = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");
            if (!Directory.Exists(pluginPath))
                Directory.CreateDirectory(pluginPath);
            LoadPlugins(pluginPath, observer);
        }

        public void UnloadPlugin(string pluginName)
        {
            var plugin = GetPlugin(pluginName);
            if (plugin != null)
                UnloadPlugin(plugin);
        }
        public void UnloadPlugin(PluginBase plugin)
        {
            plugin?.OnDestroy();
            LogHelper.Info("Destroy plugin: " + plugin?.PluginName);
            Plugins.RemoveAll(v => v == plugin);
        }

        public PluginBase LoadPlugin(string dllPath, VtuberBotObserver observer)
        {
            try
            {
                if (!File.Exists(dllPath))
                    return null;
                var bytes = File.ReadAllBytes(dllPath);
                var assembly = Assembly.Load(bytes);
                var pluginMain = assembly.GetExportedTypes().FirstOrDefault(v => v.BaseType == typeof(PluginBase));
                if (pluginMain == null)
                    return null;
                var plugin = Activator.CreateInstance(pluginMain) as PluginBase;
                plugin.Observer = observer;
                plugin.DllPath = dllPath;
                plugin.OnLoad();
                Plugins.Add(plugin);
                return plugin;
            }
            catch (Exception ex)
            {
                LogHelper.Error("Cannot load plugin " + dllPath, true, ex);
                return null;
            }
        }


    }
}
