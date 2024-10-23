using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLauncherManager.Model
{
    public class Config
    {
        public bool IsCloseAfterPluginStarted { get; set; } = true;
        
        public bool IsSteamAutoDetected { get; set; } = false;
    }
}