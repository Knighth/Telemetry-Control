using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.Steamworks;
using ColossalFramework.UI;
using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using CitiesSkylinesDetour;
using System.Text;
using UnityEngine;

namespace TelemetryControl
{
     public class Mod : IUserMod
    {
        public static bool DEBUG_LOG_ON = false;
        public static byte DEBUG_LOG_LEVEL = 0;
        internal const ulong MOD_WORKSHOPID = 556416380uL;
        internal const string MOD_OFFICIAL_NAME = "Telemetry Control";  //debug==must match folder name
        internal const string MOD_DLL_NAME = "TelemetryControl";
        internal const string MOD_DESCRIPTION = "Allows you to control what if any data is sent to PDX.";
        internal static readonly string MOD_DBG_Prefix = "Telemetry Control"; //same..for now.
        internal const string VERSION_BUILD_NUMBER = "1.2.2-f3 build_002";
        public static readonly string MOD_CONFIGPATH = "TelemetryControl_Config.xml";
        internal const string UILABEL_MSG_TEXT = "You may mouse-over each item to get some idea of what data the event sends to Paradox.\nRemember that your Steamid or Pdx id is always attached to each telemetry message.\n\n*Please note that the application startup event and the machine information events\n happen before this mod can load and can not be blocked with this mod.\n (Unless you are using a patched Assembly-CSharp.dll - see workshop page for more information.)";
        internal const string WORKSHOPADPANEL_REPLACE_TEXT = "Panel disabled by Telemetry Control mod.";
        internal const string WORKSHOPADPAENL_ORG_TEXT = "This panel is inactive as the game was started using the '-noWorkshop' toggle.";

        public static bool IsEnabled = false;           //tracks if the mod is enabled.
        public static bool IsInited = false;            //tracks if we're inited
        private static bool isMsgLabelSetup = false;
        private static UILabel uiMsg; 

        public static bool IsPushDetoured = false;       //Have we redirected Telemetry?
        public static bool IsTMDetoured = false;       //Have we redirected TelemetryManager?
        public static bool IsInProcessOfDisable = false;
        private static bool InToogleProcess = false;
        private static string orgNoWorkshopText = "";
        private static UIComponent MyOptionComponent;

        public static Configuration config;
        //private static bool isFirstEnable = true;


        public string Description
        {
            get
            {
                return MOD_DESCRIPTION;
            }
        }

        public string Name
        {
            get
            {

                return MOD_OFFICIAL_NAME ;

            }
        }


        public void OnEnabled()
        {
            if (Mod.DEBUG_LOG_ON & Mod.DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog("fired."); }
            IsInProcessOfDisable = false;
            MenuHooker.AbortMiniFlag = false;
            ReloadConfigValues(true, false);
            if (Mod.IsInited == false)
            {
                Mod.IsEnabled = true;
                Mod.init();
            }
            else 
            {   //already init'd just apply any updated config values.
                Mod.IsEnabled = true;
                IsInProcessOfDisable = false;
                SetStartupOptions();
            }
            Helper.dbgLog(" This mod has been set enabled. " + DateTime.Now.ToString() );
        }


         public void OnDisabled()
        {
            if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog("fired."); }
            un_init();
            Mod.IsEnabled = false;
            Helper.dbgLog(Mod.MOD_OFFICIAL_NAME + " v" + VERSION_BUILD_NUMBER + " This mod has been set disabled or unloaded.");
        }

         
         
         /// <summary>
         /// Public Constructor on load we grab our config info and init();
         /// </summary>
        public Mod()
		{
            try
            {
                Helper.dbgLog("\r\n" + Mod.MOD_OFFICIAL_NAME + " v" + Mod.VERSION_BUILD_NUMBER + " Mod has been loaded. " + DateTime.Now.ToString());
                if (!IsInited)
                {
                    ReloadConfigValues(false, false);
                    if (DEBUG_LOG_ON) { Helper.dbgLog("starting init: " + DateTime.Now.ToString()); }
  //                  isFirstEnable = false;
                    init();
                }
            }
            catch(Exception ex)
            { Helper.dbgLog("[" + MOD_DBG_Prefix + "]", ex, true); }

 
        }
        
         /// <summary>
         /// Called to either initially load, or force a reload our config file var; called by mod initialization and again at mapload. 
         /// </summary>
         /// <param name="bForceReread">Set to true to flush the old object and create a new one.</param>
         /// <param name="bNoReloadVars">Set this to true to NOT reload the values from the new read of config file to our class level counterpart vars</param>
         public static void ReloadConfigValues(bool bForceReread, bool bNoReloadVars)
         {
             try
             {
//                 if(isFirstEnable == true)
//                 {return;}

                 if (bForceReread)
                 {
                     config = null;
                     if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL >= 1) { Helper.dbgLog("Config re-read requested."); }
                 }
                 config = Configuration.Deserialize(MOD_CONFIGPATH);
                 if (config == null)
                 {
                     config = new Configuration();
                     config.ConfigVersion = Configuration.CurrentVersion;
                     //reset of setting should pull defaults
                     Helper.dbgLog("Existing config was null. Created new one.");
                     Configuration.Serialize(MOD_CONFIGPATH, config); //let's write it.
                 }
                 if (config != null && bNoReloadVars == false) //set\refresh our vars by default.
                 {
                     config.ConfigVersion = Configuration.CurrentVersion;
                     DEBUG_LOG_ON = config.DebugLogging;
                     DEBUG_LOG_LEVEL = config.DebugLoggingLevel;
                     if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL == 0) { DEBUG_LOG_LEVEL = 1; }
                     if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog("Vars refreshed"); }
                 }
                 if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog(string.Format("Reloaded Config data ({0}:{1} :{2})", bForceReread.ToString(), bNoReloadVars.ToString(), config.ConfigVersion.ToString())); }
             }
             catch (Exception ex)
             { Helper.dbgLog("Exception while loading config values.", ex, true); }

         }

        internal static void init()
        {
            
            if (IsInited == false)
            {
                Detours.SetupPush();
                Detours.SetupTM();
                IsInited = true;
                if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL >= 2)
                { 
                    Helper.dbgLog("DumpingredirectionTable:");
                    StringBuilder tmpSB = new StringBuilder(2048);
                    foreach (KeyValuePair<MethodInfo, RedirectCallsState> kvp in Detours.redirectDic)
                    {
                        tmpSB.AppendFormat("Name: {0} {1}",kvp.Key.Name,Environment.NewLine);
                    }
                    Helper.dbgLog(tmpSB.ToString());
                }
                IsInProcessOfDisable = false;
                SetStartupOptions();
                if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog("Init completed. " + DateTime.Now.ToString()); }
            }
        }

         internal static void un_init()
         {
             if (IsInited)
             {
                 if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog("Un-Init triggered."); }
                 MenuHooker.AbortMiniFlag = true;
                 Detours.ReveseSetup();
                 IsInited = false;
                 IsInProcessOfDisable = true;
                 SetStartupOptions();
                 IsInProcessOfDisable = false;
                 if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog("Un-Init finished."); }
             }
         }


        private void LoggingChecked(bool en)
        {
            DEBUG_LOG_ON = en;
            config.DebugLogging = en;
            if (DEBUG_LOG_LEVEL == 0) { DEBUG_LOG_LEVEL = 1; }
            Configuration.Serialize(MOD_CONFIGPATH, config);
        }


        private void eventVisibilityChanged(UIComponent component, bool value)
        {
            MyOptionComponent = component; //store this for later, could be useful.
            if (value)
            {
                component.eventVisibilityChanged -= eventVisibilityChanged;
                component.parent.StartCoroutine(DoToolTips(component));
            }
        }

         /// <summary>
         /// Sets up tool tips. Would have been much easier if they would have let us specify the name of the components.
         /// </summary>
         /// <param name="component"></param>
         /// <returns></returns>
        private System.Collections.IEnumerator DoToolTips(UIComponent component)
        {
            yield return new WaitForSeconds(0.300f);  //pause for 1/2 second then come back and do rest.
            try
            {
                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Refreshing tooltips telemetrylevel=" + Mod.config.TelemetryLevel.ToString()); }

                UICheckBox[] cb = component.GetComponentsInChildren<UICheckBox>(true);
                if (cb != null && cb.Length > 0)
                {
                    for (int i = 0; i < (cb.Length); i++)
                    {
                        switch (cb[i].text)
                        {
                            case "Enable Verbose Logging":
                                cb[i].tooltip = "Enables detailed logging for debugging purposes\n See config file for even more options, unless there are problems you probably don't want to enable this.";
                                break;
                            //case "Disable OnAppStartup":
                            //    cb[i].tooltip = "Disables telemetry sent for when you boot up the game exe.\n**Please Note: This setting does nothing atm, mods load too late to change this\n if you want to disable this you must use patched Assemembly-CSharp.dll";
                            //    cb[i].isChecked = Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableOnAppStart);
                            //    break;
                            //case "Disable Machine Info":
                            //    cb[i].tooltip = "Disables telemetry sent for when you boot up the game exe.\n it includes information to id your specific computer spec & steamid or paradox login\n**Please Note: This setting does nothing atm, mods load too late to change this\n if you want to disable this you must use patched Assemembly-CSharp.dll";
                            //    cb[i].isChecked = Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableMachineInfo);
                            //    break;
                            case "Disable Custom Content":
                                cb[i].tooltip = "Disables telemetry about what custom content you load with a map.\n It includes information such has counts of building,props,trees,vehicles,mods, and details about every enabled mod.";
                                cb[i].isChecked = Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableCustomContent );
                                break;
                            case "Disable Session Start":
                                cb[i].tooltip = "Disables telemetry about what Session Starts(loading a map).\n it includes information such has mapname,mapfilename,loadmode,environment,inverted traffic and map guid.";
                                cb[i].isChecked = Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableStartSession);
                                break;
                            case "Disable Session Loaded":
                                cb[i].tooltip = "Disables telemetry about a Loaded Session (map loading completed).\n it includes information such has current time, time in your map, and how long part of the load took to execute.";
                                cb[i].isChecked = Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableSessionLoaded );
                                break;
                            case "Disable Session End":
                                cb[i].tooltip = "Disables telemetry about a Session End (map unloaded).\n it includes data that a session has ended, and of what type it was (map,game,asset).";
                                cb[i].isChecked = Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableEndSession);
                                break;
                            case "Disable Exception Reporting":
                                cb[i].tooltip = "Disables telemetry about an Exception Error occuring.\n This only sends the 'type' of error and the basic error message, it does not send a stack trace.";
                                cb[i].isChecked = Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableExceptionReporting);
                                break;
                            case "Disable OnAppQuit":
                                cb[i].tooltip = "Disables telemetry sent when you exit the game.\n This includes data that you exited the game and a timestamp.";
                                cb[i].isChecked = Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableOnQuit);
                                break;
                            case "Disable Store Clicks":
                                cb[i].tooltip = "Disables telemetry sent when you click on a store item.\n This only sends that you clicked on the store button.";
                                cb[i].isChecked = Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableOnStoreClick);
                                break;
                            case "Disable Feed Clicks":
                                cb[i].tooltip = "Disables telemetry sent when you click on a workshop feed\\news item \n This sends that you clicked on one and the target steamAppID or url upon which you clicked.";
                                cb[i].isChecked = Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableOnClicks);
                                break;
                            case "Disable Paradox Login":
                                cb[i].tooltip = "Disables telemetry sent when the game logs you into your paradox account \n This sends data that you were auto-logged in and a timestamp.";
                                cb[i].isChecked = Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableParadoxLogin);
                                break;
                            case "Enable All SendToFile Only":
                                cb[i].tooltip = "Enables all telemetry - but nothing will be sent to Paradox, only logged in your log file.";
                                cb[i].isChecked = Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.EnableAllButLogToFileInstead);
                                break;
                            case "DisableWorkshopAdPanel":
                                cb[i].tooltip = "Disables the workshop 'feed' panel, does NOT disable Workshop in general.\n There is no telemetry directly associated with disabling this.\n I simply find the feeds a waste of bandwidth.";
                                cb[i].isChecked = Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableWorkshopAdPanel);
                                break;
                            case "NoOpThePush":
                                cb[i].tooltip = "This is a master overide to make Telemetry.Push() (function that sends the data) do absolutely nothing.\n If set nothing will be sent OR even logged (if not in verbose logging mode).";
                                cb[i].isChecked = Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.NoOpThePush);
                                break;
                            case "SetURL To LocalHost":
                                cb[i].tooltip = "Sets the Paradox API URL to whatever you have in your config file.\n The default is 'https://localhost:49100/cities' if enabled.\n Can be used if you want to enable everything but send data your own web server.";
                                cb[i].isChecked = Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.SetAPIUrlLocalHost);
                                break;

                            case "Disable All Telemetry":
                                cb[i].tooltip = "Disables all telemetry - Nothing will be sent to Paradox.\nYou do NOT have to select the individual options if this is set.\n *Please see note at bottom of options page about the OnAppStartup and MachineInfo telemetry events.";
                                cb[i].isChecked = Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableAll);
                                break;
                            case "Enable All Telemetry":
                                cb[i].tooltip = "Enables all telemetry - The game's default behavior.";
                                cb[i].isChecked = Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.EnableAll);
                                break;
                            default:
                                break;
                        }
                    }
                }

                List<UIButton> bb = new List<UIButton>();
                component.GetComponentsInChildren<UIButton>(true, bb);
                if ( bb.Count > 0)
                { 
                    bb[0].tooltip = "Press this after making changes to ensure all changes are activated.\n "; 

                    if (!isMsgLabelSetup)
                    { 
                        uiMsg = component.AddUIComponent<UILabel>();
                        isMsgLabelSetup = true;
                        uiMsg.name = "TelemetryMessageText";
                        uiMsg.text = UILABEL_MSG_TEXT;
                        uiMsg.width = 350f;
                        
                        //uiMsg.wordWrap = true;
                        uiMsg.relativePosition = new Vector3(bb[0].relativePosition.x, bb[0].relativePosition.y + 30f);
                        uiMsg.Show();
                    }

                }

                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Tooltips set"); }
            }
            catch(Exception ex)
            {
                Helper.dbgLog("", ex, true);
            }
            yield break;
        }


        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelper hp = (UIHelper)helper;
            UIScrollablePanel panel = (UIScrollablePanel)hp.self;
            panel.eventVisibilityChanged += eventVisibilityChanged;

            UIHelperBase group = helper.AddGroup("Telemetry Control");
            group.AddCheckbox("Disable All Telemetry", Helper.HasTelemFlag(Mod.config.TelemetryLevel,Helper.TelemOption.DisableAll), OptionDisableAllTelemetry);
            //group.AddCheckbox("Disable OnAppStartup", Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableOnAppStart), OptionDisableOnAppStart);
            //group.AddCheckbox("Disable Machine Info", Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableMachineInfo), OptionDisableMachineInfo);
            group.AddCheckbox("Disable Custom Content", Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableCustomContent), OptionDisableCustomContent);
            group.AddCheckbox("Disable Session Start", Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableStartSession), OptionDisableStartSession);
            group.AddCheckbox("Disable Session Loaded", Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableSessionLoaded), OptionDisableSessionLoaded);
            group.AddCheckbox("Disable Session End", Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableEndSession), OptionDisableEndSession);
            group.AddCheckbox("Disable Exception Reporting", Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableExceptionReporting), OptionDisableExceptionReporting);
            group.AddCheckbox("Disable OnAppQuit", Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableOnQuit), OptionDisableOnQuit);
            group.AddCheckbox("Disable Store Clicks", Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableOnStoreClick), OptionDisableOnStoreClick);
            group.AddCheckbox("Disable Feed Clicks", Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableOnClicks), OptionDisableOnClicks);
            group.AddCheckbox("Disable Paradox Login", Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableParadoxLogin), OptionDisableParadoxLogin);
            group.AddCheckbox("Enable All SendToFile Only", Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.EnableAllButLogToFileInstead), OptionEnableAllButLogToFileInstead);
            group.AddCheckbox("Enable All Telemetry", Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.EnableAll), OptionEnableAllTelemetry);
            group.AddSpace(16);
            group.AddCheckbox("DisableWorkshopAdPanel",Helper.HasTelemFlag(Mod.config.TelemetryLevel,Helper.TelemOption.DisableWorkshopAdPanel),OptionDisableWorkshopAdPanel);
            group.AddCheckbox("NoOpThePush", Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.NoOpThePush), OptionNoOpThePush);
            group.AddCheckbox("SetURL To LocalHost", Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.SetAPIUrlLocalHost), OptionSetAPIUrlLocalHost);
            group.AddCheckbox("Enable Verbose Logging", DEBUG_LOG_ON, LoggingChecked);
            group.AddSpace(20);
            group.AddButton("Update", SetStartupOptions);
            if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("OnSettingsUI fired" + DateTime.Now.ToString()); }

            if (Mod.IsEnabled && Mod.IsInited && !Mod.IsInProcessOfDisable &&
                Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableWorkshopAdPanel) &&
                Singleton<TelemetryManager>.instance != null)
            {
                //SetupWorkShopFeed();
                Singleton<TelemetryManager>.instance.StartCoroutine(MenuHooker.SetDelayedDisabledLabelTextMini()); 
            }
        }

        public void OptionDisableAllTelemetry(bool en)
        {
            ToggleTelemetrySetting(Helper.TelemOption.DisableAll,en);
        }

        public void OptionEnableAllTelemetry(bool en)
        {
            ToggleTelemetrySetting(Helper.TelemOption.EnableAll, en);
        }

        //public void OptionDisableOnAppStart(bool en)
        //{
        //    ToggleTelemetrySetting(Helper.TelemOption.DisableOnAppStart, en);
        //}

        //public void OptionDisableMachineInfo(bool en)
        //{
        //    ToggleTelemetrySetting(Helper.TelemOption.DisableMachineInfo, en);
        // }

        public void OptionDisableOnQuit(bool en)
        {
            ToggleTelemetrySetting(Helper.TelemOption.DisableOnQuit, en);
        }



        public void OptionDisableCustomContent(bool en)
        {
            ToggleTelemetrySetting(Helper.TelemOption.DisableCustomContent, en);
        }


        public void OptionDisableStartSession(bool en)
        {
            ToggleTelemetrySetting(Helper.TelemOption.DisableStartSession, en);
        }

        public void OptionDisableSessionLoaded(bool en)
        {
            ToggleTelemetrySetting(Helper.TelemOption.DisableSessionLoaded, en);
        }

        public void OptionDisableEndSession(bool en)
        {
            ToggleTelemetrySetting(Helper.TelemOption.DisableEndSession, en);
        }


        public void OptionDisableMilestoneUnlock(bool en)
        {
            ToggleTelemetrySetting(Helper.TelemOption.DisableMilestoneUnlock, en);
        }


        public void OptionDisableExceptionReporting(bool en)
        {
            ToggleTelemetrySetting(Helper.TelemOption.DisableExceptionReporting, en);
        }


        public void OptionDisableOnStoreClick(bool en)
        {
            ToggleTelemetrySetting(Helper.TelemOption.DisableOnStoreClick, en);
        }


        public void OptionDisableOnClicks(bool en)
        {
            ToggleTelemetrySetting(Helper.TelemOption.DisableOnClicks, en);
        }


        public void OptionDisableParadoxLogin(bool en)
        {
            ToggleTelemetrySetting(Helper.TelemOption.DisableParadoxLogin, en);
        }


        public void OptionDisableWorkshopAdPanel(bool en)
        {
            ToggleTelemetrySetting(Helper.TelemOption.DisableWorkshopAdPanel, en);
        }

        public void OptionSetAPIUrlLocalHost(bool en)
        {
            ToggleTelemetrySetting(Helper.TelemOption.SetAPIUrlLocalHost, en);
        }

        public void OptionEnableAllButLogToFileInstead(bool en)
        {
            ToggleTelemetrySetting(Helper.TelemOption.EnableAllButLogToFileInstead, en);
        }

        public void OptionNoOpThePush(bool en)
        {
            ToggleTelemetrySetting(Helper.TelemOption.NoOpThePush, en);
        }

         /// <summary>
         /// Does the acutal flag setting\unsetting
         /// </summary>
         /// <param name="toptionset">flag to work on</param>
         /// <param name="turnon">true\false flip it on or off</param>
         public void ToggleTelemetrySetting(Helper.TelemOption toptionset,bool turnon)
        {

            // abort, we are being called via settings triggered inside DoTooltips.
            // we don't want to setup an infinate loop since changing checkmarks in checkforreset via dotooltips will 
            // trigger 'toggle' event.
            if (InToogleProcess)
            {
                if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("skipping toggle trigger" + toptionset.ToString()); }
                return; 
            }
            
            InToogleProcess = true; //set our flag
            CheckForReset(toptionset,turnon);
            if(turnon)
            {
                Mod.config.TelemetryLevel = (Mod.config.TelemetryLevel | (uint)toptionset);
                if (Mod.DEBUG_LOG_LEVEL >= 1) { Helper.dbgLog(string.Concat("Toggling ",toptionset.ToString()," flag to on.")); }
            }
            else
            {
                if(Helper.HasTelemFlag(Mod.config.TelemetryLevel,toptionset))
                {
                    Mod.config.TelemetryLevel = (Mod.config.TelemetryLevel ^ (uint)toptionset);
                    if (Mod.DEBUG_LOG_LEVEL >= 1) { Helper.dbgLog(string.Concat("Toggling ", toptionset.ToString(), " flag to off.")); }
                }
            }
/*            if (Helper.HasTelemFlag(Mod.config.TelemetryLevel,Helper.TelemOption.EnableAll))
            { Mod.config.TelemetryBootEnabled = true; }
            else if(Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableOnAppStart) |
                Helper.HasTelemFlag(Mod.config.TelemetryLevel ,Helper.TelemOption.DisableMachineInfo))
            { Mod.config.TelemetryBootEnabled = false; }
            else
            { Mod.config.TelemetryBootEnabled = false; }
*/
            Configuration.Serialize(MOD_CONFIGPATH, config);
            InToogleProcess = false; //remove flag.
        }


         /// <summary>
         /// This guy does some extra work for us before flipping a flag.
         /// Basically we check to make sure if flipping certain flags on we turn off or clear other
         /// flags first. In particular EnableAll;
         /// </summary>
         /// <param name="toption1">flag about to be flipped</param>
         /// <param name="turnon1">state it's about to be flipped too</param>
         private void CheckForReset(Helper.TelemOption toption1,bool turnon1)
         {
             uint tmporg = Mod.config.TelemetryLevel;
             if (turnon1)
             {
                 //if we're enabling all then reset all flags first.
                 if(toption1 == Helper.TelemOption.EnableAll)
                 {
                     Mod.config.TelemetryLevel = 0;
                     if(Helper.HasTelemFlag(tmporg,Helper.TelemOption.DisableWorkshopAdPanel))
                     {Mod.config.TelemetryLevel = Mod.config.TelemetryLevel | (uint)Helper.TelemOption.DisableWorkshopAdPanel;}
                     if (Helper.HasTelemFlag(tmporg, Helper.TelemOption.SetAPIUrlLocalHost))
                     { Mod.config.TelemetryLevel = Mod.config.TelemetryLevel | (uint)Helper.TelemOption.SetAPIUrlLocalHost; }
                     
                     if (DEBUG_LOG_LEVEL >= 1) { Helper.dbgLog(" EnableAll being turned on, Reset Telemetry level=0 + key exceptions."); }
                     if (MyOptionComponent != null && tmporg != Mod.config.TelemetryLevel)
                     { MyOptionComponent.StartCoroutine(DoToolTips(MyOptionComponent)); }
                     return;
                 }

                 //if were enabling and it's not ones of these, let's remove the enable all flag.
                 if (toption1 != Helper.TelemOption.DisableWorkshopAdPanel & toption1 != Helper.TelemOption.SetAPIUrlLocalHost &
                     toption1 != Helper.TelemOption.NoOpThePush & toption1 != Helper.TelemOption.EnableAllButLogToFileInstead)
                 {
                     if (DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("a disabletelemetry command was used making sure EnableAll is turned off."); }
                     Mod.config.TelemetryLevel = Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.EnableAll) ? (Mod.config.TelemetryLevel ^ (uint)Helper.TelemOption.EnableAll) : Mod.config.TelemetryLevel;
                     if (MyOptionComponent != null && tmporg != Mod.config.TelemetryLevel)
                     { MyOptionComponent.StartCoroutine(DoToolTips(MyOptionComponent)); }
                     return;
                 }
             }

         }



         internal static void SetStartupOptions()
         {
             try
             {
                 //call out to this guy 
                 SetupWorkShopFeed();


                 //Handle EnableAllButLogToFileInstead aka Telemetry.debug
                 if ((Mod.config.TelemetryLevel & (uint)Helper.TelemOption.EnableAllButLogToFileInstead) == (uint)Helper.TelemOption.EnableAllButLogToFileInstead)
                 {
                     if (!IsInProcessOfDisable)
                     {
                         ColossalFramework.HTTP.Paradox.Telemetry.debug = true;
                         TelemetryKH.debug = true;
                         if (DEBUG_LOG_LEVEL >= 1) { Helper.dbgLog("Setting both Telemetry.Debug's True"); }
                     }
                     else
                     {
                         //we're disabling so let's put things back for now.
                         ColossalFramework.HTTP.Paradox.Telemetry.debug = false;
                         TelemetryKH.debug = false;
                     }
                 }
                 else
                 {
                     //always leave the original unless disabling we're assuming patched dll so really doesn't matter
                     //on the "starup" object but matters for the rest once debug disabled.
                     //Once 'enabled' we're already using our so no need to set theirs if we're not disabling.
                     if (IsInProcessOfDisable)
                     { 
                         ColossalFramework.HTTP.Paradox.Telemetry.debug = false;
                         if (DEBUG_LOG_LEVEL >= 1) { Helper.dbgLog("Setting Colossal Telemetry.Debug's false"); }
                     }

                     TelemetryKH.debug = false;
                     if (DEBUG_LOG_LEVEL >= 1) { Helper.dbgLog("Setting TelemetryKH.Debug false"); }
 
                 }

                 //does based on the config, check's itself for state.
                 TelemetryKH.SetPDXAPILocalHost();
                 
//                 if (MyOptionComponent != null) 
//                 { MyOptionComponent.StartCoroutine(DoToolTips(MyOptionComponent)); }
             }
             catch (Exception ex)
             { Helper.dbgLog("Error: ", ex, true); }
         }

         /// <summary>
         /// Handles the calls to the wrappers to setup\enable\disable the Workshop panel and disabled label text. 
         /// </summary>
         internal static void SetupWorkShopFeed()
         {
             if (Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableWorkshopAdPanel))
             {
                 if (!IsInProcessOfDisable)
                 {
                     //we are set, we are no in the middle of disabling; if we're 'enabled'(gui) then we'll
                     //attempt to disable workshop,if not it'll just set the dontInitialize var to true.
                     if (DEBUG_LOG_LEVEL >= 1) { Helper.dbgLog("Flagged to disable, no unloading of mod, trying to Set DisableWorkshopAdPanel true"); }
                     WorkShopAdPanelDisableWrapper();
                 }
                 else
                 {
                     //here we are in the mod\disable - removal process and we want to return things to normal.
                     WorkShopAdPanelEnableWrapper();
                     if (DEBUG_LOG_LEVEL >= 1) { Helper.dbgLog("Mod is in disabling process, we're set, but we are setting DisableWorkshopAdPanel false"); }
                 }
             }
             else
             {
                 //Panel should be enabled, if mod is enabled (ie have gui let's try and re-enable), else just
                 //flip the dontInitialize bool var.
                 WorkShopAdPanelEnableWrapper();
                 if (DEBUG_LOG_LEVEL >= 1) { Helper.dbgLog("Set DisableWorkshopAdPanel false"); }
             }
         }


         //Called by SetStartupOptions()
         //Used to just break up the function to smaller pieces
         private static void WorkShopAdPanelEnableWrapper() 
         {
             try
             {
                 UIView rootView2 = UIView.GetAView();
                 if (Mod.IsEnabled)
                 {
                     if (rootView2 != null)
                     {
                         AttemptToReEnableWorkshop(rootView2);
                     }
                 }
                 else
                 {
                     if (DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Mod Not enabled - Setting DisableWorkshopAdPanel false"); }
                     WorkshopAdPanel tmpWAP = new WorkshopAdPanel();
                     typeof(WorkshopAdPanel).GetField("dontInitialize", BindingFlags.NonPublic | BindingFlags.Static).SetValue(tmpWAP, false);
                     if (rootView2 != null)
                     {
                         UILabel dl = rootView2.FindUIComponent<UILabel>("DisabledLabel");
                         if (dl != null)
                         {
                             orgNoWorkshopText = dl.text;
                             dl.text = orgNoWorkshopText;
                         }
                     }
                 }
             }
             catch(Exception ex)
             { Helper.dbgLog("Error: ", ex, true); }
         }


         // This guy runs delayed for the specific case
         // of when we're properly disabled at launch time but when the mod's
         // normal attempt during enable fails because the GUI view is null
         // and has not fully loaded yet. we keep trying every 2 seconds.
         // to set the disabled text to our own.
         public static System.Collections.IEnumerator SetDelayedDisabledLabelText()
         {
             bool bWeAreDone = false;
             if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog("Coroutine started " + DateTime.Now.ToString()); }
             while (bWeAreDone == false)
             {
                try
                {
                    UIView rootView = UIView.GetAView();
                    if (rootView != null)
                    {
                        bWeAreDone = true;
                        if (DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("calling AttemptToDisableWorkshop via coroutine"); }
                        AttemptToDisableWorkshop(rootView);
                        if (DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("breaking coroutine - completed."); }
                        yield break;
                    }             
                }
                catch (Exception ex)
                {
                    Helper.dbgLog("Error in co-routine: ", ex, true);
                    yield break;
                }
                yield return new WaitForSeconds(2.0f);
            }
             if (DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("coroutine completed " + DateTime.Now.ToString()); }
            yield break;
         }

         //Called by SetStartupOptions()
         //Used to just break up the function to smaller pieces
         private static void WorkShopAdPanelDisableWrapper() 
         {
             try
             {
                 UIView rootView = UIView.GetAView();
                 if (Mod.IsEnabled)
                 {

                     if (rootView != null)
                     { AttemptToDisableWorkshop(rootView); }
                     else
                     { Singleton<TelemetryManager>.instance.StartCoroutine(SetDelayedDisabledLabelText()); }
                 }
                 else //used during bootup process
                 {
                     if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("NotInDisableProcess, Mod.IsEnabled=false - setting static var on WorkshopAdPanels"); }
                     WorkshopAdPanel tmpWAP = new WorkshopAdPanel();
                     typeof(WorkshopAdPanel).GetField("dontInitialize", BindingFlags.NonPublic | BindingFlags.Static).SetValue(tmpWAP, true);
                     if(rootView !=null)
                     {
                         UILabel dl = rootView.FindUIComponent<UILabel>("DisabledLabel");
                         if (dl != null)
                         {
                             orgNoWorkshopText = dl.text;
                             dl.text = "Panel disabled via Telemetry Control Mod.";
                         }
                     }
                 }
             }
             catch (Exception ex)
             { Helper.dbgLog("Error: ", ex, true); }
         }


         internal static bool AttemptToDisableWorkshop(UIView rootView)
         {
             WorkshopAdPanel tmpWAP;
             try
             {
                 if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Attempting to Disable WorkshopAdPanel"); }

                 List<WorkshopAdPanel> WAPList = new List<WorkshopAdPanel>();
                 rootView.GetComponentsInChildren<WorkshopAdPanel>(true, WAPList);
                 if (DEBUG_LOG_LEVEL > 2) { Helper.dbgLog("Got the list"); }
                 if (WAPList.Count > 0)
                 {
                     if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Found " + WAPList.Count.ToString() + " WorkshopAdPanels"); }
                     tmpWAP = WAPList[0];
                     if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL > 2) { Helper.dbgLog("0= n=" + tmpWAP.component.name + "  cn=" + tmpWAP.component.cachedName + "  pn=" + tmpWAP.component.parent.name + "  pcn=" + tmpWAP.component.parent.cachedName); }
                     typeof(WorkshopAdPanel).GetField("dontInitialize", BindingFlags.NonPublic | BindingFlags.Static).SetValue(tmpWAP, true);
                     if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("WorkshopAdPanel.dontInitialize= set"); }
                     bool retb = (bool)typeof(WorkshopAdPanel).GetField("dontInitialize", BindingFlags.NonPublic | BindingFlags.Static).GetValue(tmpWAP);
                     if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("WorkshopAdPanel.dontInitialize=" + retb.ToString()); }
                     if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("now going to play with components - WorkshopAdPanels"); }

                     UIScrollablePanel usp = (UIScrollablePanel)typeof(WorkshopAdPanel).GetField("m_ScrollContainer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tmpWAP);
                     if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("grabbed Scrollable ref via reflection."); }
                     if (usp != null)
                     {
                         if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Scrollable was not null."); }
                         usp.isEnabled = false;
                         usp.isVisible = false;
                         usp.Awake();
                     }
                     tmpWAP.m_AutoScroll = false;
                     tmpWAP.m_AutoScrollInterval = 3600;
                     //tmpWAP.component.isEnabled = false;
                     //tmpWAP.component.isVisible = false;
                     tmpWAP.component.Awake();
                     UILabel tl = rootView.FindUIComponent<UILabel>("DisabledLabel");
                     if (tl != null)
                     {
                         orgNoWorkshopText = tl.text;
                         tl.text = WORKSHOPADPANEL_REPLACE_TEXT;
                         tl.isVisible = true;
                         if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("DisabledLabel set to " + tl.text + "  orginal=" + orgNoWorkshopText); }

                     }
                     return true;
                 }

             }
             catch (Exception ex)
             { Helper.dbgLog("Error: ", ex, true); }
            
             try
             {
                 tmpWAP = new WorkshopAdPanel();
                 typeof(WorkshopAdPanel).GetField("dontInitialize", BindingFlags.NonPublic | BindingFlags.Static).SetValue(tmpWAP, true);
                 UILabel dl = rootView.FindUIComponent<UILabel>("DisabledLabel");
                 if (dl != null)
                 {
                     orgNoWorkshopText = dl.text;
                     dl.text = WORKSHOPADPANEL_REPLACE_TEXT;
                     if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("fallthrough - DisabledLabel set to " + dl.text + "  orginal=" + orgNoWorkshopText); }
                 }
             }
             catch (Exception ex)
             { Helper.dbgLog("Error: trying to set disabledLabel component during fallthrough", ex, true); }

             return false;
         }



         private static bool AttemptToReEnableWorkshop(UIView rootView)
         {
             WorkshopAdPanel tmpWAP;
             try
             {
                 if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Attempting to Renable WorkshopAdPanel"); }
                 List<WorkshopAdPanel> WAPList = new List<WorkshopAdPanel>();
                 rootView.GetComponentsInChildren<WorkshopAdPanel>(true, WAPList);
                 if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL > 2) { Helper.dbgLog("Got the list"); }
                 if (WAPList.Count > 0)
                 {
                     if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Found " + WAPList.Count.ToString() + " WorkshopAdPanels"); }
                     tmpWAP = WAPList[0];
                     if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL > 2) { Helper.dbgLog("0= n=" + tmpWAP.component.name + "  cn=" + tmpWAP.component.cachedName + "  pn=" + tmpWAP.component.parent.name + "  pcn=" + tmpWAP.component.parent.cachedName); }
                     typeof(WorkshopAdPanel).GetField("dontInitialize", BindingFlags.NonPublic | BindingFlags.Static).SetValue(tmpWAP, false);
                     if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("WorkshopAdPanel.dontInitialize= set"); }
                     bool retb = (bool)typeof(WorkshopAdPanel).GetField("dontInitialize", BindingFlags.NonPublic | BindingFlags.Static).GetValue(tmpWAP);
                     if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("WorkshopAdPanel.dontInitialize=" + retb.ToString()); }
                     if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Now going to play with components - WorkshopAdPanels"); }
                     UIScrollablePanel usptmp = tmpWAP.Find<UIScrollablePanel>("Container");
                     if (usptmp != null)
                     {
                         if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Found scrollablePanel 'Container' "); }
                         typeof(WorkshopAdPanel).GetField("m_ScrollContainer", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(tmpWAP, usptmp);
                         if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("inserted it into base panel object."); }
                     }

                     UIScrollablePanel usp = (UIScrollablePanel)typeof(WorkshopAdPanel).GetField("m_ScrollContainer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tmpWAP);
                     if (usp != null)
                     {
                         if (DEBUG_LOG_ON && DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Scrollable was not null"); }
                         usp.isEnabled = true;
                         usp.isVisible = true;
                         usp.Awake();
                     }
                     tmpWAP.m_AutoScroll = true;
                     tmpWAP.m_AutoScrollInterval = 20;
                     tmpWAP.enabled = true;
                     tmpWAP.component.isEnabled = true;
                     tmpWAP.component.isVisible = true;
                     tmpWAP.component.Awake();
                     Steam.workshop.QueryItems();
                     typeof(WorkshopAdPanel).GetField("m_LastTime", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(tmpWAP, Time.realtimeSinceStartup);
                     UILabel tl = rootView.FindUIComponent<UILabel>("DisabledLabel");
                     if (tl != null)
                     {
                         tl.text = orgNoWorkshopText;
                         tl.isVisible = false;
                     }
                     if (DEBUG_LOG_ON) { Helper.dbgLog("re-enabled WorkshopAdPanel"); }
                     return true;
                 }
                 else
                 {
                     if (DEBUG_LOG_ON) { Helper.dbgLog("No Panels were found to reset. We are likely quiting app from inside an active game."); }
                 }
             }
             catch (Exception ex)
             { Helper.dbgLog("Error: ", ex, true); }

             try
             {
                 if (!Singleton<LoadingManager>.instance.m_applicationQuitting)
                 {
                     tmpWAP = new WorkshopAdPanel();
                     typeof(WorkshopAdPanel).GetField("dontInitialize", BindingFlags.NonPublic | BindingFlags.Static).SetValue(tmpWAP, false);
                     UILabel dl = rootView.FindUIComponent<UILabel>("DisabledLabel");
                     if (dl != null)
                     {
                         dl.text = orgNoWorkshopText;
                     }
                 }
             }
             catch (Exception ex)
             { if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Error: Most likey panel was destroyed already during exit\\quit game action", ex, true); } }

             return false;
         }
    }
}
