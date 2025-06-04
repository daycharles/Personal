using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Newtonsoft.Json;
using PSIMSLeads.PSIMSLeads;

namespace PSIMSLeads;

public class PSIMSCore : IDisposable
{
    public static PSIMSCore _core;
    private CancellationTokenSource _cancellationTokenSource;
    private TcpClient _client;
    private bool _endCoreRecv;
    private bool _isConnected;
    private PSIMSLeadsQuery _leadsQuery;
    private Logger _logger;
    private SemaphoreSlim _semaphore;
    private Settings _settings;
    internal NetworkStream _stream;
    private RichTextBox _textLog;

    public PSIMSCore(bool endCoreRecv, RichTextBox textLog, Logger logger)
    {
        _logger = logger;
        _leadsQuery = new PSIMSLeadsQuery(textLog, logger);
        _endCoreRecv = endCoreRecv;
        var configReader = new ConfigurationReader();
        _settings = configReader.LoadSettings();
        _textLog = textLog;
        _core = this;
        _cancellationTokenSource = new CancellationTokenSource();
        _semaphore = new SemaphoreSlim(10);
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _client?.Dispose();
        _stream?.Dispose();
        _cancellationTokenSource?.Dispose();
        _semaphore?.Dispose();
    }

    public static PSIMSCore GetCoreClient()
    {
        return _core;
    }

    public async Task StartAsync(string coreAddr, int corePort)
    {
        try
        {
            _logger.LogResponse("Core Communication Started");
            _client = new TcpClient();
            await _client.ConnectAsync(coreAddr, corePort);
            _stream = _client.GetStream();
            _isConnected = true;
            _logger.LogResponse($"Core Communication Connected {coreAddr}:{corePort}");
            await ListenForDataAsync(_cancellationTokenSource.Token);
            await SendCoreCommandAsync(1, null);
        }
        catch (Exception ex)
        {
            _logger.LogResponse("Error: " + ex.Message);
        }
        finally
        {
            _isConnected = false;
            _client?.Close();
            _logger.LogResponse("Core Communication Ended");
        }
    }

    public async Task ListenForDataAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        try
        {
            while (!_endCoreRecv && _isConnected && !cancellationToken.IsCancellationRequested)
            {
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (bytesRead > 0)
                {
                    byte[] data = new byte[bytesRead];
                    Array.Copy(buffer, data, bytesRead);

                    await _semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        await ProcessDataAsync(data, cancellationToken);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogResponse("ListenForDataAsync error: " + ex.Message);
        }
    }

    private async Task ProcessDataAsync(byte[] data, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogResponse($"Processing data packet of {data.Length} bytes");
            if (data.Length < 145)
                _logger.LogResponse($"Received packet too small: {data.Length} bytes");
            else
                await ParseCoreDataPacket(data, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogResponse("CORE:(ProcessDataAsync) Error processing data: " + ex.Message + "\n" + ex.StackTrace);
        }
    }

    public async Task ParseCoreDataPacket(byte[] data, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogResponse("Entering ParseCoreDataPacket" + Environment.NewLine);
            _logger.LogResponse(string.Format("Request from {0}{1}Request Response: {2}",
                Encoding.ASCII.GetString(data, 9, 128).TrimEnd(new char[1]), Environment.NewLine,
                BitConverter.ToInt32(data, 137)));
            var packet = new CoreDataPacket
            {
                ucType = data[0],
                nFromService = BitConverter.ToInt32(data, 1),
                nToService = BitConverter.ToInt32(data, 5),
                aClientAddress = Encoding.ASCII.GetString(data, 9, 128).TrimEnd(new char[1]),
                nRequestResponse = BitConverter.ToInt32(data, 137),
                nDataLen = BitConverter.ToInt32(data, 141),
                aData = new byte[BitConverter.ToInt32(data, 141)]
            };
            JsonConvert.SerializeObject(packet);
            Array.Copy(data, 145, packet.aData, 0, packet.aData.Length);
            await ProcessCoreData(packet, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error parsing CoreDataPacket: " + ex.Message);
        }
    }

    private async Task ProcessCoreData(CoreDataPacket packet, CancellationToken cancellationToken)
    {
        var xmlFromData = ExtractXmlFromData(packet.aData);
        var empty = string.Empty;
        _logger.LogResponse("Entering ParseCoreDataPacket" + Environment.NewLine + xmlFromData);
        XElement xelement;
        if (xmlFromData != null)
        {
            var text = xmlFromData.Trim();
            if (xmlFromData.StartsWith("<?xml"))
            {
                var num = xmlFromData.IndexOf("?>", StringComparison.Ordinal);
                if (num >= 0)
                    text = xmlFromData.Substring(num + 2).Trim();
            }

            xelement = XElement.Parse(CleanInvalidXmlChars(text).Trim().Replace("\r", ""));
        }
        else
        {
            xelement = XElement.Parse(Encoding.UTF8.GetString(packet.aData));
        }

        var str1 = xelement.Element("Control")?.Element("WSID")?.Value;
        var str2 = xelement.Element("Control")?.Element("Agency")?.Value;
        var str3 = xelement.Element("Control")?.Element("StateUserName")?.Value;
        var str4 = xelement.Element("Control")?.Element("UserName")?.Value;
        var str5 = xelement.Element("Control")?.Element("Badge")?.Value;
        var str6 = xelement.Element("StateQuery")?.Element("Parameters").Value;
#if DEBUG
        var queryId = "8a90397e9d";
#else
        var queryId = Guid.NewGuid().ToString("N").Substring(0, 10);
#endif
        var model = new QueryKey
        {
            Agency = Convert.ToInt32(str2),
            WSID = str1,
            StateUsername = str3,
            Username = str4,
            LeadsService = packet.nToService,
            Query = xelement.ToString(),
            QueryParams = str6,
            Badge = str5,
            QueryFor = str5 + ":" + str4,
            ToService = packet.nFromService,
            QueryID = queryId,
            RecordSequence = 1
        };
        switch (packet.nRequestResponse)
        {
            case 107:
            case 605:
            case 1011:
                var str8 = IdentifyRequestType(xelement.ToString());
                if (str8 == null)
                    break;
                switch (str8.Length)
                {
                    case 5:
                        if (str8 != "Plate")
                            return;
                        _logger.LogResponse("CORE:(ProcessCoreData) Ad Hoc Query send to PSIMSLeadsCMD" + Environment.NewLine);
                        model.QueryType = "Plate";
                        model.RequestResponse = 2106;
                        JsonConvert.SerializeObject(model);
                        _logger.LogResponse("Entering BuildAdHocStateQuery with WSID " + model.WSID);
                        var queryKey1 = await _leadsQuery.BuildAdHocStateQuery(model, _settings);
                        return;
                    case 6:
                        if (str8 != "Person")
                            return;
                        _logger.LogResponse("CORE:(ProcessCoreData) Ad Hoc Query send to PSIMSLeadsCMD" + Environment.NewLine);
                        model.QueryType = "Person";
                        model.RequestResponse = 2106;
                        JsonConvert.SerializeObject(model);
                        _logger.LogResponse("Entering BuildAdHocStateQuery with WSID " + model.WSID);
                        var queryKey2 = await _leadsQuery.BuildAdHocStateQuery(model, _settings);
                        return;
                    case 7:
                        switch (str8[4])
                        {
                            case 'A':
                                if (str8 != "PSA ART")
                                    return;
                                model.RequestResponse = 2106;
                                _logger.LogResponse("CORE:(ProcessCoreData) Ad Hoc Query send to PSIMSLeadsCMD" +
                                            Environment.NewLine);
                                model.QueryType = "PSA ART";
                                JsonConvert.SerializeObject(model);
                                _logger.LogResponse("Entering BuildAdHocStateQuery with WSID " + model.WSID);
                                var queryKey3 = await _leadsQuery.BuildAdHocStateQuery(model, _settings);
                                return;
                            case 'G':
                                if (str8 != "PSA GUN")
                                    return;
                                model.RequestResponse = 2106;
                                _logger.LogResponse("CORE:(ProcessCoreData) Ad Hoc Query send to PSIMSLeadsCMD" +
                                            Environment.NewLine);
                                model.QueryType = "PSA GUN";
                                JsonConvert.SerializeObject(model);
                                _logger.LogResponse("Entering BuildAdHocStateQuery with WSID " + model.WSID);
                                var queryKey4 = await _leadsQuery.BuildAdHocStateQuery(model, _settings);
                                return;
                            case 'N':
                                if (str8 != "PSA NSX")
                                    return;
                                model.RequestResponse = 2106;
                                _logger.LogResponse("CORE:(ProcessCoreData) Ad Hoc Query send to PSIMSLeadsCMD" +
                                            Environment.NewLine);
                                model.QueryType = "PSA NSX";
                                JsonConvert.SerializeObject(model);
                                _logger.LogResponse("Entering BuildAdHocStateQuery with WSID " + model.WSID);
                                var queryKey5 = await _leadsQuery.BuildAdHocStateQuery(model, _settings);
                                return;
                            default:
                                return;
                        }
                    case 8:
                        if (str8 != "PSA FOID")
                            return;
                        model.RequestResponse = 2106;
                        _logger.LogResponse("CORE:(ProcessCoreData) Ad Hoc Query send to PSIMSLeadsCMD" + Environment.NewLine);
                        model.QueryType = "PSA FOID";
                        JsonConvert.SerializeObject(model);
                        _logger.LogResponse("Entering BuildAdHocStateQuery with WSID " + model.WSID);
                        var queryKey6 = await _leadsQuery.BuildAdHocStateQuery(model, _settings);
                        return;
                    case 9:
                        return;
                    case 10:
                        if (str8 != "PSA Person")
                            return;
                        _logger.LogResponse("CORE:(ProcessCoreData) Ad Hoc Query send to PSIMSLeadsCMD" + Environment.NewLine);
                        model.QueryType = "PSA Person";
                        model.RequestResponse = 2106;
                        JsonConvert.SerializeObject(model);
                        _logger.LogResponse("Entering BuildAdHocStateQuery with WSID " + model.WSID);
                        var queryKey7 = await _leadsQuery.BuildAdHocStateQuery(model, _settings);
                        return;
                    case 11:
                        if (str8 != "PSA License")
                            return;
                        _logger.LogResponse("CORE:(ProcessCoreData) Ad Hoc Query send to PSIMSLeadsCMD" + Environment.NewLine);
                        model.QueryType = "PSA License";
                        model.RequestResponse = 2106;
                        JsonConvert.SerializeObject(model);
                        _logger.LogResponse("Entering BuildAdHocStateQuery with WSID " + model.WSID);
                        var queryKey8 = await _leadsQuery.BuildAdHocStateQuery(model, _settings);
                        return;
                    default:
                        return;
                }
            case 1001:
            case 1002:
                _logger.LogResponse("CORE:(ProcessCoreData) Vehicle Query send to PSIMSLeadsCMD" + Environment.NewLine);
                var str9 = xelement.Element("Vehicle")?.Element("Agency")?.Value;
                var int32_1 = Convert.ToInt32(xelement.Element("Vehicle")?.Element("CallYear")?.Value);
                var int32_2 = Convert.ToInt32(xelement.Element("Vehicle")?.Element("CallID")?.Value);
                model.Agency = Convert.ToInt32(str9);
                model.CallYear = int32_1;
                model.CallID = int32_2;
                model.QueryType = "Call";
                model.RequestResponse = 3003;
                JsonConvert.SerializeObject(model);
                _logger.LogResponse("Entering BuildCallVehicle with WSID " + model.WSID);
                await _leadsQuery.BuildCallVehicle(model, _settings);
                break;
            case 1101:
            case 1102:
                _logger.LogResponse("CORE:(ProcessCoreData) Person Query send to PSIMSLeadsCMD" + Environment.NewLine);
                var str10 = xelement.Element("Person")?.Element("Agency")?.Value;
                var int32_3 = Convert.ToInt32(xelement.Element("Person")?.Element("CallYear")?.Value);
                var int32_4 = Convert.ToInt32(xelement.Element("Person")?.Element("CallID")?.Value);
                model.Agency = Convert.ToInt32(str10);
                model.CallYear = int32_3;
                model.CallID = int32_4;
                model.QueryType = "Call";
                model.RequestResponse = 3003;
                JsonConvert.SerializeObject(model);
                _logger.LogResponse("Entering BuildCallPerson with WSID " + model.WSID);
                await _leadsQuery.BuildCallPerson(model, _settings);
                break;
        }
    }

    public string IdentifyRequestType(string xmlContent)
    {
        try
        {
            var xelement1 = XElement.Parse(xmlContent);
            var xelement2 = xelement1.Element("Command");
            var xelement3 = xelement1.Element("CommandType");
            if (xelement2 != null && xelement3 != null)
                switch (xelement1.Element("StateQuery")?.Element("Name")?.Value)
                {
                    case "LIC":
                        return "PSA License";
                    case "NAM":
                        return "PSA Person";
                    case "NSX":
                        return "PSA NSX";
                    case "ART":
                        return "PSA ART";
                    case "FOID":
                        return "PSA FOID";
                    case "GUN":
                        return "PSA GUN";
                }
            else
                switch (xelement1.Element("StateQuery")?.Element("Name")?.Value)
                {
                    case "LIC":
                        return "Plate";
                    case "NAM":
                        return "Person";
                }

            return "Unknown Request Type";
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error parsing XML: " + ex.Message);
            return "Error";
        }
    }

    private async Task SendCoreCommandAsync(int command, byte[] data)
    {
        try
        {
            if (!_isConnected || _stream == null)
                return;
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(1);
                ms.WriteByte((byte)command);
                if (data != null)
                    ms.Write(data, 0, data.Length);
                var array = ms.ToArray();
                await _stream.WriteAsync(array, 0, array.Length);
                _logger.LogResponse("Core command sent.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogResponse("Error sending core command: " + ex.Message);
        }
    }

    private string ExtractXmlFromData(byte[] data)
    {
        try
        {
            _logger.LogResponse("Attempting to extract XML from data...");
            var str = Encoding.UTF8.GetString(data);
            var startIndex = str.IndexOf("<?xml", StringComparison.Ordinal);
            if (startIndex != -1)
            {
                var xmlFromData = str.Substring(startIndex).Trim(new char[1]);
                _logger.LogResponse("Extracted XML: " + xmlFromData);
                return xmlFromData;
            }

            _logger.LogResponse("No XML found in data.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogResponse("Error extracting XML: " + ex.Message);
            return null;
        }
    }

    public static string CleanInvalidXmlChars(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var stringBuilder = new StringBuilder();
        foreach (var ch in text)
        {
            // Allow valid XML characters
            if (ch == '\t' || ch == '\n' || ch == '\r' || (ch >= ' ' && ch <= '\uD7FF') ||
                (ch >= '\uE000' && ch <= '\uFFFD'))
            {
                stringBuilder.Append(ch);
            }
        }

        return stringBuilder.ToString(); // Do not remove spaces
    }
}