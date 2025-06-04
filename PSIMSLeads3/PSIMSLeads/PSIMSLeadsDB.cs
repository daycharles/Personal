// Decompiled with JetBrains decompiler
// Type: PSIMSLeads.PSIMSLeadsDB
// Assembly: PSIMSLeads, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 15BC1052-E0EF-48B7-B65D-E2917F0C2DC9
// Assembly location: C:\Users\cd104535\Desktop\PSIMSLeads.exe

using PSIMS.Data.Contexts;
using PSIMS.Data.Models;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PSIMSLeads
{
    public class PSIMSLeadsDB
    {
        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["PSIMSContext"].ConnectionString;
        private Logger _logger;

        public PSIMSLeadsDB(RichTextBox textLog, Logger logger)
        {
            _logger = logger;
        }

        public async Task StoreAppData(int nFromService, string data)
        {
            using (var db = new PSIMSContext(ConnectionString))
            {
                var str = data.Replace("'", "''");
                if (str.Length > 8000)
                    str = str.Substring(0, 8000);
                var now = DateTime.Now;
                var entity = new PSIMSStateData()
                {
                    EntrySource = "LEADSREQ",
                    EntryTimestamp = now,
                    EntryService = nFromService,
                    EntryData = str
                };
                try
                {
                    db.PSIMSStateDatas.Add(entity);
                    var num = await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogResponse($"Database error: Code {(object)ex.HResult}, Message {(object)ex.Message}");
                }
            }
        }
    }
}
