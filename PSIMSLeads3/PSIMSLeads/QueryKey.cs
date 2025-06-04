// Decompiled with JetBrains decompiler
// Type: PSIMSLeads.QueryKey
// Assembly: PSIMSLeads, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 15BC1052-E0EF-48B7-B65D-E2917F0C2DC9
// Assembly location: C:\Users\cd104535\Desktop\PSIMSLeads.exe

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace PSIMSLeads
{
    public class QueryKey
    {
        public string QueryID { get; set; }

        public int Agency { get; set; }

        public int CallYear { get; set; }

        public int CallID { get; set; }

        public string RecordType { get; set; }

        public int RecordSequence { get; set; }

        public string Record { get; set; }

        public DateTime RecordDate { get; set; }

        public string WSID { get; set; }

        public string Query { get; set; }

        public string QueryParams { get; set; }

        public int LeadsService { get; set; }

        public int ToService { get; set; }

        public string Badge { get; set; }

        public string QueryFor { get; set; }

        public string QueryType { get; set; }

        public DateTime Created { get; set; }

        public string StateUsername { get; set; }

        public string Username { get; set; }

        public int RequestResponse { get; set; }

        public int SessionID { get; set; }

        public ConcurrentDictionary<ushort, byte[]> Responses { get; set; } = new();
        public CoreDataPacket CoreDataPacket { get; set; }

        public string Lic { get; set; }

        public string Lis { get; set; }

        public string Lit { get; set; }

        public string Liy { get; set; }

        public string Vin { get; set; }

        public string Vma { get; set; }

        public string Vyr { get; set; }

        public string Lname { get; set; }

        public string Fname { get; set; }

        public string Mname { get; set; }

        public string Dob { get; set; }

        public string Sex { get; set; }

        public string Race { get; set; }

        public string Oln { get; set; }

        public string Ols { get; set; }

        public string Noh { get; set; }

        public string Nam { get; set; }

        public string Pno { get; set; }

        public string ArticleType { get; set; }

        public string ArticleSerial { get; set; }

        public string OwnerNumber { get; set; }

        public bool Sighted { get; set; }

        public string ArticleId { get; set; }

        public string QueryEntity { get; set; }

        public string LNAM { get; set; }

        public string GIV1 { get; set; }

        public string GIV2 { get; set; }

        public string DOB { get; set; }

        public string FOID { get; set; }

        public string GunSerial { get; set; }

        public string GunMake { get; set; }

        public string GunCaliber { get; set; }

        public void ParseNameString(string concatenatedName)
        {
            var strArray1 = !string.IsNullOrWhiteSpace(concatenatedName) ? concatenatedName.Split(',') : throw new ArgumentException("Concatenated name cannot be null or empty", nameof(concatenatedName));
            if (strArray1.Length != 0)
                Lname = strArray1[0].Trim();
            if (strArray1.Length <= 1)
                return;
            var strArray2 = strArray1[1].Trim().Split(' ');
            if (strArray2.Length != 0)
                Fname = strArray2[0].Trim();
            if (strArray2.Length == 1)
                Mname = ""; // No middle name/initial
            else if (strArray2.Length > 1)
                Mname = strArray2[1].Trim();
        }
    }
}
