using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Newtonsoft.Json;
using PSIMSLeads.PSIMSLeads;

namespace PSIMSLeads;

public class PSIMSLeadsQuery
{
    private readonly List<QueryKey> queryKeys = new();
    public PSIMSEyeNetDB _eyeNet;
    private Logger _logger;
    private Settings _settings;

    public PSIMSLeadsQuery(RichTextBox textBox, Logger logger)
    {
        _eyeNet = new PSIMSEyeNetDB(textBox, logger);
        _logger = logger;
    }

    public FoxTalkClient GetCurrentClient()
    {
        return FoxTalkClient.GetFoxTalkClient();
    }

    public async Task BuildCallPerson(QueryKey model, Settings configReader)
    {
        _settings = configReader;
        _logger.LogResponse("Building Call Person. WSID: " + model.WSID);

        var xelement1 = XElement.Parse(model.Query);

        var nam = xelement1.Element("Person")?.Element("NAM")?.Value;
        var rawDob = xelement1.Element("Person")?.Element("DOB")?.Value;
        var dob = ParseDob(rawDob);
        var sex = xelement1.Element("Person")?.Element("SEX")?.Value;
        var oln = xelement1.Element("Person")?.Element("OLN")?.Value;
        var ols = xelement1.Element("Person")?.Element("OLS")?.Value;

        if (!string.IsNullOrWhiteSpace(oln))
            oln = oln.Replace("-", "").Trim().ToUpper();

        if (string.IsNullOrWhiteSpace(ols))
            ols = "IL";

        var sequence = xelement1.Element("Person")?.Element("Sequence")?.Value;

        model.Dob = dob;
        model.Sex = sex;
        model.Oln = oln;
        model.Ols = ols;
        model.RecordSequence = Convert.ToInt32(sequence);

        var transactionElements = new List<XElement>();
        bool addedOln = false;

        var hasOlnAndOls = !string.IsNullOrWhiteSpace(model.Oln) && !string.IsNullOrWhiteSpace(model.Ols);
        var hasNameBasedData = !string.IsNullOrWhiteSpace(nam)
            && !string.IsNullOrWhiteSpace(dob)
            && !string.IsNullOrWhiteSpace(sex);

        // Option B
        if (hasOlnAndOls)
        {
            transactionElements.Add(new XElement("OLN", model.Oln));
            transactionElements.Add(new XElement("OLS", model.Ols));
            addedOln = true;
        }

        // Option A or C
        if (hasNameBasedData)
        {
            model.ParseNameString(nam);
            var namUpper = nam.ToUpper();  // Ensure NAM is uppercase
            transactionElements.Add(new XElement("NAM", namUpper));
            transactionElements.Add(new XElement("DOB", dob));
            transactionElements.Add(new XElement("SEX", sex));

            if (!string.IsNullOrWhiteSpace(model.Race))
                transactionElements.Add(new XElement("RAC", model.Race.ToUpper())); // Optional but safe
        }


        transactionElements.Add(new XElement("RSH", "N"));

        var document = new XDocument(
            new XElement("OFML",
                new XElement("HDR",
                    new XElement("ID", model.QueryID),
                    new XElement("DAC", _settings.OriginatingCDCName),
                    new XElement("DAT", DateTime.Now.ToString("yyyyMMddHHmmss")),
                    new XElement("REF", model.QueryID),
                    new XElement("USR", model.StateUsername),
                    new XElement("ORI", _settings.ORI),
                    new XElement("MKE", "Z2")
                ),
                new XElement("TRN", transactionElements)
            )
        );

        model.Query = document.ToString();
        model.RecordType = "P";

        _logger.LogResponse($"Sending data message from {model.StateUsername} at workstation {model.WSID}");
        await GetCurrentClient().SendDataMessage(model);
    }


    private string ParseDob(string inputDob)
    {
        if (string.IsNullOrWhiteSpace(inputDob))
            return null;

        // Try MMddyyyy first
        if (DateTime.TryParseExact(inputDob, "MMddyyyy", null, DateTimeStyles.None, out var dobParsed))
            return dobParsed.ToString("yyyyMMdd");

        // Fallback to general DateTime parsing if needed
        if (DateTime.TryParse(inputDob, out var dobFlexible))
            return dobFlexible.ToString("yyyyMMdd");

        _logger.LogResponse("Invalid DOB format: " + inputDob);
        return null;
    }

    public async Task BuildCallVehicle(QueryKey model, Settings configReader)
    {
        _settings = configReader;
        _logger.LogResponse("Building Call Vehicle. WSID: " + model.WSID);

        var element = XElement.Parse(model.Query);
        var vehicleElement = element.Element("Vehicle");

        if (vehicleElement != null)
        {
            var sequence = vehicleElement.Element("Sequence");
            if (sequence != null)
                model.RecordSequence = Convert.ToInt32(sequence.Value);
        }

        // Extract all fields
        var strLic = element.Element("Vehicle")?.Element("LIC")?.Value;
        var strLis = element.Element("Vehicle")?.Element("LIS")?.Value ?? "IL";
        var strLit = element.Element("Vehicle")?.Element("LIT")?.Value ?? "PC";
        var strLiy = element.Element("Vehicle")?.Element("LIY")?.Value;
        var strVma = element.Element("Vehicle")?.Element("VMA")?.Value;
        var strVin = element.Element("Vehicle")?.Element("VIN")?.Value;
        var strVyr = element.Element("Vehicle")?.Element("VYR")?.Value;

        // Assign to model
        model.Lic = strLic;
        model.Lis = strLis;
        model.Lit = strLit;
        model.Liy = strLiy;
        model.Vma = strVma;
        model.Vyr = strVyr;
        model.Vin = strVin;

        var transactionElements = new List<XElement>();
        var isVinOnlySearch = !string.IsNullOrWhiteSpace(model.Vin);

        if (isVinOnlySearch)
        {
            // ✅ VIN-only: include only VIN
            transactionElements.Add(new XElement("VIN", model.Vin));
            transactionElements.Add(new XElement("LIS", model.Lis ?? "IL"));
        }
        else
        {
            // ✅ Plate-based: include full set
            if (!string.IsNullOrWhiteSpace(model.Lic))
                transactionElements.Add(new XElement("LIC", model.Lic));
            transactionElements.Add(new XElement("LIS", model.Lis ?? "IL"));

            if (!string.IsNullOrEmpty(model.Liy) && model.Liy != "0")
                transactionElements.Add(new XElement("LIY", model.Liy));
            if (!string.IsNullOrEmpty(model.Lit))
                transactionElements.Add(new XElement("LIT", model.Lit));
            if (!string.IsNullOrEmpty(model.Vma))
                transactionElements.Add(new XElement("VMA", model.Vma));
            if (!string.IsNullOrEmpty(model.Vyr))
                transactionElements.Add(new XElement("VYR", model.Vyr));
        }

        //transactionElements.Add(new XElement("RSH", "N"));

        var mke = "Z5";
        if ((!string.IsNullOrWhiteSpace(model.Lis) && !model.Lis.Equals("IL", StringComparison.OrdinalIgnoreCase)) ||
            isVinOnlySearch)
            mke = "Z2";

        var xdocument = new XDocument(
            new XElement("OFML",
                new XElement("HDR",
                    new XElement("ID", model.QueryID),
                    new XElement("DAC", _settings.OriginatingCDCName),
                    new XElement("DAT", DateTime.Now.ToString("yyyyMMddHHmmss")),
                    new XElement("REF", model.QueryID),
                    new XElement("USR", model.StateUsername),
                    new XElement("ORI", _settings.ORI),
                    new XElement("MKE", mke)
                ),
                new XElement("TRN", transactionElements)
            )
        );

        model.RecordType = "V";
        model.Query = xdocument.ToString();

        _logger.LogResponse("Sending data message from " + model.StateUsername + " at workstation " + model.WSID);
        await GetCurrentClient().SendDataMessage(model);
    }

    public async Task BuildNameTransactionElements(QueryKey model)
    {
        _logger.LogResponse("Building Name Transaction Elements. WSID: " + model.WSID);

        var config = new ConfigurationReader().LoadSettings();
        var transactionElements = new List<XElement>();

        if (!string.IsNullOrWhiteSpace(model.Oln))
            model.Oln = model.Oln.Replace("-", "").Trim().ToUpper();

        if (string.IsNullOrWhiteSpace(model.Ols))
            model.Ols = "IL";

        bool hasOlnAndOls = !string.IsNullOrWhiteSpace(model.Oln) && !string.IsNullOrWhiteSpace(model.Ols);
        bool isOlsIL = string.Equals(model.Ols, "IL", StringComparison.OrdinalIgnoreCase);
        bool addedOln = false;

        // Option B
        if (hasOlnAndOls)
        {
            transactionElements.Add(new XElement("OLN", model.Oln));
            transactionElements.Add(new XElement("OLS", model.Ols));
            addedOln = true;
        }

        // Option A or C
        var fullName = $"{model.Lname}, {model.Fname} {model.Mname}".Trim().ToUpper();
        bool hasNameBasedData = !string.IsNullOrWhiteSpace(fullName) && !string.IsNullOrWhiteSpace(model.Dob);

        if (hasNameBasedData)
        {
            transactionElements.Add(new XElement("NAM", fullName));
            transactionElements.Add(new XElement("DOB", model.Dob));

            if (!string.IsNullOrWhiteSpace(model.Sex))
                transactionElements.Add(new XElement("SEX", model.Sex));

            if (!string.IsNullOrWhiteSpace(model.Race))
                transactionElements.Add(new XElement("RAC", model.Race.ToUpper()));

            if (!isOlsIL && !string.IsNullOrWhiteSpace(model.Ols))
                transactionElements.Add(new XElement("OLS", model.Ols));

            if (!addedOln && !string.IsNullOrWhiteSpace(model.Oln))
            {
                transactionElements.Add(new XElement("OLN", model.Oln));
                addedOln = true;
            }
        }

        if (!hasOlnAndOls && !hasNameBasedData)
        {
            _logger.LogResponse("Missing sufficient identifiers for out-of-state driver inquiry. Expect rejection.");
        }

        transactionElements.Add(new XElement("RSH", "N"));

        var document = new XDocument(
            new XElement("OFML",
                new XElement("HDR",
                    new XElement("ID", model.QueryID),
                    new XElement("DAC", config.OriginatingCDCName),
                    new XElement("DAT", DateTime.Now.ToString("yyyyMMddHHmmss")),
                    new XElement("REF", model.QueryID),
                    new XElement("USR", model.StateUsername),
                    new XElement("ORI", config.ORI),
                    new XElement("MKE", "Z2")
                ),
                new XElement("TRN", transactionElements)
            )
        );

        model.Query = document.ToString();

        _logger.LogResponse($"Sending data message from {model.StateUsername} at workstation {model.WSID}");
        await GetCurrentClient().SendDataMessage(model);
    }



    public async Task BuildLicTransactionElements(QueryKey model)
    {
        _logger.LogResponse("Building LIC Transaction Elements. WSID: " + model.WSID);
        var configurationReader = new ConfigurationReader();
        var settings = configurationReader.LoadSettings();

        var transactionElements = new List<XElement>();

        var isVinOnlySearch = !string.IsNullOrWhiteSpace(model.Vin);

        if (isVinOnlySearch)
        {
            // ✅ VIN-only request: only include VIN
            transactionElements.Add(new XElement("VIN", model.Vin));
            transactionElements.Add(new XElement("LIS", model.Lis ?? "IL"));
        }
        else
        {
            // ✅ Plate-based request
            if (!string.IsNullOrWhiteSpace(model.Lic))
                transactionElements.Add(new XElement("LIC", model.Lic));
            transactionElements.Add(new XElement("LIS", model.Lis ?? "IL"));

            if (!string.IsNullOrEmpty(model.Liy) && model.Liy != "0")
                transactionElements.Add(new XElement("LIY", model.Liy));
            if (!string.IsNullOrEmpty(model.Lit))
                transactionElements.Add(new XElement("LIT", model.Lit));
            if (!string.IsNullOrEmpty(model.Vma))
                transactionElements.Add(new XElement("VMA", model.Vma));
            if (!string.IsNullOrEmpty(model.Vyr))
                transactionElements.Add(new XElement("VYR", model.Vyr));
        }

        //transactionElements.Add(new XElement("RSH", "N"));

        var mke = "Z5";
        if ((!string.IsNullOrWhiteSpace(model.Lis) && !model.Lis.Equals("IL", StringComparison.OrdinalIgnoreCase)) ||
            isVinOnlySearch)
            mke = "Z2";

        _logger.LogResponse("Transaction elements added. MKE = " + mke);

        var xdocument = new XDocument(
            new XElement("OFML",
                new XElement("HDR",
                    new XElement("ID", model.QueryID),
                    new XElement("DAC", settings.OriginatingCDCName),
                    new XElement("DAT", DateTime.Now.ToString("yyyyMMddHHmmss")),
                    new XElement("REF", model.QueryID),
                    new XElement("USR", model.StateUsername),
                    new XElement("ORI", settings.ORI),
                    new XElement("MKE", mke)
                ),
                new XElement("TRN", transactionElements)
            )
        );

        _logger.LogResponse($"Here is the xDocument:{Environment.NewLine}{xdocument}");

        model.Query = xdocument.ToString();

        _logger.LogResponse("Sending data message from " + model.StateUsername + " at workstation " + model.WSID);

        await GetCurrentClient().SendDataMessage(model);
    }


    public async Task<QueryKey> BuildAdHocStateQuery(QueryKey model, Settings configReader)
    {
        _settings = configReader;
        _logger.LogResponse("Inside BuildAdHocStateQuery WSID: " + model.WSID);
        XDocument.Parse(model.Query);
        var queryName = XElement.Parse(model.Query).Element("StateQuery")?.Element("Name")?.Value;
        var strQueryFor = model.QueryFor;
        var dictionary = new Dictionary<string, string>();
        if (queryName != null && queryName.Equals("LIC", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!string.IsNullOrEmpty(model.QueryParams))
            {
                var queryParams = model.QueryParams;
                var chArray1 = new[] { '|' };
                foreach (var str2 in queryParams.Split(chArray1))
                {
                    var chArray2 = new[] { '=' };
                    var strArray = str2.Split(chArray2);
                    if (strArray.Length == 2)
                        dictionary[strArray[0]] = strArray[1];
                }

                dictionary.TryGetValue("LIC", out var strLic);
                dictionary.TryGetValue("FOR", out strQueryFor);
                dictionary.TryGetValue("LIS", out var strLis);
                dictionary.TryGetValue("LIT", out var strLit);
                dictionary.TryGetValue("LIY", out var strLiy);
                dictionary.TryGetValue("VIN", out var strVin);
                dictionary.TryGetValue("VMA", out var strVma);
                dictionary.TryGetValue("VYR", out var strVyr);
                model.QueryFor = strQueryFor;
                model.Lic = strLic;
                model.Lis = strLis;
                if (string.IsNullOrEmpty(strLit))
                    strLit = "PC";
                model.Lit = strLit;
                model.Liy = strLiy;
                model.Vin = strVin;
                model.Vma = strVma;
                model.Vyr = strVyr;
                if (string.IsNullOrEmpty(strQueryFor))
                    strQueryFor = model.QueryFor;
                model.RecordType = "V";
                JsonConvert.SerializeObject(model);
                try
                {
                    await BuildLicTransactionElements(model);
                }
                catch (Exception ex)
                {
                    _logger.LogResponse("Error calling BuildLicTransactionElements: " + ex.Message);
                }
            }
        }
        else if (queryName != null && queryName.Equals("NAM", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!string.IsNullOrEmpty(model.QueryParams))
            {
                var queryParams = model.QueryParams.Split('|');
                foreach (var param in queryParams)
                {
                    var chArray = new[] { '=' };
                    var strArray2 = param.Split(chArray);
                    if (strArray2.Length == 2)
                        dictionary[strArray2[0]] = strArray2[1];
                }

                dictionary.TryGetValue("LNAM", out var strLnam);
                dictionary.TryGetValue("GIV1", out var strGiv1);
                dictionary.TryGetValue("GIV2", out var strGiv2);
                dictionary.TryGetValue("DOB", out var strDob);
                dictionary.TryGetValue("SEX", out var strSex);
                dictionary.TryGetValue("RAC", out var strRac);
                dictionary.TryGetValue("OLN", out var strOln);
                dictionary.TryGetValue("OLS", out var strOls);
                dictionary.TryGetValue("FOR", out strQueryFor);
                dictionary.TryGetValue("NOH", out var strNoh);
                model.QueryFor = strQueryFor;
                model.Lname = strLnam;
                model.Fname = strGiv1;
                model.Mname = strGiv2;
                model.Dob = strDob;
                model.Sex = strSex;
                model.Race = strRac;
                model.Oln = strOln;
                model.Ols = strOls;
                model.Noh = strNoh;
                if (string.IsNullOrEmpty(strQueryFor))
                {
                    var queryFor2 = model.QueryFor;
                }

                model.Query = queryParams.ToString();
                model.RecordType = "P";
                await BuildNameTransactionElements(model);
            }
        }
        else if (queryName != null && queryName.Equals("NSX", StringComparison.InvariantCultureIgnoreCase))
        {
            var queryParams = model.QueryParams.Split('|');
            foreach (var param in queryParams)
            {
                var chArray = new[] { '=' };
                var strArray4 = param.Split(chArray);
                if (strArray4.Length == 2)
                    dictionary[strArray4[0]] = strArray4[1];
            }

            dictionary.TryGetValue("NAM", out var strNam);
            dictionary.TryGetValue("PNO", out var strPno);
            dictionary.TryGetValue("FOR", out strQueryFor);
            if (string.IsNullOrEmpty(strQueryFor))
                strQueryFor = model.QueryFor;
            model.Nam = strNam;
            model.Pno = strPno;
            model.QueryFor = strQueryFor;
            model.Query = queryParams.ToString();
            model.RecordType = "I";
            await BuildTransactionElements(model, configReader);
        }
        else if (queryName != null && queryName.Equals("ART", StringComparison.InvariantCultureIgnoreCase))
        {
            var strArray5 = model.QueryParams.Split('|');
            foreach (var str23 in strArray5)
            {
                var chArray = new[] { '=' };
                var strArray6 = str23.Split(chArray);
                if (strArray6.Length == 2)
                    dictionary[strArray6[0]] = strArray6[1];
            }

            dictionary.TryGetValue("TYP", out var strTyp);
            dictionary.TryGetValue("SER", out var strSer);
            dictionary.TryGetValue("OAN", out var strOan);
            dictionary.TryGetValue("SIGHTED", out var strSighted);
            dictionary.TryGetValue("ID", out var strId);
            dictionary.TryGetValue("QUERYENTITY", out var strQueryEntity);
            dictionary.TryGetValue("FOR", out strQueryFor);
            model.QueryFor = strQueryFor;
            model.ArticleType = strTyp;
            model.ArticleSerial = strSer;
            model.OwnerNumber = strOan;
            model.Sighted = strSighted == "TRUE";
            model.ArticleId = strId;
            model.QueryEntity = strQueryEntity;
            model.Query = strArray5.ToString();
            model.RecordType = "A";
            await BuildTransactionElements(model, configReader);
        }
        else if (queryName != null && queryName.Equals("FOID", StringComparison.InvariantCultureIgnoreCase))
        {
            var queryParams = model.QueryParams.Split('|');
            foreach (var str30 in queryParams)
            {
                var chArray = new[] { '=' };
                var strArray8 = str30.Split(chArray);
                if (strArray8.Length == 2)
                    dictionary[strArray8[0]] = strArray8[1];
            }

            dictionary.TryGetValue("LNAM", out var lnam);
            dictionary.TryGetValue("GIV1", out var fname);
            dictionary.TryGetValue("GIV2", out var mname);
            dictionary.TryGetValue("DOB", out var dob);
            dictionary.TryGetValue("SEX", out var sex);
            dictionary.TryGetValue("FOID", out var foid);
            dictionary.TryGetValue("FOR", out strQueryFor);
            model.Lname = lnam;
            model.Fname = fname;
            model.Mname = mname;
            model.DOB = ParseDob(dob);
            model.Sex = sex;
            model.FOID = foid;
            model.QueryFor = strQueryFor;
            model.Query = queryParams.ToString();
            model.RecordType = "F";
            await BuildTransactionElements(model, configReader);
        }
        else if (queryName != null && queryName.Equals("GUN", StringComparison.InvariantCultureIgnoreCase))
        {
            var strArray9 = model.QueryParams.Split('|');
            foreach (var str37 in strArray9)
            {
                var chArray = new[] { '=' };
                var strArray10 = str37.Split(chArray);
                if (strArray10.Length == 2)
                    dictionary[strArray10[0]] = strArray10[1];
            }

            dictionary.TryGetValue("SER", out var str38);
            dictionary.TryGetValue("MAK", out var str39);
            dictionary.TryGetValue("CAL", out var str40);
            dictionary.TryGetValue("FOR", out strQueryFor);
            model.ArticleType = str38;
            model.ArticleSerial = str39;
            model.OwnerNumber = str40;
            model.QueryFor = strQueryFor;
            model.Query = strArray9.ToString();
            model.RecordType = "G";
            await BuildTransactionElements(model, configReader);
        }

        return new QueryKey();
    }

    private async Task BuildTransactionElements(QueryKey model, Settings configReader)
    {
        _settings = configReader;
        _logger.LogResponse("Building Transaction Elements WSID: " + model.WSID);
        var transactionElements = new List<XElement>();
        var mke = "";
        var concatenatedName = model.Lname + ", " + model.Fname;
        if (concatenatedName.Length > 30)
            concatenatedName = model.Lname + ", " + model.Fname[0];
        switch (model.RecordType)
        {
            case "I":
                mke = "ZWS";
                if (!string.IsNullOrEmpty(model.Nam))
                    transactionElements.Add(new XElement("NAM", model.Nam));
                if (!string.IsNullOrEmpty(model.Pno)) transactionElements.Add(new XElement("PNO", model.Pno));
                break;
            case "A":
                mke = "QA";
                if (!string.IsNullOrEmpty(model.ArticleType))
                    transactionElements.Add(new XElement("TYP", model.ArticleType));
                if (!string.IsNullOrEmpty(model.ArticleSerial))
                    transactionElements.Add(new XElement("SER", model.ArticleSerial));
                if (!string.IsNullOrEmpty(model.OwnerNumber))
                    transactionElements.Add(new XElement("OAN", model.OwnerNumber));
                if (!string.IsNullOrEmpty(model.Sighted.ToString()))
                    transactionElements.Add(new XElement("SIGHTED", model.Sighted.ToString()));
                if (!string.IsNullOrEmpty(model.ArticleId))
                    transactionElements.Add(new XElement("ID", model.ArticleId));
                if (!string.IsNullOrEmpty(model.QueryEntity))
                    transactionElements.Add(new XElement("QUERYENTITY", model.QueryEntity));
                break;
            case "F":
                mke = "ZF";
                if (!string.IsNullOrEmpty(concatenatedName))
                    transactionElements.Add(new XElement("NAM", concatenatedName.ToUpper()));
                if (!string.IsNullOrEmpty(model.DOB))
                    transactionElements.Add(new XElement("DOB", model.DOB));
                if (!string.IsNullOrEmpty(model.Sex))
                    transactionElements.Add(new XElement("SEX", model.Sex));
                if (!string.IsNullOrEmpty(model.FOID))
                    transactionElements.Add(new XElement("FID", model.FOID.ToUpper()));
                break;
            case "G":
                mke = "QG";
                if (!string.IsNullOrEmpty(model.GunSerial))
                    transactionElements.Add(new XElement("SER", model.GunSerial));
                if (!string.IsNullOrEmpty(model.GunMake))
                    transactionElements.Add(new XElement("MAK", model.GunMake));
                if (!string.IsNullOrEmpty(model.GunCaliber))
                    transactionElements.Add(new XElement("CAL", model.GunCaliber));
                break;
        }

        var objArray = new object[]
        {
            new XElement("OFML",
                new XElement("HDR", new XElement("ID", model.QueryID),
                    new XElement("DAC", _settings.OriginatingCDCName),
                    new XElement("DAT", DateTime.Now.ToString("yyyyMMddHHmmss")), new XElement("REF", model.QueryID),
                    new XElement("USR", model.StateUsername), new XElement("ORI", _settings.ORI),
                    new XElement("MKE", mke)), new XElement("TRN", transactionElements))
        };
        model.Query = new XDocument(objArray).ToString();
        _logger.LogResponse("Sending data message from " + model.StateUsername + " at workstation " + model.WSID);
        await GetCurrentClient().SendDataMessage(model);
    }
}