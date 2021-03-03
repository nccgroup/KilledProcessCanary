/*

A Windows Service to detect if it has been shutdown and hibernate if multiple instances are. 

Released as open source by NCC Group Plc - http://research.nccgroup.com/

Developed by Ollie Whitehouse, ollie dot whitehouse at nccgroup dot com

https://github.com/nccgroup/KilledProcessCanary

Released under AGPL see LICENSE for more information

thanks to Harry for the idea

 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace SWOLLENRIVER
{

    class Power
    {
        [DllImport("Powrprof.dll", CharSet = CharSet.Auto, ExactSpelling = true)]

        public static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);
    }

    static class Program
    {
        static Mutex mut = new Mutex(false, "SWOLLENRIVER");

        

        public static void IncCount()
        {
            MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen("mmf1", 2, MemoryMappedFileAccess.ReadWrite);
            MemoryMappedViewAccessor mmva = mmf.CreateViewAccessor(0, sizeof(int));

            mut.WaitOne();
            int Cur = mmva.ReadInt32(0);
            Cur++;
            mmva.Write(0, Cur);
            mut.ReleaseMutex();
        }

        public static void DecCount()
        {
            MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen("mmf1", 2, MemoryMappedFileAccess.ReadWrite);
            MemoryMappedViewAccessor mmva = mmf.CreateViewAccessor(0, sizeof(int));

            mut.WaitOne();
            int Cur = mmva.ReadInt32(0);
            Cur--;
            mmva.Write(0, Cur);
            mut.ReleaseMutex();
        }

        public static void CheckCount()
        {
            MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen("mmf1", 2, MemoryMappedFileAccess.ReadWrite);
            MemoryMappedViewAccessor mmva = mmf.CreateViewAccessor(0, sizeof(int));

            mut.WaitOne();
            int Cur = mmva.ReadInt32(0);
            mut.ReleaseMutex();

            // Number of instances threshold
            if( Cur < 2)
            {
                Power.SetSuspendState(true, true, true);
            }

        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            //
            // run from the command line
            // for testing
            //
            if (System.Environment.UserInteractive)
            {
                // Run two instances
                Console.WriteLine("[SWOLLENRIVER] Interactive");
                IncCount();
                Console.WriteLine("[SWOLLENRIVER] Sleeping for 10 seconds");
                Thread.Sleep(10000);
                Console.WriteLine("[SWOLLENRIVER] Checking how many of me are running");
                CheckCount();    
                DecCount();
            }
            //
            // run as Windows service
            // 
            else
            {
                Console.WriteLine("[SWOLLENRIVER] Service");
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new SWOLLENRIVERSVC()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
