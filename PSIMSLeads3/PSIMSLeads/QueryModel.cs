using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Xml.Linq;

namespace PSIMSLeads;

public class QueryModel
{
    public string QueryID { get; set; }
    public int Agency { get; set; }
    public int CallYear { get; set; }
    public int CallID { get; set; }
    public string RecordType { get; set; }
    public int RecordSequence { get; set; }
    public string Record { get; set; }
    public string WSID { get; set; }
    public ushort ExchangeId { get; set; }
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
    public string RecordDate { get; set; }
    public CoreDataPacket CoreDataPacket { get; set; }
    public ConcurrentDictionary<ushort, string> Responses { get; set; } = new();

    // Vehicle
    public string Lic { get; set; }
    public string Lis { get; set; }
    public string Lit { get; set; }
    public string Liy { get; set; }
    public string Vin { get; set; }
    public string Vma { get; set; }
    public string Vyr { get; set; }

    // Person
    public string Lname { get; set; }
    public string Fname { get; set; }
    public string Mname { get; set; }
    public string Dob { get; set; }
    public string Sex { get; set; }
    public string Race { get; set; }
    public string Oln { get; set; }
    public string Ols { get; set; }

    // NSX
    public string Noh { get; set; }
    public string Nam { get; set; }
    public string Pno { get; set; }

    // ART
    public string ArticleType { get; set; }
    public string ArticleSerial { get; set; }
    public string OwnerNumber { get; set; }
    public bool Sighted { get; set; }
    public string ArticleId { get; set; }
    public string QueryEntity { get; set; }

    // FOID
    public string LNAM { get; set; }
    public string GIV1 { get; set; }
    public string GIV2 { get; set; }
    public string DOB { get; set; }
    public string FOID { get; set; }

    // GUN
    public string GunSerial { get; set; }
    public string GunMake { get; set; }
    public string GunCaliber { get; set; }

    public void ParseNameString(string concatenatedName)
    {
        if (string.IsNullOrWhiteSpace(concatenatedName))
            throw new ArgumentException("Concatenated name cannot be null or empty", nameof(concatenatedName));

        var parts = concatenatedName.Split(',');

        var lname = parts[0].Trim();
        var fname = parts.Length > 1 ? parts[1].Trim().Split(' ')[0].Trim() : null;
        var mname = parts.Length > 1 && parts[1].Trim().Split(' ').Length > 1
            ? parts[1].Trim().Split(' ')[1].Trim() : null;

        // You must return a new record instance to update values.
        throw new InvalidOperationException("Use With() to clone and update name fields.");
    }
}

public class FrameAssembler
{
    private readonly NetworkStream _stream;
    private readonly List<byte> _buffer = new List<byte>();

    public FrameAssembler(NetworkStream stream)
    {
        _stream = stream;
    }

    // This method keeps reading until a full frame is detected.
    public async Task<byte[]> ReadFrameAsync(CancellationToken token)
    {
        while (true)
        {
            // Check if we can locate the start pattern (0xFF00AA55) and determine the frame length.
            // (Implementation: search _buffer for the start pattern; if found, ensure that enough bytes have been read as indicated by the subsequent 4-byte frame length field.)
            // When a complete frame (including the stop pattern 0x55AA00FF) is available, extract it,
            // remove the processed bytes from _buffer and return the frame.

            // If no full frame exists, read more data:
            byte[] temp = new byte[1024];
            int bytesRead = await _stream.ReadAsync(temp, 0, temp.Length, token);
            if (bytesRead == 0)
                throw new Exception("Connection closed.");
            _buffer.AddRange(temp.Take(bytesRead));

            // Try to extract a full frame from _buffer (this logic must be written per protocol).
            byte[] fullFrame = TryExtractFrame();
            if (fullFrame != null)
                return fullFrame;
        }
    }

    private byte[] TryExtractFrame()
    {
        // Implement parsing logic per FoxTalk framing:
        // 1. Find start pattern (4 bytes: FF 00 AA 55).
        // 2. Read the next 4 bytes to get the frame length (ensure it’s within negotiated bounds).
        // 3. Verify that the buffer contains the entire frame (including header, payload, stop pattern).
        // 4. Verify that the stop pattern (55 AA 00 FF) is at the expected location.
        // Return null if a complete frame is not yet available.
        return null;
    }
}


#region CONSTANTS

public static class Constants
{
    public const int MAX_LAST_ERROR = 255;
    public const int MAX_CORE_IP_PACKET = 32768;
    public const int MAX_CLIENT_DATA_BUFFER = 32768;
    public const int MAX_CLIENT_DATA = MAX_CLIENT_DATA_BUFFER - 2;

    public const int PSIMS_LEADS_PORT = 5055;

    public const byte CORE_MESSAGE_TYPE_CMD = 1;
    public const byte CORE_MESSAGE_TYPE_DATA = 2;

    public const int CORE_PACKET_SOURCE_SIZE = 128;

    public const int CORE_COMMAND_SHUTDOWN = 0xFF;
    public const int CORE_COMMAND_SERVICE_END = 0xFE;
    public const int CORE_COMMAND_SERVICE_READY = 0x01;
    public const int CORE_COMMAND_HEARTBEAT = 0x02;
    public const int CORE_COMMAND_ACK = 0x03;

    public const int CORE_SERVICE_ID = 1;

    public const int DBACCESS_OPENING = 1;
    public const int DBACCESS_OPENED = 2;
    public const int DBACCESS_CLOSING = 3;
    public const int DBACCESS_CLOSED = 4;
    public const int DBACCESS_OPEN_FAILED = 5;

    // DBMS
    private const int DBMS_RQRRSP_ROOT = 1000;
    public const int DBMS_REQ_PICKLIST = 29 + DBMS_RQRRSP_ROOT;

    public const int RMSEXTERNAL_EVIDENCE = 1;
    public const int RMSEXTERNAL_FIREHOUSE = 2;

    public const int WM_USER_MESSAGE_ROOT = 10000;
    public const int WM_USER_SHOWCALL = WM_USER_MESSAGE_ROOT + 1;
    public const int WM_USER_NEWTSTOP = WM_USER_MESSAGE_ROOT + 2;
    public const int WM_USER_SHOWCASE = WM_USER_MESSAGE_ROOT + 3;
    public const int WM_USER_SWITCH_VIEW = WM_USER_MESSAGE_ROOT + 4;

    // Response
    public const int STATE_RSP_DATA = 2106;
    public const int CALL_RQRRSP_ROOT = 3000;
    public const int MOB_RSP_STATEQUERY = 108;
    public const int WB_RSP_STATEQUERY = 1010;
    public const int WS_REQ_PERSONUPDATE = 114;
    public const int WS_REQ_VEHICLEUPDATE = 115;


    // Request
    public const int CALL_REQ_COMMAND = 3 + CALL_RQRRSP_ROOT;
    public const int WS_REQ_STATE_DATA = 605;
    public const int WS_RSP_STATE_DATA = 606;
    public const int MOB_REQ_STATEQUERY = 107;
    public const int WB_REQ_STATE_DATA = 1011;
    public const int STATE_REQ_VEHICLE_LICENSE = 1001;
    public const int STATE_REQ_VEHICLE_VIN = 1002;
    public const int STATE_REQ_PERSON_NAME = 1101;
    public const int STATE_REQ_PERSON_LICENSE = 1102;
    public const int STATE_REQ_USERLOGON = 1104;
    public const int STATE_REQ_USERLOGOFF = 1105;
    public const int STATE_REQ_PERSON_SSN = 1103;
    public const int CALL_RSP_NEWSTATEDATA = 3507;
}

public class CoreDataPacket
{
    public string aClientAddress { get; set; }
    public byte[] aData { get; set; }
    public int nDataLen { get; set; }
    public int nFromService { get; set; }
    public int nRequestResponse { get; set; }
    public int nToService { get; set; }
    public byte ucType { get; set; }
    public DataArgs Args { get; set; } = new();

    public byte[] BuildPacket()
    {
        using (var ms = new MemoryStream())
        {
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(ucType);
                bw.Write(nFromService);
                bw.Write(nToService);
                bw.Write(Encoding.ASCII.GetBytes(aClientAddress.PadRight(Constants.CORE_PACKET_SOURCE_SIZE, '\0')));
                bw.Write(nRequestResponse);
                bw.Write(nDataLen);
                bw.Write(Args.pData ?? Args.aData);
            }

            return ms.ToArray();
        }
    }
}

public class DataArgs
{
    public byte[] pData { get; set; }
    public byte[] aData { get; set; } = new byte[1];
}

public class CoreCommandPacket
{
    public byte UcType { get; set; }
    public int FromService { get; set; }
    public int ToService { get; set; }
    public int CommandLen { get; set; }
    public CommandArgs Args { get; set; } = new();

    public byte[] BuildPacket()
    {
        using (var ms = new MemoryStream())
        {
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(UcType);
                bw.Write(FromService);
                bw.Write(ToService);
                bw.Write(CommandLen);
                bw.Write(Args.pCommand ?? Args.aCommand);
            }

            return ms.ToArray();
        }
    }
}

public class CommandArgs
{
    public byte[] pCommand { get; set; }
    public byte[] aCommand { get; set; } = new byte[1];
}


#endregion Data Packet Classes