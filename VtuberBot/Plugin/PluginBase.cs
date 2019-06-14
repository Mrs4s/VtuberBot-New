using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VtuberBot.Bot;

namespace VtuberBot.Plugin
{
    public abstract class PluginBase
    {
        public virtual string PluginName { get; set; }

        public string DllPath { get; set; }

        public VtuberBotObserver Observer { get; set; }

        public abstract void OnLoad();

        public abstract void OnDestroy();


    }
}
