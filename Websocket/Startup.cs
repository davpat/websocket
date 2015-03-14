using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TankDispatchManager
{
    //This class is for SignalR
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}