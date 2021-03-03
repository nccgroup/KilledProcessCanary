/*

A Microsoft Windows memory page delta tool

Released as open source by NCC Group Plc - http://research.nccgroup.com/

Developed by Ollie Whitehouse, ollie dot whitehouse at nccgroup dot com

https://github.com/nccgroup/KilledProcessCanary

Released under AGPL see LICENSE for more information

thanks to Harry for the idea

*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace SWOLLENRIVER
{
    public partial class SWOLLENRIVERSVC : ServiceBase
    {
        public SWOLLENRIVERSVC()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            SWOLLENRIVER.Program.IncCount();
        }

        protected override void OnStop()
        {
            SWOLLENRIVER.Program.CheckCount();
            SWOLLENRIVER.Program.DecCount();

        }

        protected override void OnShutdown()
        {
            // Just Shutdown
        }
    }
}
