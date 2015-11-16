using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICities;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;

namespace TelemetryControl
{

    public static class MenuHooker 
    {
        //This is a total crappy hack because i'm probably missing something stupid
        //in terms of finding a way to fire on a "MainMenu" Scene load completion event.
        //Technically this serves my purpose but there has got to be a better way.
        
        public static bool AbortMiniFlag = false;


        //testing
        //internal static void SetText()
        //{
        //    UIView rootView = UIView.GetAView();
        //    UILabel tl = rootView.FindUIComponent<UILabel>("DisabledLabel");
        //    if (rootView != null && tl != null)
        //    {
        //        tl.text = Mod.WORKSHOPADPANEL_REPLACE_TEXT;
        //    }
 
        //}


        /// <summary>
        /// Used during OnSettingsUI, different then original use during startup.
        /// </summary>
        /// <returns></returns>
        public static System.Collections.IEnumerator SetDelayedDisabledLabelTextMini()
        {
            bool bWeAreDone = false;
            int timeoutcount = 0;
            if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog("Coroutine started " + DateTime.Now.ToString()); }
            //we need to delay a short amount of time otherwise both objects can exist 
            //but we'll set the text before their awake functions complete which setup the default next.
            // 100ms would probably work in most cases, I'm using about 1.5 sec -for machines that might be super slow. 
            yield return new WaitForSeconds(1.5f); 
            while (bWeAreDone == false & timeoutcount <= 120 & AbortMiniFlag == false)
            {
                timeoutcount++;
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 2) { Helper.dbgLog("timeoutcount= " + timeoutcount.ToString()+" abortflag="+ AbortMiniFlag.ToString()); }
                try
                {
                    UIView rootView = UIView.GetAView();
                    UILabel tl = rootView.FindUIComponent<UILabel>("DisabledLabel");
                    if (rootView != null && tl != null)
                    {
                        bWeAreDone = true;
                        if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("calling AttemptToDisableWorkshop via coroutine"); }
                        Mod.AttemptToDisableWorkshop(rootView);
                        if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("breaking coroutine - completed."); }
                        yield break;
                    }
                    if (AbortMiniFlag)
                    {
                        if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("breaking coroutine - aborted."); }
                        yield break;
                    }
                    if (Singleton<LoadingManager>.instance.m_currentlyLoading | !Singleton<LoadingManager>.instance.m_loadingComplete)
                    {
                        if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("breaking coroutine - (level loading) aborted."); }
                        yield break;
                    }
                }
                catch (Exception ex)
                {
                    Helper.dbgLog("Error in co-routine: ", ex, true);
                    yield break;
                }
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1 & timeoutcount >120) { Helper.dbgLog("timout exceeded, approximately 4 minutes passed."); }
                yield return new WaitForSeconds(2.0f);
            }
            if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("coroutine completed " + DateTime.Now.ToString()); }
            yield break;
        }


    }
}
