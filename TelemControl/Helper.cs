using ICities;
using ColossalFramework.IO;
using ColossalFramework.Plugins;
using ColossalFramework.Packaging;
using ColossalFramework;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TelemetryControl
{
    public class Helper
    {
        public struct KeycodeData
        {
            public byte NumOfCodes;
            public KeyCode kCode1;
            public KeyCode kCode2;
            public KeyCode kCode3;
        }


        //This holds our various options 
        [Flags]
        public enum TelemOption : uint
        {
            None = 0, //NoMeaning;
            DisableMachineInfo = 1,
            DisableCustomContent = 2,
            DisableStartSession = 4,
            DisableEndSession = 8,
            DisableMilestoneUnlock = 16,
            DisableOnQuit = 32,
            DisableOnStoreClick = 64,
            DisableOnClicks = 128,
            DisableSessionLoaded = 256,
            DisableParadoxLogin =512,
            DisableExceptionReporting = 1024,
            DisableWorkshopAdPanel = 2048,
            DisableOnAppStart = 4096,
            SetAPIUrlLocalHost = 8192,
            NoOpThePush = 16384,
            EnableAllButLogToFileInstead = 32768,
            EnableAll = 1048576, //Don't touch anything; full logging sent.
            DisableAll = 2097152 //super shortcut flag.
        }
 
        //should be enough for most log messages and we want this guy in the HFHeap.
        private static StringBuilder logSB = new System.Text.StringBuilder(512);


        public static bool HasTelemFlag(uint source,TelemOption toption)
        {
            if ((source & (uint)toption) == (uint)toption)
            { return true; }
            return false;
        }



        /// <summary>
        /// Our LogWrapper...used everywhere.
        /// </summary>
        /// <param name="sText">Text to log</param>
        /// <param name="ex">An Exception - if not null it's basic data will be printed.</param>
        /// <param name="bDumpStack">If an Exception was passed do you want the stack trace?</param>
        /// <param name="bNoIncMethod">If for some reason you don't want the method name prefaced with the log line.</param>
        public static void dbgLog(string sText, Exception ex = null, bool bDumpStack = false, bool bNoIncMethod = false) 
        {
            try
            {
                logSB.Length = 0;
                string sPrefix = string.Concat("[", Mod.MOD_DBG_Prefix);
                if (bNoIncMethod) { string.Concat(sPrefix, "] "); }
                else
                {
                    System.Diagnostics.StackFrame oStack = new System.Diagnostics.StackFrame(1); //pop back one frame, ie our caller.
                    sPrefix = string.Concat(sPrefix, ":", oStack.GetMethod().DeclaringType.Name, ".", oStack.GetMethod().Name, "] ");
                }
                logSB.Append(string.Concat(sPrefix, sText));

                if (ex != null)
                {
                    logSB.Append(string.Concat("\r\nException: ", ex.Message.ToString()));
                }
                if (bDumpStack)
                {
                    logSB.Append(string.Concat("\r\nStackTrace: ", ex.ToString()));
                }
                if (Mod.config != null && Mod.config.UseCustomLogFile == true)
                {
                    string strPath = System.IO.Directory.Exists(Path.GetDirectoryName(Mod.config.CustomLogFilePath)) ? Mod.config.CustomLogFilePath.ToString() : Path.Combine(DataLocation.executableDirectory.ToString(), Mod.config.CustomLogFilePath);
                    using (StreamWriter streamWriter = new StreamWriter(strPath, true))
                    {
                        streamWriter.WriteLine(logSB.ToString());
                    }
                }
                else 
                {
                    Debug.Log(logSB.ToString());
                }
            }
            catch (Exception Exp)
            {
                Debug.Log(string.Concat("[TelemetryControl.Helper.dbgLog()] Error in log attempt!  ", Exp.Message.ToString()));
            }
            logSB.Length = 0;
            //if we grew large for some odd reason (large log call), let's shink ourselves.
            if (logSB.Capacity > 8192)
            { logSB.Capacity = 4096; }

        }
    }

}
