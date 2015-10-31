using ColossalFramework.IO;
using ColossalFramework.Steamworks;
using ColossalFramework;
using ColossalFramework.HTTP.Paradox;
using ColossalFramework.HTTP;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace TelemetryControl
{
    public class TelemetryKH
    {
        public enum Event
        {
            [EnumPosition("start_game", 0)]
            StartGame,
            [EnumPosition("exit_game", 0)]
            ExitGame,
            [EnumPosition("login", 0)]
            Login,
            [EnumPosition("first_launch", 0)]
            FirstLaunch,
            [EnumPosition("create_account", 0)]
            AccountCreated,
            [EnumPosition("meta", 0)]
            Meta
        }

        public struct Pair
        {
            private const int kMaxLength = 64;

            private string m_Key;

            private string m_Value;

            public string key
            {
                get
                {
                    return this.m_Key;
                }
            }

            public string value
            {
                get
                {
                    return this.m_Value;
                }
            }

            public Pair(string k, object v)
            {
                this.m_Key = k;
                string text = "null";
                if (v != null)
                {
                    text = v.ToString();
                }
                this.m_Value = text.Substring(0, Mathf.Min(text.Length, 64));
            }
        }

        private const string kUserID = "userid";

        private const string kUniverse = "universe";

        private const string kGame = "game";

        private const string kSteamID = "steamid";

        private const string kData = "data";

        private const string kTimestamp = "timestamp";

        private const string kEvent = "event";

        private const string kParadoxAccountUniverse = "accounts";

        private const string kSteamAccountUniverse = "steam";

        private Dictionary<string, List<Telemetry.Pair>> m_Data = new Dictionary<string, List<Telemetry.Pair>>();

        private string[] kEventNames = Utils.GetOrderedEnumNames<Telemetry.Event>();
        internal static readonly string originalAPIUrl = "https://opstm.paradoxplaza.com/cities";
        public static bool debug
        {
            get;
            set;
        }

        public static string paradoxApiURL
        {
            get;
            set;
        }

        static TelemetryKH()
        {

            //SetPDXAPILocalHost();
            if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("fired. " + DateTime.Now.ToString()); }
        }

        public static void SetPDXAPILocalHost()
        {
            try
            {
                if (Mod.IsEnabled && Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.SetAPIUrlLocalHost) )
                {
                    Telemetry.paradoxApiURL = Mod.config.SetAPIUrlLocalHost;
                    paradoxApiURL = Mod.config.SetAPIUrlLocalHost;
                    if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Setting PDX API URL to " + Mod.config.SetAPIUrlLocalHost); }
                    return;
                }
                else
                {
                    Telemetry.paradoxApiURL = originalAPIUrl;
                    paradoxApiURL = originalAPIUrl;
                }
            }
            catch (Exception ex)
            { Helper.dbgLog("Error:", ex, true); }
        }

        private bool IsStandardTelemetry(string value)
        {
            for (int i = 0; i < this.kEventNames.Length; i++)
            {
                if (value == this.kEventNames[i])
                {
                    return true;
                }
            }
            return false;
        }

        public void AddEvent(Telemetry.Event evt, params Telemetry.Pair[] infoPair)
        {
            if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("AddEvent Event!"); }
            this.AddEvent(evt.Name<Telemetry.Event>(), infoPair);
        }

        public void AddEvent(string evt, params Telemetry.Pair[] infoPair)
        {
            if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("AddEvent string!"); }
            List<Telemetry.Pair> list;
            if (this.m_Data.TryGetValue(evt, out list))
            {
                list.AddRange(infoPair);
                return;
            }
            list = new List<Telemetry.Pair>();
            list.AddRange(infoPair);
            this.m_Data.Add(evt, list);
        }

        public void Clear()
        {
            this.m_Data.Clear();
        }


        public void Push()
        {
            if (Mod.DEBUG_LOG_LEVEL > 1) { Helper.dbgLog("Push triggered. " + DateTime.Now.ToString()); }
            Hashtable hashtable = new Hashtable();
            if (Account.currentAccount != null)
            {
                hashtable.Add("userid", Account.currentAccount.id);
                hashtable.Add("universe", "accounts");
            }
            else
            {
                hashtable.Add("userid", ColossalFramework.Steamworks.Steam.steamID.ToString());
                hashtable.Add("universe", "steam");
            }
            hashtable.Add("game", ColossalFramework.IO.DataLocation.productName.ToLower());
            hashtable.Add("steamid", ColossalFramework.Steamworks.Steam.steamID.ToString());
            ArrayList arrayList = new ArrayList();
            //Dictionary<string, List<Telemetry.Pair>> m_Data = new Dictionary<string,List<Telemetry.Pair>>();
            //m_Data = (Dictionary<string, List<Telemetry.Pair>> )typeof(Telemetry).GetField("m_Data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(m_Data); //.GetValue(ppp);

            foreach (KeyValuePair<string, List<Telemetry.Pair>> current in m_Data)
            {
                int num = this.IsStandardTelemetry(current.Key) ? 3 : 6;
                Hashtable hashtable2 = null;
                if (current.Value.Count == 0)
                {
                    arrayList.Add(new Hashtable
        			{
				        {
					        "event",
					        current.Key
				        },
				        {
					        "timestamp",
					        DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fff")
				        }
			        });
                }
                else
                {
                    for (int i = 0; i < current.Value.Count; i++)
                    {
                        if (i % num == 0)
                        {
                            hashtable2 = new Hashtable();
                            hashtable2.Add("event", current.Key);
                            hashtable2.Add("timestamp", DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fff"));
                            arrayList.Add(hashtable2);
                        }
                        hashtable2[current.Value[i].key] = current.Value[i].value;
                    }
                }
            }
            hashtable.Add("data", arrayList);

            if (Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableAll) ||
                Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.NoOpThePush))
            {
                if (Mod.DEBUG_LOG_ON)
                { Helper.dbgLog("NoOp'd the Push and clearing the hashtables.  \r\n" + JSON.JsonEncode(hashtable)); }

                this.Clear();
                return;
            }

            if (Telemetry.debug || Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.EnableAllButLogToFileInstead))
            {
                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Dumping telemetry to log enabled:"); }
                Helper.dbgLog("logging data that would be pushed:\n" + JSON.JsonEncode(hashtable));
                this.Clear();
                return;
            }
            if (Mod.DEBUG_LOG_ON)
            { Helper.dbgLog("debug mode, Pushing data.  :\r\n" + JSON.JsonEncode(hashtable)); }
            Request request2 = new Request("post", Telemetry.paradoxApiURL, hashtable);
            request2.Send(delegate(Request request)
            {
                this.Clear();
            });
        }
    }
}
