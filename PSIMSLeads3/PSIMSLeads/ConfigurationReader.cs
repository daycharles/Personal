using System;
using System.Configuration;
using System.Xml.Linq;
using System.Xml.XPath;

namespace PSIMSLeads
{
    // ConfigurationReader.cs
    namespace PSIMSLeads
    {
        public class ConfigurationReader
        {
            public const int CALL_REQ_COMMAND = 0; // Replace with the actual value
            public const int MOB_RSP_STATEQUERY = 1; // Replace with the actual value
            public const int STATE_RSP_DATA = 2; // Replace with the actual value
            public Settings AppSettings { get; } = new();

            public Settings LoadSettings()
            {
                var settings = new Settings();
                var xmlDoc = XDocument.Load(ConfigurationManager.AppSettings["MasterXMLPath"]);

                settings.TargetIP = GetSettingString(xmlDoc, "//PSIMSState/CoreCommunications/TargetIP", "0.0.0.0")
                    .Trim();
                settings.TargetPort = GetSettingInt(xmlDoc, "//PSIMSState/CoreCommunications/TargetPort", 0);

                settings.MaxIPBuffer = GetSettingInt(xmlDoc, "//PSIMSState/CoreCommunications/MaxIPBuffer", 4096);
                settings.WSCommIP = GetSettingString(xmlDoc, "//PSIMSWsm/WSCommunications/IPAddress", "0.0.0.0").Trim();
                settings.WSCommPort = GetSettingInt(xmlDoc, "//PSIMSWsm/WSCommunications/ListenPort", 0);

                settings.ServiceID = GetSettingInt(xmlDoc, "//PSIMSState/Service/ID", -1);
                settings.TraceServiceID = GetSettingInt(xmlDoc, "//PSIMSTrcm/Service/ID", -1);
                settings.MobServiceID = GetSettingInt(xmlDoc, "//PSIMSMob/Service/ID", -1);
                settings.WsmServiceID = GetSettingInt(xmlDoc, "//PSIMSWsm/Service/ID", -1);
                settings.CallServiceID = GetSettingInt(xmlDoc, "//PSIMSCall/Service/ID", 0);
                settings.WebServiceID = GetSettingInt(xmlDoc, "//PSIMSWeb/Service/ID", -1);
                settings.DbmsServiceID = GetSettingInt(xmlDoc, "//PSIMSDbms/Service/ID", -1);

                settings.DSN = GetSettingString(xmlDoc, "//PSIMSDbms/DBConnect/DSN", "NO VALUE").Trim();
                settings.UID = GetSettingString(xmlDoc, "//PSIMSDbms/DBConnect/UID", "NO VALUE").Trim();
                settings.PWD = DecryptPassword(GetSettingString(xmlDoc, "//PSIMSDbms/DBConnect/PWD", "NO VALUE"))
                    .Trim();
                settings.Database = GetSettingString(xmlDoc, "//PSIMSDbms/DBConnect/DataBase", "CT_PSIMS").Trim();
                settings.Server = GetSettingString(xmlDoc, "//PSIMSDbms/DBConnect/Server", "(local)").Trim();

                settings.SQLDriver =
                    GetSettingString(xmlDoc, "//PSIMSDbms/DBConnect/SQLDriver", "SQL Server Native Client 10.0").Trim();

                settings.LeadsID = GetSettingString(xmlDoc, "//PSIMSState/State/ID", "").Trim();
                settings.OriginatingSystemName =
                    GetSettingString(xmlDoc, "//PSIMSState/State/OriginatingSystemName", "UNKNOWN").Trim();
                settings.OriginatingCDCName =
                    GetSettingString(xmlDoc, "//PSIMSState/State/OriginatingCDCName", "UNKNOWN").Trim();
                settings.OriginatingLUName =
                    GetSettingString(xmlDoc, "//PSIMSState/State/OriginatingLUName", "").Trim();
                settings.ORI = GetSettingString(xmlDoc, "//PSIMSState/State/ORI", "").Trim();

                settings.SendIPAddress = GetSettingString(xmlDoc, "//PSIMSState/State/SendIPAddress", "0.0.0.0").Trim();
                settings.SendPort = GetSettingInt(xmlDoc, "//PSIMSState/State/SendPort", 0);
                settings.MaxStateBuffer = GetSettingInt(xmlDoc, "//PSIMSState/State/MaxBuffer", 1024 * 60);
                settings.DAC = GetSettingString(xmlDoc, "//PSIMSState/State/DAC", "").Trim();
                settings.Mnemonic = GetSettingString(xmlDoc, "//PSIMSState/State/Mnemonic", "").Trim();
                settings.UserID = DecryptPassword(GetSettingString(xmlDoc, "//PSIMSState/State/UserID", "")).Trim();
                settings.Password = DecryptPassword(GetSettingString(xmlDoc, "//PSIMSState/State/Password", "")).Trim();
                settings.HeartBeat = GetSettingInt(xmlDoc, "//PSIMSState/State/HeartBeat", 0);
                settings.NetworkInterface =
                    GetSettingString(xmlDoc, "//PSIMSState/State/NetworkInterface", "0.0.0.0").Trim();
                settings.InState = GetSettingString(xmlDoc, "//PSIMSState/State/ShortForm", "IL").Trim();

                settings.Trace = GetSettingString(xmlDoc, "//PSIMSState/State/Trace", "No").Trim()
                    .Equals("YES", StringComparison.OrdinalIgnoreCase);
                settings.TraceMax = GetSettingInt(xmlDoc, "//PSIMSState/State/TraceMax", 512);
                settings.TraceFile = GetSettingString(xmlDoc, "//PSIMSState/State/TraceFile", "").Trim();
                settings.TraceFileEnabled = !string.IsNullOrEmpty(settings.TraceFile.Trim());

                settings.TranslateNamePrefix = GetSettingString(xmlDoc, "//PSIMSState/State/TranslateNamePrefix", "No")
                    .Trim().Equals("YES", StringComparison.OrdinalIgnoreCase);

                settings.RetryTime = GetSettingInt(xmlDoc, "//PSIMSState/State/RetryTime", 60);
                settings.Loopback = GetSettingString(xmlDoc, "//PSIMSState/State/Loopback", "No").Trim()
                    .Equals("YES", StringComparison.OrdinalIgnoreCase);

                settings.QueryTTL = GetSettingInt(xmlDoc, "//PSIMSState/State/QueryTTL", 1);
                settings.StatePasswordRequired = !GetSettingString(xmlDoc, "//Defaults/StatePasswordRequired", "YES")
                    .Trim().Equals("NO", StringComparison.OrdinalIgnoreCase);

                // EyeNet
                settings.EyeNetListHilites = GetSettingString(xmlDoc, "//PSIMSState/State/ListHilites", "ListEyeNetHilites")
                    .Trim();
                settings.EyeNetStoreAllResponses = GetSettingString(xmlDoc, "//PSIMSState/State/EyeNetStoreAllResponses", "No")
                    .Trim().Equals("YES", StringComparison.OrdinalIgnoreCase);

                return settings;
            }

            private string GetSettingString(XDocument xmlDoc, string xpath, string defaultValue)
            {
                var element = xmlDoc.XPathSelectElement(xpath);
                return element != null ? element.Value : defaultValue;
            }

            private int GetSettingInt(XDocument xmlDoc, string xpath, int defaultValue)
            {
                var element = xmlDoc.XPathSelectElement(xpath);
                return element != null && int.TryParse(element.Value, out var result) ? result : defaultValue;
            }

            private string DecryptPassword(string encryptedPwd)
            {
                // Implement your decryption logic here if needed.
                return encryptedPwd;
            }
        }

        public class Settings
        {
            public int DbmsServiceID;
            public string TargetIP { get; set; }
            public int TargetPort { get; set; }
            public string WSCommIP { get; set; }
            public int WSCommPort { get; set; }
            public int MaxIPBuffer { get; set; }
            public int ServiceID { get; set; }
            public int TraceServiceID { get; set; }
            public int MobServiceID { get; set; }
            public int WsmServiceID { get; set; }
            public int CallServiceID { get; set; }
            public int WebServiceID { get; set; }
            public string DSN { get; set; }
            public string UID { get; set; }
            public string PWD { get; set; }
            public string Database { get; set; }
            public string Server { get; set; }
            public string SQLDriver { get; set; }
            public string LeadsID { get; set; }
            public string OriginatingSystemName { get; set; }
            public string OriginatingCDCName { get; set; }
            public string OriginatingLUName { get; set; }
            public string ORI { get; set; }
            public string SendIPAddress { get; set; }
            public int SendPort { get; set; }
            public int MaxStateBuffer { get; set; }
            public string DAC { get; set; }
            public string Mnemonic { get; set; }
            public string UserID { get; set; }
            public string Password { get; set; }
            public int HeartBeat { get; set; }
            public string NetworkInterface { get; set; }
            public string InState { get; set; }
            public bool Trace { get; set; }
            public int TraceMax { get; set; }
            public string TraceFile { get; set; }
            public bool TraceFileEnabled { get; set; }
            public bool TranslateNamePrefix { get; set; }
            public int RetryTime { get; set; }
            public bool Loopback { get; set; }
            public int QueryTTL { get; set; }
            public bool StatePasswordRequired { get; set; }
            public string EyeNetListHilites { get; set; }
            public bool EyeNetStoreAllResponses { get; set; }
        }
    }
}