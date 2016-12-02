using ColossalFramework.IO;
using ColossalFramework.PlatformServices;
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
            Hashtable hashtables = new Hashtable();

            if (Account.currentAccount == null)
            {
                hashtables.Add("userid", PlatformService.userID.ToString());
                if (PlatformService.apiBackend == APIBackend.Steam)
                {
                    hashtables.Add("universe", "steam");
                }
                else if (PlatformService.apiBackend == APIBackend.Rail)
                {
                    if (PlatformService.platformType == PlatformType.TGP)
                    {
                        hashtables.Add("universe", "tgp");
                    }
                    else if (PlatformService.platformType == PlatformType.QQGame)
                    {
                        hashtables.Add("universe", "qq");
                    }
                }
            }
            else
            {
                hashtables.Add("userid", Account.currentAccount.id);
                hashtables.Add("universe", "accounts");
            }

            hashtables.Add("game", DataLocation.productName.ToLower());
            hashtables.Add("steamid", PlatformService.userID.ToString());
            ArrayList arrayLists = new ArrayList();
            foreach (KeyValuePair<string, List<Telemetry.Pair>> mDatum in this.m_Data)
            {
                int num = (this.IsStandardTelemetry(mDatum.Key) ? 3 : 6);
                Hashtable item = null;
                if (mDatum.Value.Count != 0)
                {
                    for (int i = 0; i < mDatum.Value.Count; i++)
                    {
                        if (i % num == 0)
                        {
                            item = new Hashtable()
                            {
                                { "event", mDatum.Key }
                            };
                            DateTime universalTime = DateTime.Now.ToUniversalTime();
                            item.Add("timestamp", universalTime.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
                            arrayLists.Add(item);
                        }
                        Telemetry.Pair pair = mDatum.Value[i];
                        item[pair.key] = mDatum.Value[i].@value;
                    }
                }
                else
                {
                    item = new Hashtable()
                    {
                        { "event", mDatum.Key }
                    };
                    DateTime dateTime = DateTime.Now.ToUniversalTime();
                    item.Add("timestamp", dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
                    arrayLists.Add(item);
                }
            }
            hashtables.Add("data", arrayLists);

            

            if (Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.DisableAll) ||
                Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.NoOpThePush))
            {
                if (Mod.DEBUG_LOG_ON)
                { Helper.dbgLog("NoOp'd the Push and clearing the hashtables.  \r\n" + JSON.JsonEncode(hashtables)); }

                this.Clear();
                return;
            }

            if (Telemetry.debug || Helper.HasTelemFlag(Mod.config.TelemetryLevel, Helper.TelemOption.EnableAllButLogToFileInstead))
            {
                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Dumping telemetry to log enabled:"); }
                Helper.dbgLog("logging data that would be pushed:\n" + JSON.JsonEncode(hashtables));
                this.Clear();
                return;
            }
            if (Mod.DEBUG_LOG_ON)
            { Helper.dbgLog("debug mode, Pushing data.  :\r\n" + JSON.JsonEncode(hashtables)); }
            Request request2 = new Request("post", Telemetry.paradoxApiURL, hashtables);
            request2.Send(delegate(Request request)
            {
                this.Clear();
            });
        }
    }
}
