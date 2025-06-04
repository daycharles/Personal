using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using PSIMSLeads.PSIMSLeads;

namespace PSIMSLeads;

public class PSIMSLeadsParse
{
    // *** PHOTO ID RESPONSE ***

    private Logger _logger;
    private ConfigurationReader _config;

    public PSIMSLeadsParse(Logger logger)
    {
        _logger = logger;
        _config = new ConfigurationReader();
    }

    public async Task<XElement?> ParseStateData(XDocument responseDoc, QueryKey model, XDocument coreDoc)
    {
        try
        {
            var responseText = coreDoc.Element("OFML")?.Element("RSP")?.Element("TXT")?.Value;

            if (string.IsNullOrWhiteSpace(responseText))
            {
                _logger.LogResponse("Response TXT is empty.");
                return null;
            }

            var cleanedResponseText = CleanInvalidXmlChars(responseText);

            var responseTextLines = cleanedResponseText.Split(["\r\n", "\n"], StringSplitOptions.None)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrEmpty(line))
                .ToArray();

            if (responseTextLines.Length == 0)
            {
                _logger.LogResponse("No lines found in the RSP TXT.");
                return null;
            }

            _logger.LogResponse($"Total Lines Found: {responseTextLines.Length}");

            for (var i = 0; i < responseTextLines.Length; i++)
            {
                var line = responseTextLines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                _logger.LogResponse($"Line {i + 1}: {line}");

                if (line.StartsWith("SOS "))
                {
                    // It's an SOS query — parse it
                    var parsedRecord = await ParseSOS(cleanedResponseText, model);
                    return parsedRecord;
                }

                if (line.StartsWith("CHF "))
                {
                    if (!line.Contains("LIC/") && !line.Contains("NAM/"))
                    {
                        // CHF without LIC or NAM -> exit (not valid for parsing)
                        return null;
                    }
                }

                if (line.StartsWith("NCIC RESPONSE"))
                {
                    // Not an SOS or CHF record -> skip
                    return null;
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogResponse($"Error Parsing state data: {e.Message}\n{e.StackTrace}");
            return null;
        }

        return null;
    }


    private Task<XElement?> ParseSOS(string response, QueryKey model)
    {
        var list = response.Split([
            "\r\n",
            "\n"
        ], StringSplitOptions.None).Select(line => line.Trim()).ToList();
        if (list.Count == 0)
            return Task.FromResult<XElement>(null);
        var strSticker = string.Empty;
        var recordType = model.RecordType;
        var queryType = recordType == "P" ? "TARGET" : "OWNER";
        var result = new XElement("Record");
        for (var index1 = 0; index1 < list.Count; ++index1)
        {
            if (list[index1].StartsWith("DL/"))
            {
                var num = index1 + 4;
                if (num >= list.Count)
                    return Task.FromResult<XElement>(null);
                var index2 = num + 1;
                if (index2 >= list.Count)
                    return Task.FromResult<XElement>(null);
                var name2 = list[index2];
                if (index2 >= list.Count)
                    return Task.FromResult<XElement>(null);
                var index3 = index2 + 1;
                if (index3 >= list.Count)
                    return Task.FromResult<XElement>(null);
                var strAddress = list[index3];
                if (index3 + 1 >= list.Count)
                    return Task.FromResult<XElement>(null);
                var strSex = GetKeyValue(response, "SEX/");
                var strDOB = FormatDOB(GetKeyValue(response, "DOB/"));
                var strHeight = FormatHeight(GetKeyValue(response, "HGT/"));
                var strWeight = GetKeyValue(response, "WGT/");
                var strHairColor = GetKeyValue(response, "HAI/");
                var strEyeColor = GetKeyValue(response, "EYE/");
                var strOLN = GetKeyValue(response, "OLN/");
                var strName = NormalizeName(name2);
                model.Nam = strName;
                model.Sex = strSex;
                model.DOB = strDOB;
                model.Oln = strOLN;
                var targetElement = new XElement(queryType, [
                    new XElement("Data", "Y"),
            new XElement("Name", strName),
            new XElement("Address", strAddress),
            new XElement("Sex", strSex),
            new XElement("Height", strHeight),
            new XElement("Weight", strWeight),
            new XElement("DOB", strDOB),
            new XElement("HairColor", strHairColor),
            new XElement("EyeColor", strEyeColor),
            new XElement("OLN", strOLN)
                ]);
                result.Add(targetElement);
                return Task.FromResult(result);
            }

            if (list[index1].StartsWith("STA/"))
            {
                if (!list[index1].EndsWith("STA/VALID", StringComparison.OrdinalIgnoreCase))
                    _logger.LogResponse("Adding Warning to Plate: " + list[index1]);
                var num1 = index1 + 1;
                if (num1 >= list.Count)
                    return Task.FromResult<XElement>(null);
                var index4 = num1 + 1;
                if (index4 >= list.Count)
                    return Task.FromResult<XElement>(null);
                if (list[index4].Contains("STX/"))
                    strSticker = ExtractSticker(list[index4]);
                var index5 = index4 + 1;
                if (index5 >= list.Count)
                    return Task.FromResult<XElement>(null);
                var str = list[index5];
                var num2 = index5 + 1;
                if (num2 >= list.Count)
                    return Task.FromResult<XElement>(null);
                var index6 = num2 + 1;
                if (index6 >= list.Count)
                    return Task.FromResult<XElement>(null);
                var strOwner = str + "  " + list[index6];
                var num3 = index6 + 1;
                if (num3 >= list.Count)
                    return Task.FromResult<XElement>(null);
                var index7 = num3 + 1;
                if (index7 >= list.Count)
                    return Task.FromResult<XElement>(null);
                var (Year, Make, Type, Color1, Color2, VIN) = ExtractVehicleInfo(list[index7]);
                var strVehicleMake = Make;
                var strType = Type;
                var strColor1 = Color1;
                var strColor2 = Color2;
                var strVIN = VIN;
                var strVehicleYear = Year;
                var plateElement = new XElement("Plate", [
                    new XElement("Data", "Y"),
                    new XElement("Sticker", strSticker),
                    new XElement("Owner", strOwner),
                    new XElement("VIN", strVIN),
                    new XElement("VehicleYear", strVehicleYear),
                    new XElement("Make", strVehicleMake),
                    new XElement("Color1", strColor1),
                    new XElement("Color2", strColor2),
                    new XElement("VehicleType", strType)
                ]);
                result.Add(plateElement);
                return Task.FromResult(result);
            }
        }
        return Task.FromResult<XElement>(null);
    }

    private string NormalizeName(string name)
    {
        name = name.ToUpper();
        if (name.StartsWith("VAN DER ")) name = "VANDER" + name.Substring(8).TrimStart();
        if (name.StartsWith("VAN ")) name = "VAN" + name.Substring(4).TrimStart();
        if (name.StartsWith("MAC ")) name = "MAC" + name.Substring(4).TrimStart();
        if (name.StartsWith("MC ")) name = "MC" + name.Substring(3).TrimStart();
        if (name.StartsWith("DE ")) name = "DE" + name.Substring(3).TrimStart();
        if (name.StartsWith("DI ")) name = "DI" + name.Substring(3).TrimStart();
        return name;
    }

    private string GetKeyValue(string response, string key)
    {
        var startIndex = response.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (startIndex == -1) return string.Empty;

        startIndex += key.Length;
        var endIndex = response.IndexOf(' ', startIndex);
        if (endIndex == -1) endIndex = response.Length;

        return response.Substring(startIndex, endIndex - startIndex).Trim();
    }

    private string FormatDOB(string dob)
    {
        if (dob.Length == 8)
            return $"{dob.Substring(0, 2)}/{dob.Substring(2, 2)}/{dob.Substring(4, 4)}";
        if (dob.Length == 6)
        {
            var year = DateTime.Now.Year - 100 + int.Parse(dob.Substring(4, 2));
            return $"{dob.Substring(0, 2)}/{dob.Substring(2, 2)}/{year}";
        }

        return dob;
    }

    private string FormatHeight(string height)
    {
        // Find the position of the single quote (')
        var nHPos = height.IndexOf('\'');
        if (nHPos == -1) throw new ArgumentException("Invalid height format");

        // Extract the feet and inches parts
        var feetPart = height.Substring(0, nHPos);
        var inchesPart = height.Substring(nHPos + 1).Replace("\"", "").Trim();

        // Convert to integers
        if (!int.TryParse(feetPart, out var feet)) throw new ArgumentException("Invalid feet format");

        if (!int.TryParse(inchesPart, out var inches)) throw new ArgumentException("Invalid inches format");

        // Format the height as a numeric string
        var formattedHeight = $"{feet}{inches:00}";

        return formattedHeight;
    }

    private string ExtractSticker(string line)
    {
        var stxIndex = line.IndexOf("STX/", StringComparison.OrdinalIgnoreCase);
        if (stxIndex == -1) return string.Empty;

        var sticker = line.Substring(stxIndex + 4).Trim();
        var spaceIndex = sticker.IndexOf(' ');
        return spaceIndex == -1 ? sticker : sticker.Substring(0, spaceIndex);
    }

    private (string Year, string Make, string Type, string Color1, string Color2, string VIN) ExtractVehicleInfo(string lines)
    {
        if (string.IsNullOrWhiteSpace(lines))
            return (string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);


        var tokens = lines.Split([' '], StringSplitOptions.RemoveEmptyEntries)
                          .Select(line => line.Trim())
                          .Where(line => !string.IsNullOrEmpty(line))
                          .ToList();

        var vin = string.Empty;
        var color1 = string.Empty;
        var color2 = string.Empty;
        var year = string.Empty;
        var make = string.Empty;
        var type = string.Empty;

        var index = 0;

        while (index < tokens.Count)
        {
            vin = tokens[index];

            index++;
            if (index >= lines.Length) return (string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

            // Check if next token is Color/Color (optional)
            if (tokens[index].Contains('/'))
            {
                var colors = tokens[index].Split('/');
                color1 = colors.Length > 0 ? colors[0] : string.Empty;
                color2 = colors.Length > 1 ? colors[1] : string.Empty;
                index++;
            }

            if (index >= lines.Length) return (string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

            // Check if next token is Year (optional)
            if (tokens[index].Length == 4 && int.TryParse(tokens[index], out _))
            {
                year = tokens[index];
                index++;
            }

            if (index >= lines.Length) return (string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

            // Remaining tokens could be make and type
            if (tokens[index].Length > 1)
            {
                make = tokens[index];
                index++;
            }

            if (index >= lines.Length) return (string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

            if (index < tokens.Count)
            {
                type = tokens[index];
                index++;
            }


        }

        return (year, make, type, color1, color2, vin);
    }


    #region SOS Test string `testParseSOS`

    private string testParseSOSVehicle = "SOS  20250317  080743 \r" +
                                   " \r" +
                                   "STA/VALID VAL/03142023 TTL/23081700460 \r" +
                                   " \r" +
                                   "DV92737 032024 ORIG PLT LIC STX/4C9119819 \r" +
                                   "DANIELS SIERRA C \r" +
                                   " \r" +
                                   "311 N WEST ST APT 4 JACKSONVILLE IL 62650 \r" +
                                   " \r" +
                                   "3GYFNCE39DS594841 BLK/BLK 2013 CADILLAC UTILITY  \r" +
                                   "REGISTRATION IS FROM THE VEHICLE MASTER FILE  \r" +
                                   "END \r" +
                                   " \r";

    private string testParseSOS1 = "SOS  20250317  080743 \r" +
                                   " \r" +
                                   "DL/IP STA/VALID \r" +
                                   "TDL/TIP STA/SEE ILOLNHELP \r" +
                                   "CDL/CIP STA/VALID CDL MEDICALLY CERTIFIED \r" +
                                   "SCHLBUS STA/NOT A SCHOOL BUS DRIVER (SEE ILOLNHELP) \r" +
                                   " \r" +
                                   "PUBLIC JOHN Q  \r" +
                                   "123 S NORTH  SPRINGFIELD  62702 \r" +
                                   "SEX/M DOB/08061954 HGT/6'02\" WGT/200 HAI/BRO EYE/BLU  \r" +
                                   "OLN/P142-4755-4223  OLC/D* OLT/ORIGINAL EXP/08062025 ISS/08052021  \r" +
                                   "RES-PID CLASS/NONE \r" +
                                   "NO STOPS IN EFFECT \r" +
                                   "03 CONV LAST 12 MO \r" +
                                   "CONV 06242024  11-601                CHAM \r" +
                                   "CONV 06242024  11-1204               CHAM \r" +
                                   "CONV 06052024  11-601                CHAM \r" +
                                   "END \r" +
                                   " \r";

    // Typical Response
    private string testParseSOS2 = "SOS  20250317  0753 \r" +
                                   " \r" +
                                   "DL/IP STA/VALID \r" +
                                   "TDL/TIP STA/SEE ILOLNHELP \r" +
                                   "CDL/CIP STA/SEE ILOLNHELP  \r" +
                                   " \r" +
                                   "PUBLIC JOHN Q  \r" +
                                   "ANY STREET HOMETOWN 62629 \r" +
                                   "SEX/M DOB/08061954 HGT/6'02\" WGT/200 HAI/BRO EYE/BLU  \r" +
                                   "OLN/P142-4755-4223  OLC/D* OLT/ORIGINAL EXP/08062025 ISS/08052021  \r" +
                                   "RES-PID CLASS/NONE \r" +
                                   "NO STOPS IN EFFECT \r" +
                                   "01 CONV LAST 12 MO \r" +
                                   "CONV 012321 I 11-601                LAKE \r" +
                                   "END \r" +
                                   " \r";

    // CDL Response
    private string testParseSOS3 = "SOS  20250317  0753 \r" +
                                   " \r" +
                                   "DL/IP STA/VALID \r" +
                                   "TDL/TIP STA/SEE ILOLNHELP \r" +
                                   "CDL/CIP STA/VALID COMMERCIAL DRIVERS LICENSE \r" +
                                   " \r" +
                                   "LARGE MARGARET T  \r" +
                                   "ANY STREET HOMETOWN 62629 \r" +
                                   "SEX/F DOB/01051952 HGT/6’01 WGT/285 HAI/BLN EYE/BLU  \r" +
                                   "OLN/L142-4755-2801 OLC/A* OLT/ORIG EXP/01051998 ISS/04051994  \r" +
                                   "RES-PID CLASS/NONE \r" +
                                   "ENDORSEMENTS/COMBINATION HAZARDOUS MATERIALS AND TANK VEHICLE  \r" +
                                   "NO STOPS IN EFFECT \r" +
                                   "NO CONV LAST 12 MO \r" +
                                   "END \r" +
                                   " \r";

    // Driver Control Response
    private string testParseSOS4 = "SOS NO VALID ILLINOIS LICENSE  20250317  0753 \r" +
                                   " \r" +
                                   "DL/IP STA/NO VALID IL LIC  \r" +
                                   "TDL/TIP STA/SEE ILOLNHELP \r" +
                                   "CDL/CIP STA/VALID COMMERCIAL DRIVERS LICENSE \r" +
                                   "SCHLBUS STA/NOT A SCHOOL BUS DRIVER (SEE ILOLNHELP) \r" +
                                   " \r" +
                                   "PUBLIC JOHN Q  \r" +
                                   "123 TESTING DR JOLIET 60433 \r" +
                                   "SEX/M DOB/01112002 HAI/OTH EYE/OTH   \r" +
                                   "OLN/P142-4750-2011  \r" +
                                   "RES-PID CLASS/NONE \r" +
                                   "NO STOPS IN EFFECT \r" +
                                   "NO CONV LAST 12 MO \r" +
                                   "STOP 08172019 3-707(C)1 \r" +
                                   "END \r" +
                                   " \r";

    // Special responses Example A
    private string testParseSOS5 = "SOS 20250317  0753 \r" +
                                   "SOS21 INQ YIELDED FOLLOWING DUP REC \r" +
                                   " \r" +
                                   "DL/IP STA/VALID  \r" +
                                   "TDL/TIP STA/SEE ILOLNHELP \r" +
                                   "CDL/CIP STA/SEE ILOLNHELP \r" +
                                   " \r" +
                                   "PUBLIC JANE Q  \r" +
                                   "321 S 1ST AVE MAYWOOD 60153 \r" +
                                   "SEX/F DOB/021253 HGT/5’03 WGT/145 HAI/BRO EYE/BRO  \r" +
                                   "OLN/P520-5432-3214 OLC/D* OLT/DUP EXP/12021998 ISS/02191994  \r" +
                                   "RES-PID CLASS/NONE \r" +
                                   "NO STOPS IN EFFECT \r" +
                                   "NO CONV LAST 12 MO \r" +
                                   "END \r" +
                                   " \r" +
                                   " \r" +
                                   "MORE SOS DATA TO FOLLOW \r" +
                                   " \r" +
                                   " \r" +
                                   "SOS 20250317  0753 \r" +
                                   "SOS21 INQ YIELDED FOLLOWING DUP REC \r" +
                                   " \r" +
                                   "DL/IP STA/SUSPENDED  \r" +
                                   "TDL/TIP STA/SEE ILOLNHELP \r" +
                                   "CDL/CIP STA/SEE ILOLNHELP \r" +
                                   " \r" +
                                   "DOE JANE  \r" +
                                   "666 MCBRIDE STREET CAIRO 62914 \r" +
                                   "SEX/F DOB/021253 HGT/5’01 WGT/152 HAI/BLK EYE/BRO  \r" +
                                   "OLN/D520-5041-3943 OLC/D* OLT/ORIG EXP/12021998 ISS/11271994  \r" +
                                   "RES-PID CLASS/NONE \r" +
                                   "1 STOPS IN EFFECT \r" +
                                   "NO CONV LAST 12 MO \r" +
                                   "SUSP 12261994 I 11-501(A)1 \r" +
                                   "END \r";

    #endregion

    #region Utility Methods

    public static string CleanInvalidXmlChars(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        var stringBuilder = new StringBuilder();
        foreach (var ch in text)
            if (ch == '\t' || ch == '\n' || ch == '\r' || (ch >= ' ' && ch <= '\uD7FF') ||
                (ch >= '\uE000' && ch <= '�'))
                stringBuilder.Append(ch);
        return stringBuilder.ToString();
    }

    #endregion
}