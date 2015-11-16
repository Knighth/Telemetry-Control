using ColossalFramework;
using ColossalFramework.HTTP.Paradox;
using ColossalFramework.Plugins;
using ColossalFramework.Steamworks;
using ColossalFramework.UI;
using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TelemetryControl
{
    // TelemetryManager
    internal static class TelemetryManagerKH
    {
        static TelemetryManagerKH()
        {
            if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("initialized " + DateTime.Now.ToString()); }
        }

        private static void Awake()
        {
            if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("I never ever fire. " + DateTime.Now.ToString()); }
	        UIView.eventExceptionForwarded += new UIView.ForwardExceptionHandler(OnExceptionForwarded);
	        try
	        {
                string sc="colossal";
                string ft="firstTime";
                sc = (string)typeof(Settings).GetField("colossal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(sc);
                ft = (string)typeof(Settings).GetField("firstTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(ft);
                GameSettings.AddSettingsFile(new SettingsFile[]
		        {

			        new SettingsFile
			        {
				        systemFileName = sc
			        }
		        });
                SavedBool savedBool = new SavedBool(ft, sc, true);
		        CODebugBase<LogChannel>.Log(LogChannel.Core, "Telemetry enabled");
		        TelemetryKH telemetry = new TelemetryKH();
		        if (savedBool)
		        {
			        telemetry.AddEvent(Telemetry.Event.FirstLaunch, new Telemetry.Pair[0]);
			        savedBool.value = false;
		        }

                if (Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableAll) ==false |
                    Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableOnAppStart) == false)
                {
                    telemetry.AddEvent(Telemetry.Event.StartGame, new Telemetry.Pair[0]);
                }


                if (Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableAll) == false |
                    Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableMachineInfo) == false)
                { 
		            telemetry.AddEvent(Telemetry.Event.Meta, new Telemetry.Pair[]
		            {
			            new Telemetry.Pair("machineid", SystemInfo.deviceUniqueIdentifier),
			            new Telemetry.Pair("machinemodel", SystemInfo.deviceModel),
			            new Telemetry.Pair("gfxdevice", SystemInfo.graphicsDeviceName),
			            new Telemetry.Pair("gfxversion", SystemInfo.graphicsDeviceVersion),
			            new Telemetry.Pair("gfxmemory", SystemInfo.graphicsMemorySize),
			            new Telemetry.Pair("gfxshadermodel", SystemInfo.graphicsShaderLevel),
			            new Telemetry.Pair("os", SystemInfo.operatingSystem),
			            new Telemetry.Pair("oslanguage", Application.systemLanguage),
			            new Telemetry.Pair("cpu", SystemInfo.processorType),
			            new Telemetry.Pair("cpucount", SystemInfo.processorCount),
			            new Telemetry.Pair("sysmemory", SystemInfo.systemMemorySize)
		            });
                }
                if (Mod.DEBUG_LOG_ON ) { Helper.dbgLog("TMKH Awake doing startup push"); }
                telemetry.Push();
	        }
	        catch (GameSettingsException ex)
	        {
		        CODebugBase<LogChannel>.Warn(LogChannel.HTTP, ex.GetType() + ": Game Settings error " + ex.Message);
	        }
	        catch (Exception ex2)
	        {
		        CODebugBase<LogChannel>.Warn(LogChannel.HTTP, ex2.GetType() + ": Telemetry event failed " + ex2.Message);
	        }
        }


        public static void CustomContentInfo(int buildingsCount, int propsCount, int treeCount, int vehicleCount)
        {
            try
            {
                if (Mod.IsEnabled && Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableAll) ||
                    Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableCustomContent))
                {
                    if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Custom Content telemetry disabled."); }
                    return;
                }


                TelemetryKH telemetry = new TelemetryKH();
                telemetry.AddEvent("custom_content", new Telemetry.Pair[]
		        {
			        new Telemetry.Pair("buildings", buildingsCount),
			        new Telemetry.Pair("props", propsCount),
			        new Telemetry.Pair("trees", treeCount),
			        new Telemetry.Pair("vehicles", vehicleCount)
		        });
                telemetry.AddEvent("custom_mods", new Telemetry.Pair[]
		        {
			        new Telemetry.Pair("enabledModCount", Singleton<PluginManager>.instance.enabledModCount),
			        new Telemetry.Pair("modCount", Singleton<PluginManager>.instance.modCount)
		        });
                telemetry.Push();
                foreach (PluginManager.PluginInfo current in Singleton<PluginManager>.instance.GetPluginsInfo())
                {
                    if (current.isEnabled)
                    {
                        TelemetryKH telemetry2 = new TelemetryKH();
                        telemetry2.AddEvent("mod_used", new Telemetry.Pair[]
				        {
					        new Telemetry.Pair("modName", current.name),
					        new Telemetry.Pair("modWorkshopID", (!(current.publishedFileID != PublishedFileId.invalid)) ? "none" : current.publishedFileID.ToString()),
					        new Telemetry.Pair("assemblyInfo", current.assembliesString)
				        });
                        telemetry2.Push();
                    }
                }
            }
            catch (Exception ex)
            {
                CODebugBase<LogChannel>.Warn(LogChannel.HTTP, ex.GetType() + ": Telemetry event failed " + ex.Message);
            }
        }


        public static void StartSession(string mapName, string playerMap, SimulationManager.UpdateMode mode, SimulationMetaData ngs)
        {
            try
            {
                if (Mod.IsEnabled && Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableAll) ||
                    Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableStartSession))
                {
                    if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Start Session telemetry disabled."); }
                    return;
                }

                TelemetryKH telemetry = new TelemetryKH();
                telemetry.AddEvent("start_session", new Telemetry.Pair[]
		        {
			        new Telemetry.Pair("type", playerMap),
			        new Telemetry.Pair("start_flag", mode.ToString()),
			        new Telemetry.Pair("map_name", Path.GetFileName(mapName))
		        });
                if (ngs != null)
                {
                    telemetry.AddEvent("start_session", new Telemetry.Pair[]
			        {
				        new Telemetry.Pair("environment", ngs.m_environment),
				        new Telemetry.Pair("invert_traffic", ngs.m_invertTraffic),
				        new Telemetry.Pair("guid", ngs.m_gameInstanceIdentifier)
			        });
                }
                telemetry.Push();
            }
            catch (Exception ex)
            {
                CODebugBase<LogChannel>.Warn(LogChannel.HTTP, ex.GetType() + ": Telemetry event failed " + ex.Message);
            }
        }


        public static void SessionLoaded(long mainTime, long scenesTime, long simulationTime)
        {
            try
            {
                if (Mod.IsEnabled && Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableAll) ||
                    Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableSessionLoaded))
                {
                    if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Session Loaded telemetry disabled."); }
                    return;
                }

                TelemetryKH telemetry = new TelemetryKH();
                telemetry.AddEvent("session_loaded", new Telemetry.Pair[]
		        {
			        new Telemetry.Pair("totalTime", Math.Max(mainTime, Math.Max(scenesTime, simulationTime))),
			        new Telemetry.Pair("main", mainTime),
			        new Telemetry.Pair("scenes", scenesTime),
			        new Telemetry.Pair("simulationTime", simulationTime),
			        new Telemetry.Pair("gameTime", (!Singleton<SimulationManager>.exists) ? "Similation not running o_O" : Singleton<SimulationManager>.instance.m_currentGameTime.ToString("dd/MM/yyyy H:mm:ss"))
		        });
                telemetry.Push();
            }
            catch (Exception ex)
            {
                CODebugBase<LogChannel>.Warn(LogChannel.HTTP, ex.GetType() + ": Telemetry event failed " + ex.Message);
            }
        }


        public static void EndSession(ItemClass.Availability availability)
        {
            try
            {
                if (Mod.IsEnabled && Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableAll) ||
                    Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableEndSession))
                {
                    if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("End Session telemetry disabled."); }
                    return;
                }

                TelemetryKH telemetry = new TelemetryKH();
                telemetry.AddEvent("end_session", new Telemetry.Pair[]
		        {
			        new Telemetry.Pair("type", availability.ToString())
		        });
                telemetry.Push();
            }
            catch (Exception ex)
            {
                CODebugBase<LogChannel>.Warn(LogChannel.HTTP, ex.GetType() + ": Telemetry event failed " + ex.Message);
            }
        }


        private static void OnApplicationQuit()
        {
            try
            {
                if (Mod.IsEnabled && Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableAll) ||
                    Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableOnQuit))
                {
                    if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("OnApplicationQuit telemetry disabled."); }
                    return;
                }

                TelemetryKH telemetry = new TelemetryKH();
                telemetry.AddEvent(Telemetry.Event.ExitGame, new Telemetry.Pair[0]);
                telemetry.Push();
            }
            catch (Exception ex)
            {
                CODebugBase<LogChannel>.Warn(LogChannel.HTTP, ex.GetType() + ": Telemetry event failed " + ex.Message);
            }
        }



        public static void MilestoneUnlocked(MilestoneInfo info)
        {
            try
            {
                if (Mod.IsEnabled && Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableAll) ||
                    Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableMilestoneUnlock))
                {
                    if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Milestone telemetry disabled."); }
                    return;
                }

                TelemetryKH telemetry = new TelemetryKH();
                telemetry.AddEvent("unlocking", new Telemetry.Pair[]
		        {
			        new Telemetry.Pair("name", (!(info != null)) ? "Name unavailable" : info.name),
			        new Telemetry.Pair("gameTime", (!Singleton<SimulationManager>.exists) ? "Similation not running o_O" : Singleton<SimulationManager>.instance.m_currentGameTime.ToString("dd/MM/yyyy H:mm:ss"))
		        });
                telemetry.Push();
            }
            catch (Exception ex)
            {
                CODebugBase<LogChannel>.Warn(LogChannel.HTTP, ex.GetType() + ": Telemetry event failed " + ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"></param>
        public static void OnExceptionForwarded(Exception ex)
        {
            try
            {
                if (Mod.IsEnabled && Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableAll) ||
                    Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableExceptionReporting))
                {
                    if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Exception Reporting telemetry disabled."); }
                    return;
                }

                if (ex != null)
                {
                    TelemetryKH telemetry = new TelemetryKH();
                    telemetry.AddEvent("error", new Telemetry.Pair[]
			        {
				        new Telemetry.Pair("type", ex.GetType().Name),
				        new Telemetry.Pair("message", ex.Message)
			        });
                    telemetry.Push();
                }
            }
            catch (Exception ex2)
            {
                CODebugBase<LogChannel>.Warn(LogChannel.HTTP, ex2.GetType() + ": Telemetry event failed " + ex2.Message);
            }
        }


        public static void OnFeedClicked(uint steamAppID)
        {
            try
            {
                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Clicked steamAppID!"); }

                if (Mod.IsEnabled && Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableAll) ||
                    Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableOnClicks))
                {
                    if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Feed Click telemetry disabled."); }
                    return;
                }

                TelemetryKH telemetry = new TelemetryKH();
                telemetry.AddEvent("newsfeed_clicked", new Telemetry.Pair[]
		        {
			        new Telemetry.Pair("target", steamAppID.ToString())
		        });
                telemetry.Push();
            }
            catch (Exception ex)
            {
                CODebugBase<LogChannel>.Warn(LogChannel.HTTP, ex.GetType() + ": Telemetry event failed " + ex.Message);
            }
        }


        public static void OnFeedClicked(string url)
        {
            try
            {
                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Clicked url!"); }
                if (Mod.IsEnabled && Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableAll) ||
                    Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableOnClicks))
                {
                    if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Feed Click telemetry disabled."); }
                    return;
                }

                TelemetryKH telemetry = new TelemetryKH();
                telemetry.AddEvent("newsfeed_clicked", new Telemetry.Pair[]
		        {
			        new Telemetry.Pair("target", url)
		        });
                telemetry.Push();
            }
            catch (Exception ex)
            {
                CODebugBase<LogChannel>.Warn(LogChannel.HTTP, ex.GetType() + ": Telemetry event failed " + ex.Message);
            }
        }


        public static void OnStoreClicked()
        {
            try
            {
                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Store Click!"); }

                if (Mod.IsEnabled && Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableAll) ||
                    Helper.HasTelemFlag(Mod.config.TelemetryLevel,Helper.TelemOption.DisableOnStoreClick))
                {
                    if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Store Click telemetry disabled."); }
                    return;
                }

                TelemetryKH telemetry = new TelemetryKH();
                telemetry.AddEvent("store_clicked", new Telemetry.Pair[0]);
                telemetry.Push();
            }
            catch (Exception ex)
            {
                CODebugBase<LogChannel>.Warn(LogChannel.HTTP, ex.GetType() + ": Telemetry event failed " + ex.Message);
            }
        }


        public static void ParadoxAccountCreated()
        {
            try
            {
                if (Mod.IsEnabled && Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableAll))
                {
                    if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("PDX account creation event telemetry disabled."); }
                    return;
                }
                TelemetryKH telemetry = new TelemetryKH();
                telemetry.AddEvent(Telemetry.Event.AccountCreated, new Telemetry.Pair[0]);
                telemetry.Push();
            }
            catch (Exception ex)
            {
                CODebugBase<LogChannel>.Warn(LogChannel.HTTP, ex.GetType() + ": Telemetry event failed " + ex.Message);
            }
        }


        public static void ParadoxLogin(bool autoLogin)
        {
            try
            {
                if (Mod.IsEnabled && Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableAll) ||
                    Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableParadoxLogin))
                {
                    if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("PDX Login telemetry disabled."); }
                    return;
                }

                TelemetryKH telemetry = new TelemetryKH();
                telemetry.AddEvent(Telemetry.Event.Login, new Telemetry.Pair[]
		        {
			        new Telemetry.Pair("autologin", autoLogin)
		        });
                telemetry.Push();
            }
            catch (Exception ex)
            {
                CODebugBase<LogChannel>.Warn(LogChannel.HTTP, ex.GetType() + ": Telemetry event failed " + ex.Message);
            }
        }
    }
}
