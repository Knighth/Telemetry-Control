using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using CitiesSkylinesDetour;
using ColossalFramework;
using ColossalFramework.HTTP.Paradox;
using System.Runtime.CompilerServices;

namespace TelemetryControl
{
    static class Detours
    {
        internal static Dictionary<MethodInfo, RedirectCallsState> redirectDic = new Dictionary<MethodInfo, RedirectCallsState>();


        /// <summary>
        /// This guy is our wrapper to doing the detours. it does the detour and then adds the returned
        /// RedirectCallState object too our dictionary for later reversal.
        /// </summary>
        /// <param name="type1">The original type of the method we're detouring</param>
        /// <param name="type2">Our replacement type of the method we're detouring</param>
        /// <param name="p">The original method\function name</param>
        /// <param name="OursIsPublic">If second method is public set to true</param>
        private static void RedirectCalls(Type type1, Type type2, string p,bool OursIsPublic = false, bool RestrictPrivate = false)
        {
            var bindflags1 = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            if (RestrictPrivate) { bindflags1 = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public; }
            var bindflags2 = BindingFlags.Static | BindingFlags.NonPublic;
            if (OursIsPublic) { bindflags2 = BindingFlags.Static | BindingFlags.Public; }

            var theMethod = type1.GetMethod(p, bindflags1);
            if (Mod.DEBUG_LOG_ON) { Helper.dbgLog(string.Concat("attempting to redirect ", theMethod.ToString(), " to ",type2.GetMethod(p, bindflags2).ToString()));}
            redirectDic.Add(theMethod, RedirectionHelper.RedirectCalls(theMethod, type2.GetMethod(p, bindflags2), false)); //makes the actual detour and stores the callstate info.
            if (Mod.DEBUG_LOG_ON) { Helper.dbgLog(string.Concat("redirect success: ", theMethod.ToString(), " to ", type2.GetMethod(p, bindflags2).ToString())); }
            //RedirectionHelper.RedirectCalls(type1.GetMethod(p, bindflags1), type2.GetMethod(p, bindflags2), false);
        }

        private static void RedirectCallsInstance(Type type1, Type type2, string p, bool OursIsPublic = false)
        {
            var bindflags1 = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var bindflags2 = BindingFlags.Instance | BindingFlags.NonPublic;
            if (OursIsPublic) { bindflags2 = BindingFlags.Instance | BindingFlags.Public; }
            var theMethod = type1.GetMethod(p, bindflags1);
            if (Mod.DEBUG_LOG_ON) { Helper.dbgLog(string.Concat("attempting to redirect ", theMethod.ToString(), " to ", type2.GetMethod(p, bindflags2).ToString())); }
            redirectDic.Add(theMethod, RedirectionHelper.RedirectCalls(theMethod, type2.GetMethod(p, bindflags2), false)); //makes the actual detour and stores the callstate info.
            if (Mod.DEBUG_LOG_ON) { Helper.dbgLog(string.Concat("redirect success: ", theMethod.ToString(), " to ", type2.GetMethod(p, bindflags2).ToString())); }
            //RedirectionHelper.RedirectCalls(type1.GetMethod(p, bindflags1), type2.GetMethod(p, bindflags2), false);
        }

        public static void SetupPush()
        {
            try
            {
                if (!Mod.IsPushDetoured)
                {
                    RedirectCallsInstance(typeof(Telemetry), typeof(TelemetryKH), "Push", true);
                    RedirectCallsInstance(typeof(Telemetry), typeof(TelemetryKH), "Clear", true);
                    RedirectCallsInstance(typeof(Telemetry), typeof(TelemetryKH), "IsStandardTelemetry");
                    MethodInfo[] methods = typeof(TelemetryKH).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
                    MethodInfo m1 = null;
                    MethodInfo m2 = null;
                    MethodInfo m3 = null;
                    MethodInfo m4 = null;
                    for (int i = 0; i < (int)methods.Length; i++)
                    {
                        MethodInfo methodInfo = methods[i];
                        if(methodInfo.Name.Contains("AddEvent"))
                        {
                            if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Found AddEvent in TelemetryKH"); }
                            ParameterInfo[] p = methodInfo.GetParameters();
                            if (p.Count() == 2)
                            {
                                if(Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("ParamName == " + p[0].Name.ToString()); }
                                if(Mod.DEBUG_LOG_LEVEL > 1) {Helper.dbgLog("ParamType == " + p[0].ParameterType.ToString());}
                                if(Mod.DEBUG_LOG_LEVEL > 1) {Helper.dbgLog("ParamName == " + p[1].Name.ToString());}
                                if(Mod.DEBUG_LOG_LEVEL > 1) {Helper.dbgLog("ParamType == " + p[1].ParameterType.ToString());}
                                if (p[0].ParameterType.ToString().Contains("Event"))
                                {
                                    m2 = methodInfo;
                                    if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Found method 2"); }
                                }
                                if(p[0].ParameterType.ToString().Contains("String"))
                                {
                                    m4 = methodInfo;
                                    if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Found method 4"); }
                                }
                            }
                        }
                    }
                    methods = typeof(Telemetry).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
                    for (int i = 0; i < (int)methods.Length; i++)
                    {
                        MethodInfo methodInfo = methods[i];
                        if (methodInfo.Name.Contains("AddEvent"))
                        {
                            if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Found AddEvent in Telemetry"); }
                            ParameterInfo[] p = methodInfo.GetParameters();
                            if (p.Count() == 2)
                            {
                                if(Mod.DEBUG_LOG_LEVEL > 1) {Helper.dbgLog("ParamName == " + p[0].Name.ToString());}
                                if(Mod.DEBUG_LOG_LEVEL > 1) {Helper.dbgLog("ParamType == " + p[0].ParameterType.ToString());}
                                if(Mod.DEBUG_LOG_LEVEL > 1) {Helper.dbgLog("ParamName == " + p[1].Name.ToString());}
                                if(Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("ParamType == " + p[1].ParameterType.ToString()); }

                                if (p[0].ParameterType.ToString().Contains("Event"))
                                {
                                    if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Found method 1"); }
                                    m1 = methodInfo;
                                }
                                if (p[0].ParameterType.ToString().Contains("String"))
                                {
                                    if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Found method 3"); }
                                    m3 = methodInfo;
                                }
                            }
                        }
                    }
                    if (m1 != null & m2 != null)
                    {
                        if(Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Manually passing 1&2"); }
                        redirectDic.Add(m1, RedirectionHelper.RedirectCalls(m1, m2, false));
                    }
                    if (m3 != null & m3 != null)
                    {
                        if(Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Manually passing 3&4"); }
                        redirectDic.Add(m3, RedirectionHelper.RedirectCalls(m3, m4, false));
                    }
                    Mod.IsPushDetoured = true;
                    if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Redirected Telemetry class calls."); }
                }
            }
            catch (Exception exception1)
            {
                Helper.dbgLog("SetupPush error:", exception1, true);
            }
        }


        /// <summary>
        /// Sets up our redirects of our replacement methods.
        /// </summary>
        public static void SetupTM()
        {
            if (Mod.IsTMDetoured) { return; }

            try
            {
                //do private ones
                if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Doing private TelemetryManager calls."); }
                MethodInfo[] methods = typeof(TelemetryManagerKH).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic);
                for (int i = 0; i < (int)methods.Length; i++)
                {
                    MethodInfo methodInfo = methods[i];
                    RedirectCalls(typeof(TelemetryManager), typeof(TelemetryManagerKH), methodInfo.Name);
                }
                //do public ones
                if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Doing public TelemetryManager calls."); }
                methods = typeof(TelemetryManagerKH).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public);
                if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("TelemetryManagerKH method count: " + methods.Length.ToString()); }
                MethodInfo m1 = null;
                MethodInfo m2 = null;
                MethodInfo m3 = null;
                MethodInfo m4 = null;
                for (int i = 0; i < (int)methods.Length; i++)
                {
                    MethodInfo methodInfo = methods[i];
                    if (methodInfo.Name.Contains("OnFeedClick"))
                    {
                        if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Found OnFeedClick in TelemetryManagerKH"); }
                        ParameterInfo[] p = methodInfo.GetParameters();
                        if (p.Count() == 1)
                        {
                            if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("ParamName == " + p[0].Name.ToString()); }
                            if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("ParamType == " + p[0].ParameterType.ToString());}
                            if (p[0].Name.Contains("url"))
                            {
                                if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("found method 2"); }
                                m2 = methodInfo;
                            }
                            if (p[0].Name.Contains("steamAppID"))
                            {
                                if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("found method 4"); }
                                m4 = methodInfo;
                            }
                        }
                        MethodInfo[] methods2 = typeof(TelemetryManager).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
                        for (int j = 0; j < (int)methods2.Length; j++)
                        {
                            if (m1 != null & m3 != null) { break; }
                            MethodInfo methodInfo2 = methods2[j];
                            if (methodInfo2.Name.Contains("OnFeedClick"))
                            {
                                if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Found OnFeedClick in TelemetryManager"); }
                                p = methodInfo2.GetParameters();
                                if (p.Count() == 1)
                                {
                                    if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("ParamName == " + p[0].Name.ToString());}
                                    if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("ParamType == " + p[0].ParameterType.ToString()); }
                                    if (p[0].Name.Contains("url"))
                                    {
                                        if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("found method 1"); }
                                        m1 = methodInfo2;
                                    }
                                    if (p[0].Name.Contains("steamAppID"))
                                    {
                                        if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("found method 3"); }
                                        m3 = methodInfo2;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!methodInfo.Name.Contains("TelemetryManagerKH"))
                        {
                            //normal for all but overloads.
                            RedirectCalls(typeof(TelemetryManager), typeof(TelemetryManagerKH), methodInfo.Name, true, true);
                        }
                    }
                }

                if (m1 != null & m2 != null)
                {
                    if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Doing manual OnFeedClick 1 and 2"); }
                    redirectDic.Add(m1, RedirectionHelper.RedirectCalls(m1, m2, false));
                }
                if (m3 != null & m4 != null)
                {
                    if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Doing manual OnFeedClick 3 and 4"); }
                    redirectDic.Add(m3, RedirectionHelper.RedirectCalls(m3, m4, false));
                }

                Mod.IsTMDetoured = true;
                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Redirected TelemetryManager calls."); }
            }
            catch (Exception exception1)
            {
                Helper.dbgLog("SetupTM error:", exception1, true);
            }
        }

        /// <summary>
        /// Reverses our redirects from ours back to C/O's
        /// </summary>
        public static void ReveseSetup()
        {
            if (Mod.IsTMDetoured == false & Mod.IsPushDetoured == false) { return; }
            if (redirectDic.Count == 0)
            {
                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("No state entries exists to revert."); }
                return;
            }
            try
            {
                foreach (var keypair in redirectDic)
                {
                    RedirectionHelper.RevertRedirect(keypair.Key, keypair.Value);
                }
                redirectDic.Clear();
                Mod.IsTMDetoured = false;
                Mod.IsPushDetoured = false;
                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Reverted redirected calls."); }
            }
            catch (Exception exception1)
            { Helper.dbgLog("ReverseSetup error:", exception1, true); }
        }


    }
}
