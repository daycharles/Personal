using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using PSIMSLeads;
using PSIMSLeads.PSIMSLeads;
using static CoreDataPacket;
using Timer = System.Threading.Timer;

namespace PSIMSLeads
{
    public class FoxTalkClient
    {
        private static FoxTalkClient _foxTalkClient;
        private static int _exchangeIdCounter;
        private readonly PSIMSCore _psimsCore;

        private readonly Channel<byte[]> _writeChannel = Channel.CreateBounded<byte[]>(
            new BoundedChannelOptions(100)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest
            });


        // field in your class
        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private CancellationTokenSource _cts;
        private ushort _defaultTimeout;
        private Timer _heartbeatTimer;
        private int _idleCounter;
        private bool _isConnected;
        private bool _isMultiFrameResponse;
        private PSIMSLeadsParse _leadsParse;
        private Logger _logger;
        private int _majorVersion;
        private uint _maxFrameLength;
        private ushort _maxIdleTime;
        private int _minorVersion;

        private NetworkStream _networkStream;

        // track the last heartbeat we sent, to match on the reply
        private volatile ushort _pendingHeartbeatId;
        private ConcurrentDictionary<string, QueryKey> _queries = new();
        private string _serverAddress;
        private int _serverPort;
        private int _sessionCounter;
        private Timer _sessionTimer;
        private Settings _settings;
        private TcpClient _tcpClient;
        private char _useEncryption;
        private StringBuilder _xmlAccumulator = new();


        public FoxTalkClient(
            Logger logger,
            RichTextBox textBox,
            Label lblIdleTimer,
            Label lblSessionCounter,
            Label txtConnectLabel)
        {
            _logger = logger;
            _psimsCore = new PSIMSCore(false, textBox, _logger);
            _foxTalkClient = this;
            var config = new ConfigurationReader();
            _settings = config.LoadSettings();
            _maxIdleTime = 30;
            _defaultTimeout = 60;
            _leadsParse = new PSIMSLeadsParse(_logger);
        }

        public static FoxTalkClient GetFoxTalkClient()
        {
            return _foxTalkClient;
        }

        public async Task StartCoreListeningThread()
        {
            var targetIP = _settings.TargetIP;
            var targetPort = _settings.TargetPort;
            await _psimsCore.StartAsync(targetIP, targetPort);
            _logger.LogResponse($"Listening on {targetIP}:{targetPort}");
        }

        public PSIMSCore GetCurrentClient()
        {
            return PSIMSCore.GetCoreClient();
        }

        public async Task ConnectAsync(string serverAddress, int port)
        {
            _serverAddress = serverAddress;
            _serverPort = port;
            var networkInterface = _settings.NetworkInterface;
            var attempt = 0;
            while (!_isConnected)
            {
                ++attempt;
                _logger.LogResponse($"Connection attempt {attempt}...");
                try
                {
                    InitializeTcpClientAsync(networkInterface);
                    await AttemptConnectionAsync(serverAddress, port);
                    networkInterface = null;
                    networkInterface = null;
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogResponse(
                        $"Connection attempt {attempt} failed: {ex.Message}. Retrying in {_defaultTimeout} seconds...");
                    await Task.Delay(_defaultTimeout * 1000);
                }
            }

            networkInterface = null;
            networkInterface = null;
        }

        private void InitializeTcpClientAsync(string networkInterface)
        {
            if (IsValidNetworkInterface(networkInterface) && IsNetworkInterfaceActive(networkInterface))
            {
                var localEP = new IPEndPoint(IPAddress.Parse(networkInterface), 0);
                _tcpClient = new TcpClient(localEP);
                _logger.LogResponse($"Bound TcpClient to local IP: {localEP}");
            }
            else
            {
                _tcpClient = new TcpClient();
                _logger.LogResponse("Using default TcpClient configuration.");
            }
        }

        private async Task AttemptConnectionAsync(string serverAddress, int port)
        {
            _logger.LogResponse($"Connecting to server {serverAddress}:{port}...");
            await _tcpClient.ConnectAsync(serverAddress, port);

            _networkStream = _tcpClient.GetStream();
            _networkStream.ReadTimeout = _defaultTimeout * 1000;
            _networkStream.WriteTimeout = _defaultTimeout * 1000;
            await _networkStream.FlushAsync();

            // ─── START WRITE-QUEUE DRAIN ────────────────────────────────
            _ = Task.Run(DrainWriteQueueAsync);
            // ─────────────────────────────────────────────────────────────

            // ─── FRAME-BASED RECEIVE LOOP ───────────────────────────────
            _ = Task.Run(async () =>
            {
                var backlog = new List<byte>();
                var buf = new byte[4096];

                const int preambleLen = 4;
                const int lengthOffset = 4;
                const int lengthFieldSz = 4;

                while (_isConnected)
                    try
                    {
                        var read = await _networkStream.ReadAsync(buf, 0, buf.Length);
                        if (read <= 0)
                        {
                            _logger.LogResponse("Server closed connection; will reconnect");
                            _isConnected = false;
                            StopHeartbeatTimer();
                            _cts?.Cancel();
                            _ = ReconnectAsync();
                            break;
                        }

                        backlog.AddRange(buf.Take(read));

                        while (backlog.Count >= preambleLen + lengthFieldSz)
                        {
                            // Find preamble
                            var sync = -1;
                            for (var i = 0; i <= backlog.Count - preambleLen; i++)
                                if (backlog[i] == 0xFF && backlog[i + 1] == 0x00 &&
                                    backlog[i + 2] == 0xAA && backlog[i + 3] == 0x55)
                                {
                                    sync = i;
                                    break;
                                }

                            if (sync < 0)
                            {
                                _logger.LogResponse("No valid frame preamble found. Dropping oldest byte to resync.");
                                backlog.RemoveAt(0); // Drop one byte and try again next read
                                break;
                            }

                            if (sync > 0)
                            {
                                backlog.RemoveRange(0, sync);
                                continue;
                            }

                            if (backlog.Count < preambleLen + lengthFieldSz)
                                break;

                            var lenBytes = backlog.Skip(lengthOffset).Take(lengthFieldSz).ToArray();
                            if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
                            var frameLen = BitConverter.ToInt32(lenBytes, 0);

                            if (frameLen < preambleLen + lengthFieldSz || frameLen > 32768)
                            {
                                _logger.LogResponse($"Invalid frame length {frameLen}, discarding preamble.");
                                backlog.RemoveRange(0, preambleLen);
                                continue;
                            }

                            if (backlog.Count < frameLen)
                                break;

                            var frame = backlog.GetRange(0, frameLen).ToArray();
                            backlog.RemoveRange(0, frameLen);

                            _logger.LogResponse($"[FRAME {frameLen} bytes]");
                            await ProcessResponse(frame);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogResponse("ReceiveLoop error: " + ex.Message);
                        await Task.Delay(1000);
                    }
            });
            // ───────────────────────────────────────────────────────────────


            _isConnected = true;
            await ClearStrandedMessagesAsync(CancellationToken.None);
            await SendConnectMessage();
        }


        private async Task DrainWriteQueueAsync()
        {
            var reader = _writeChannel.Reader;
            while (await reader.WaitToReadAsync())
                while (reader.TryRead(out var buffer))
                    try
                    {
                        await _writeLock.WaitAsync();
                        await _networkStream.WriteAsync(buffer, 0, buffer.Length);
                        await _networkStream.FlushAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogResponse($"Error sending data: {ex.Message}. Re-enqueuing and retrying…");
                        // re-queue the failed buffer so no messages are lost
                        _writeChannel.Writer.TryWrite(buffer);
                        await Task.Delay(1000); // allow time for reconnect
                        break; // exit inner loop to let reconnect logic run
                    }
                    finally
                    {
                        _writeLock.Release();
                    }
        }


        private async Task SendConnectMessage()
        {
            var exchangeId = GenerateExchangeId();
            var buffer = new byte[36]
            {
                0xFF, 0x00, 0xAA, 0x55, 0x00, 0x00, 0x00, 0x24,
                (byte)(exchangeId >> 8), (byte)exchangeId,
                0x43, 0x59, 0x00, 0x01, 0x00, 0x01,
                0x00, 0x00, 0xFD, 0xE8, 0x00, 0x00,
                0x00, 0x00, 0x4E, 0x42, 0x36, 0x34,
                0x4C, 0x46, 0x20, 0x20, 0x55, 0xAA,
                0x00, 0xFF
            };

            _logger.LogResponse($"Sending Connect packet (ID {exchangeId})");
            _writeChannel.Writer.TryWrite(buffer);
            // ← no more ListenForConnectResponse here
        }

        private void ProcessConnectResponse(byte[] responseBytes)
        {
            if (responseBytes.Length < 36)
                _logger.LogResponse("Invalid response length for connect message");
            int num1 = ReverseBytes(BitConverter.ToUInt16(responseBytes, 8));
            int responseByte1 = responseBytes[10];
            int responseByte2 = responseBytes[11];
            var num2 = ReverseBytes(BitConverter.ToUInt16(responseBytes, 12));
            var num3 = ReverseBytes(BitConverter.ToUInt16(responseBytes, 14));
            var num4 = ReverseBytes(BitConverter.ToUInt32(responseBytes, 16));
            var num5 = ReverseBytes(BitConverter.ToUInt16(responseBytes, 20));
            var num6 = ReverseBytes(BitConverter.ToUInt16(responseBytes, 22));
            var responseByte3 = (char)responseBytes[24];
            var str1 = Encoding.ASCII.GetString(responseBytes, 25, 3);
            Encoding.ASCII.GetString(responseBytes, 28, 4);
            _majorVersion = num2;
            _minorVersion = num3;
            _maxFrameLength = num4;
            _maxIdleTime = num5;
            _defaultTimeout = num6;
            _useEncryption = responseByte3;
            var str2 = responseByte3 == 'Y' ? "Encryption will be used" : "No encryption will be used";
            _logger.LogResponse(
                $"Received Hex: {Environment.NewLine}{BitConverter.ToString(responseBytes).Replace("-", " ")}{Environment.NewLine}{Environment.NewLine}FoxTalk Version {num2}.{num3}{Environment.NewLine}Maximum Frame Length {num4}{Environment.NewLine}Maximum Idle time {num5 / 60} minutes{Environment.NewLine}Default Timeout {num6} seconds{Environment.NewLine}{str2}{Environment.NewLine}Objects will be encoded with the {str1} method{Environment.NewLine}");
        }

        private async Task ReconnectAsync()
        {
            _logger.LogResponse("Attempting to reconnect...");

            // Cancel any pending operations and clean up
            try
            {
                _cts?.Cancel();
            }
            catch
            {
            }

            try
            {
                _cts?.Dispose();
            }
            catch
            {
            }

            _cts = new CancellationTokenSource();

            _isConnected = false;
            StopHeartbeatTimer();

            var attempts = 0;
            while (!_isConnected && attempts < 5)
            {
                attempts++;
                try
                {
                    _logger.LogResponse($"Connection attempt {attempts}...");
                    await ConnectAsync(_serverAddress, _serverPort);
                    _logger.LogResponse("Reconnection successful.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogResponse("Reconnect attempt failed: " + ex.Message);
                    await Task.Delay(_defaultTimeout * 1000);
                }
            }
        }


        private bool IsValidNetworkInterface(string networkInterface)
        {
            foreach (var networkInterface1 in NetworkInterface.GetAllNetworkInterfaces())
                if (networkInterface1.GetIPProperties().UnicastAddresses.Any(unicastAddress =>
                        unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork &&
                        unicastAddress.Address.ToString() == networkInterface))
                {
                    _logger.LogResponse("Found valid network interface: " + networkInterface1.Name + " with IP " +
                                        networkInterface);
                    return true;
                }

            _logger.LogResponse("The specified IP '" + networkInterface +
                                "' was not found among available network interfaces.");
            return false;
        }

        private bool IsNetworkInterfaceActive(string networkInterface)
        {
            foreach (var networkInterface1 in NetworkInterface.GetAllNetworkInterfaces())
                foreach (var unicastAddress in networkInterface1.GetIPProperties().UnicastAddresses)
                    if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork &&
                        unicastAddress.Address.ToString() == networkInterface)
                    {
                        if (networkInterface1.OperationalStatus == OperationalStatus.Up)
                        {
                            _logger.LogResponse("Network interface '" + networkInterface1.Name + "' with IP " +
                                                networkInterface + " is active.");
                            return true;
                        }

                        _logger.LogResponse("Network interface '" + networkInterface1.Name + "' with IP " +
                                            networkInterface + " is not active.");
                    }

            _logger.LogResponse("The specified IP '" + networkInterface + "' was not found or is inactive.");
            return false;
        }

        private void StartHeartbeatTimer()
        {
            StopHeartbeatTimer();
            _heartbeatTimer = new Timer(HeartbeatAndIdleCallback, null, 0, 1000);
            _idleCounter = _maxIdleTime;
        }

        private void StopHeartbeatTimer()
        {
            _heartbeatTimer?.Change(-1, -1);
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;
        }

        private void HeartbeatAndIdleCallback(object state)
        {
            try
            {
                // Decrement our idle counter and update the UI
                _idleCounter--;
                _logger.UpdateIdleTimeUI($"Idle time: {_idleCounter:D2}s");

                // Update session-time display
                var ts = TimeSpan.FromSeconds(_sessionCounter);
                _logger.UpdateSessionTimeUI($"Session time: {ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}");

                // If we've hit zero, fire off a heartbeat (fire-and-forget)
                if (_idleCounter <= 0)
                {
                    _logger.LogResponse("Idle expired—sending heartbeat");
                    _ = SendHeartbeatMessage();
                }
            }
            catch (Exception ex)
            {
                // Never let an exception bubble out of the timer
                _logger.LogResponse("Error in HeartbeatAndIdleCallback: " + ex.Message);
            }
        }


        private async Task SendHeartbeatMessage()
        {
            var exchangeId = GenerateExchangeId();
            _pendingHeartbeatId = exchangeId;

            // FoxTalk Heartbeat Frame (16 bytes total)
            var buffer = new byte[16]
            {
                0xFF, 0x00, 0xAA, 0x55, // Frame Start Pattern
                0x00, 0x00, 0x00, 0x10, // Length = 16 (whole frame)
                (byte)(exchangeId >> 8), (byte)exchangeId, // Exchange ID (big-endian)
                (byte)'H', (byte)'Y', // Frame Type 'H', End of Exchange 'Y'
                0x55, 0xAA, 0x00, 0xFF // ✅ Correct Stop Pattern (big-endian)
            };

            _logger.LogResponse($"Sending Heartbeat (ID {exchangeId})");
            _writeChannel.Writer.TryWrite(buffer);
        }


        public async Task SendDataMessage(QueryKey model)
        {
            var foxTalkClient = this;
            foxTalkClient._logger.LogResponse("Record: " + model.Query);
            var queryEntry = new QueryEntry(model,
                new Timer(foxTalkClient.OnQueryTimeout, model.QueryID, foxTalkClient._defaultTimeout * 1000, -1));
            foxTalkClient._queries[model.QueryID] = queryEntry.Model; // Store the QueryKey from QueryEntry
            var xmlBytes = Encoding.ASCII.GetBytes(CleanInvalidXmlChars(model.Query));
            var maxDataLength = (int)foxTalkClient._maxFrameLength - 20;
            for (var offset = 0; offset < xmlBytes.Length; offset += maxDataLength)
            {
                var length = Math.Min(maxDataLength, xmlBytes.Length - offset);
                var numArray = new byte[length];
                Array.Copy(xmlBytes, offset, numArray, 0, length);
                ResetIdleCounter();

                await SendDataSegment(model, numArray);
            }
        }

        private async Task ClearStrandedMessagesAsync(CancellationToken cancellationToken)
        {
            _logger.LogResponse("Starting to clear stranded messages...");

            // your negotiated max frame length (e.g. from handshake)  
            var maxFrame = 32768;
            if (_maxFrameLength != 0) // Changed from `_maxFrameLength != null` to `_maxFrameLength != 0`  
                maxFrame = (int)_maxFrameLength;

            _logger.LogResponse($"Max frame length set to: {maxFrame}");

            var buffer = new byte[maxFrame];
            var temp = new List<byte>();

            // keep pulling until no more data is immediately available  
            while (_networkStream.DataAvailable)
            {
                _logger.LogResponse("Data available on the network stream. Reading...");

                var avail = await _networkStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (avail <= 0)
                {
                    _logger.LogResponse("No more data available to read.");
                    break;
                }

                _logger.LogResponse($"Read {avail} bytes from the network stream.");
                temp.AddRange(buffer.Take(avail));

                // extract every complete packet (header is always 145 bytes)  
                while (temp.Count >= 145)
                {
                    var dataLen = BitConverter.ToInt32(temp.ToArray(), 141);
                    var packetLen = 145 + dataLen;
                    if (temp.Count < packetLen)
                    {
                        _logger.LogResponse("Incomplete packet detected. Waiting for more data.");
                        break;
                    }

                    // pull one packet off the buffer  
                    var packet = temp.GetRange(0, packetLen).ToArray();
                    temp.RemoveRange(0, packetLen);

                    var frameType = (char)packet[10];
                    var exId = (ushort)((packet[8] << 8) | packet[9]);

                    _logger.LogResponse($"Processing packet with frame type: {frameType}, Exchange ID: {exId}");

                    // for data ('M') or NACK ('N'), send our 16-byte ACK  
                    if (frameType == 'M' || frameType == 'N')
                    {
                        _logger.LogResponse($"Sending acknowledgment for Exchange ID: {exId}");
                        await SendAcknowledgment(exId);
                    }
                }
            }

            _logger.LogResponse("Finished clearing stranded messages.");
        }

        private void OnQueryTimeout(object state)
        {
            var key = (string)state;
            if (!_queries.TryRemove(key, out var queryKey)) // Adjusted to work with QueryKey
                return;
            _logger.LogResponse("Query " + key + " removed after inactivity.");
        }

        private async Task SendDataSegment(QueryKey model, byte[] dataSegment)
        {
            _logger.LogResponse("Sending Data Segment. WSID: " + model.WSID);

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            try
            {
                // Frame Start Pattern (FF 00 AA 55)
                writer.Write([0xFF, 0x00, 0xAA, 0x55]);

                // Placeholder for frame length (will patch later)
                writer.Write(new byte[4]);

                // Exchange ID (big endian)
                var exchangeId = GenerateExchangeId();
                writer.Write(new[] { (byte)(exchangeId >> 8), (byte)(exchangeId & 0xFF) });

                // Frame Type and End-of-Exchange
                writer.Write((byte)'M'); // 'M' for Data Message
                writer.Write((byte)'Y'); // End-of-Exchange = 'Y'

                // Payload
                writer.Write(dataSegment);

                // Stop Pattern (must be 0x55AA00FF in big endian)
                writer.Write(new byte[] { 0x55, 0xAA, 0x00, 0xFF });

                // Patch the length field (total frame length)
                var frameLength = (int)ms.Length;
                ms.Position = 4;

                var lengthBytes = BitConverter.GetBytes(frameLength);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lengthBytes);

                writer.Write(lengthBytes);

                // Finalize and write to queue
                var frameBytes = ms.ToArray();
                _writeChannel.Writer.TryWrite(frameBytes);

                _logger.LogResponse($"Data Segment Frame Length = {frameLength} (Exchange ID {exchangeId})");
            }
            catch (Exception ex)
            {
                _logger.LogResponse("Error in SendDataSegment: " + ex.Message);
                await ReconnectWithRetries(model); // or however you retry
                throw;
            }
        }


        private void ResetIdleCounter()
        {
            // TODO: It is adding 1 second each time. Possibly look at clearing buffer after 
            _idleCounter = _maxIdleTime;
            _logger.LogResponse($"Idle counter reset to {_idleCounter}s");
        }

        public async Task ProcessPendingMessages()
        {
            _logger.LogResponse("Processing pending messages...");
            var buffer = new byte[(int)_maxFrameLength];
            for (var length = await _networkStream.ReadAsync(buffer, 0, buffer.Length);
                 length > 0;
                 length = await _networkStream.ReadAsync(buffer, 0, buffer.Length))
            {
                var numArray = new byte[length];
                Array.Copy(buffer, numArray, length);
                await ProcessResponse(numArray);
            }

            _logger.LogResponse("Finished processing pending messages.");
            buffer = null;
            buffer = null;
        }

        private bool IsCompleteResponse(byte[] responseBytes)
        {
            return responseBytes.Length >= 16 && responseBytes.Length >= 16;
        }

        private async Task ReconnectWithRetries(QueryKey model)
        {
            for (var i = 0; i < 5; ++i)
                try
                {
                    _logger.LogResponse($"Attempting to reconnect... ({i + 1}/{5})");
                    await ReconnectAndClearBacklog(model);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogResponse("Reconnect attempt failed: " + ex.Message);
                    await Task.Delay(2000);
                }

            _logger.LogResponse("Max reconnection attempts reached. Giving up.");
        }

        private async Task ReconnectAndClearBacklog(QueryKey model)
        {
            _logger.LogResponse("Attempting to reconnect and clear backlog...");
            var connected = false;
            while (!connected)
                try
                {
                    await ConnectAsync(_serverAddress, _serverPort);
                    _idleCounter = _maxIdleTime;
                    connected = true;
                }
                catch (Exception ex)
                {
                    _logger.LogResponse("Reconnect attempt failed: " + ex.Message);
                    await Task.Delay(_defaultTimeout * 1000);
                }
        }

        private void StartSessionCounter()
        {
            _sessionCounter = 0;
            _sessionTimer = new Timer(UpdateSessionCounter, null, 0, 1000);
        }

        private void UpdateSessionCounter(object state)
        {
            ++_sessionCounter;
            var timeSpan = TimeSpan.FromSeconds(_sessionCounter);
            _logger.UpdateSessionTimeUI("Session time: " +
                                        $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}");
        }

        public ushort ReverseBytes(ushort value)
        {
            return (ushort)((value >> 8) | (value << 8));
        }

        public uint ReverseBytes(uint value)
        {
            return (uint)((int)(value >> 24) | (((int)value << 8) & 16711680) | ((int)(value >> 8) & 65280) |
                          ((int)value << 24));
        }

        public static string CleanInvalidXmlChars(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            var stringBuilder = new StringBuilder();
            foreach (var ch in text)
                if (ch == '\t' || ch == '\n' || ch == '\r' || ch == 0x00 || (ch >= ' ' && ch <= '\uD7FF') ||
                    (ch >= '\uE000' && ch <= '�'))
                    stringBuilder.Append(ch);
            return stringBuilder.ToString();
        }

        public void Close()
        {
            // 1) Signal loops to stop
            _isConnected = false;

            // 2) Tidy up heartbeat/session timers (if you haven’t already)
            StopHeartbeatTimer();
            _sessionTimer?.Change(-1, -1);
            _sessionTimer?.Dispose();

            // 3) Close streams/sockets
            _networkStream?.Close();
            _tcpClient?.Close();

            // 4) (Optional) prevent any further writes
            _writeChannel.Writer.Complete();

            _logger.LogResponse("FoxTalkClient closed cleanly.");
        }


        private ushort GenerateExchangeId()
        {
            return (ushort)Interlocked.Increment(ref _exchangeIdCounter);
        }

        private async Task ProcessResponse(byte[] responseBytes)
        {
            if (responseBytes.Length < 16)
            {
                _logger.LogResponse("Invalid response length");
                return;
            }

            var exchangeId = ReverseBytes(BitConverter.ToUInt16(responseBytes, 8));
            var frameType = (char)responseBytes[10];

            _logger.LogResponse($"Frame Type received: {frameType}");
            ResetIdleCounter();
            switch (frameType)
            {
                case 'A': // Acknowledgment
                    _logger.LogResponse("Received ACK from server.");
                    break;

                case 'C': // Connect response
                    _logger.LogResponse("Processing Connect Response");
                    ProcessConnectResponse(responseBytes);
                    StartSessionCounter();
                    StartHeartbeatTimer();
                    break;

                case 'H': // Heartbeat
                    _logger.LogResponse("Received Heartbeat frame");
                    var replyId = ReverseBytes(BitConverter.ToUInt16(responseBytes, 8));
                    if (replyId == _pendingHeartbeatId)
                    {
                        _logger.LogResponse("Heartbeat validated");
                    }
                    else
                    {
                        _logger.LogResponse($"Unexpected heartbeat ID {replyId}, expected {_pendingHeartbeatId}");
                    }

                    break;

                case 'M': // Data frame
                    _logger.LogResponse("Processing Data Frame");
                    await ProcessDataFrame(responseBytes);
                    await SendAcknowledgment(exchangeId);
                    break;

                case 'N': // Negative Acknowledgement
                    {
                        _logger.LogResponse("Received NAK from FoxTalk");

                        exchangeId = ReverseBytes(BitConverter.ToUInt16(responseBytes, 8));

                        // Extract payload text (starts at byte 12)
                        var errorMsg = Encoding.ASCII.GetString(responseBytes, 12, responseBytes.Length - 16);
                        _logger.LogResponse("NAK Reason: " + errorMsg);

                        // Find matching query
                        if (_queries.TryRemove(exchangeId.ToString(), out var model))
                        {
                            // Create a simple XML or error response to return
                            var errorXml = new XElement("NAK", new XElement("Reason", errorMsg));
                            SendDataToClient(model, new XDocument(errorXml));
                        }
                        else
                        {
                            _logger.LogResponse($"NAK received for unknown Exchange ID {exchangeId}");
                        }

                        await SendAcknowledgment(exchangeId); // still required
                        break;
                    }


                case 'E': // Encrypted (unsupported)
                case 'I': // Identity
                case 'K': // Key
                    _logger.LogResponse($"Received unhandled frame type: {frameType}");
                    break;

                default:
                    _logger.LogResponse($"Unknown frame type received: {frameType}");
                    break;
            }
        }

        private async Task SendAcknowledgment(ushort exchangeId)
        {
            var buffer = new byte[16]
            {
                0xFF, 0x00, 0xAA, 0x55,
                0x00, 0x00, 0x00, 0x10,
                (byte)(exchangeId >> 8), (byte)exchangeId,
                (byte)'A', (byte)'Y', (byte)'U', 0xAA,
                0x00, 0xFF
            };

            _logger.LogResponse("Sending Acknowledgment");
            _writeChannel.Writer.TryWrite(buffer);
        }


        private async Task ProcessDataFrame(byte[] frame)
        {
            if (frame.Length < 16)
            {
                _logger.LogResponse("Invalid data frame length");
                return;
            }

            var exchangeId = ReverseBytes(BitConverter.ToUInt16(frame, 8));
            var frameType = (char)frame[10];
            var endOfExchange = (char)frame[11];

            var payloadLength = frame.Length - 16;
            var payload = new byte[payloadLength];
            Array.Copy(frame, 12, payload, 0, payloadLength);

            string xmlContent;
            try
            {
                xmlContent = Encoding.UTF8.GetString(payload);
            }
            catch (Exception ex)
            {
                _logger.LogResponse("Failed to decode data frame payload: " + ex.Message);
                return;
            }

            // Detect and accumulate multi-frame responses
            if (_isMultiFrameResponse || !xmlContent.Contains("</OFML>"))
            {
                if (!_isMultiFrameResponse)
                {
                    _logger.LogResponse("Multi-frame response start detected (no </OFML> in initial frame)");
                    _isMultiFrameResponse = true;
                    _xmlAccumulator.Clear();
                }

                _xmlAccumulator.Append(xmlContent);

                if (xmlContent.Contains("</OFML>"))
                {
                    _isMultiFrameResponse = false;
                    var fullXml = _xmlAccumulator.ToString();
                    _xmlAccumulator.Clear();
                    _logger.LogResponse("Multi-frame response complete, processing full XML");
                    await ProcessAccumulatedXml(fullXml);
                }
                else
                {
                    _logger.LogResponse("Waiting for next frame to complete multi-frame message...");
                }
            }
            else
            {
                await ProcessAccumulatedXml(xmlContent);
            }
        }


        private async Task ProcessAccumulatedXml(string xmlContent)
        {
            try
            {
                var key = XDocument.Parse(xmlContent).Element("OFML")?.Element("HDR")?.Element("ID").Value;
                if (string.IsNullOrEmpty(key))
                {
                    _logger.LogResponse("No <ID> tag found in response. Dumping full XML:");
                    _logger.LogResponse(xmlContent);
                    return;
                }

                if (_queries.TryGetValue(key, out var model))
                {
                    switch (model.QueryType)
                    {
                        case "Plate":
                        case "Person":
                        case "Call":
                            await CreateCallResponseXml(xmlContent, model);
                            break;
                        case "PSA License":
                        case "PSA Person":
                        case "PSA NSX":
                        case "PSA FOID":
                            await CreatePSAResponseXml(xmlContent, model);
                            break;
                        default:
                            _logger.LogResponse("Unhandled query type: " + model.QueryType);
                            break;
                    }
                }
                else
                {
                    _logger.LogResponse("Query ID not found in dictionary: " + key);
                }
            }
            catch (XmlException ex)
            {
                _logger.LogResponse("XML parsing error: " + ex.Message);
                _logger.LogResponse("Offending XML: " + xmlContent);
            }
        }


        public async Task CreateCallResponseXml(string strText, QueryKey model)
        {
            _logger.LogResponse("Creating Call response xml. WSID: " + model.WSID);

            var content = XDocument.Parse(strText).Root.Element("RSP")?.Value ?? string.Empty;
            var xdocument = XDocument.Parse(strText);

            var doc = new XDocument(
                new XElement("PSIMS",
                    new XElement("Command",
                        new XElement("Verb", "NEWSTATEDATA"),
                        new XElement("Call",
                            new XElement("Agency", model.Agency),
                            new XElement("CallYear", model.CallYear),
                            new XElement("CallID", model.CallID)),
                        new XElement("StateData",
                            new XElement("SubType", model.RecordType),
                            new XElement("Query", model.QueryParams),
                            new XElement("QueryModel", model.QueryID),
                            new XElement("QueryFor", model.QueryFor),
                            new XElement("StateUserName", model.StateUsername),
                            new XElement("Sequence", model.RecordSequence),
                            new XElement("Date", DateTime.Now.ToString(CultureInfo.InvariantCulture)),
                            new XElement("Record", content)))));


            var test = await _leadsParse.ParseStateData(doc, model, xdocument);

            if (test != null)
            {
                doc.Root.Add(test);
                _logger.LogResponse(doc.ToString());
            }

            ProcessUserMessage(doc, model, strText);
        }


        public async Task CreatePSAResponseXml(string strText, QueryKey model)
        {
            _logger.LogResponse("Creating PSA Response XML. WSID: " + model.WSID);
            var xdocument = XDocument.Parse(strText);
            var content = xdocument.Root.Element("RSP")?.Value ?? string.Empty;
            var doc = new XDocument(
                new XElement("PSIMS",
                    new XElement("Command",
                        new XElement("Verb", "NEWSTATEDATA"),
                        new XElement("Call",
                            new XElement("Agency", model.Agency),
                            new XElement("CallYear", model.CallYear),
                            new XElement("CallID", model.CallID)),
                        new XElement("StateData",
                            new XElement("SubType", model.RecordType),
                            new XElement("Query", model.QueryParams),
                            new XElement("QueryModel", model.QueryID),
                            new XElement("QueryFor", model.QueryFor),
                            new XElement("StateUserName", model.StateUsername),
                            new XElement("Sequence", model.RecordSequence),
                            new XElement("Date", DateTime.Now.ToString(CultureInfo.InvariantCulture)),
                            new XElement("Record", content)))));

            var test = await _leadsParse.ParseStateData(doc, model, xdocument);

            if (test != null)
            {
                doc.Root.Add(test);
                _logger.LogResponse(doc.ToString());
            }

            ProcessUserMessage(doc, model, strText);
        }

        private void ProcessUserMessage(XDocument doc, QueryKey model, string strText)
        {
            var respID = Constants.CALL_REQ_COMMAND;
            if (model.ToService == _settings.MobServiceID)
                respID = Constants.MOB_RSP_STATEQUERY;
            if (model.ToService == _settings.WsmServiceID)
                respID = Constants.STATE_RSP_DATA;
            if (model.ToService == _settings.WebServiceID)
                respID = Constants.WB_RSP_STATEQUERY; // For Web Service, use the same as STATE_RSP_DATA

            if (model?.QueryFor?.Equals("JAILCHECKID", StringComparison.OrdinalIgnoreCase) == true)
            {
                var updatedModel = new QueryKey
                {
                    QueryID = model.QueryID,
                    Agency = model.Agency,
                    CallYear = model.CallYear,
                    CallID = model.CallID,
                    RecordType = model.RecordType,
                    RecordSequence = model.RecordSequence,
                    Record = model.Record,
                    WSID = "D:*",
                    Query = model.Query,
                    QueryParams = model.QueryParams,
                    LeadsService = model.LeadsService,
                    ToService = model.ToService,
                    Badge = model.Badge,
                    QueryFor = model.QueryFor,
                    QueryType = model.QueryType,
                    Created = model.Created,
                    StateUsername = model.StateUsername,
                    Username = model.Username,
                    RequestResponse = model.RequestResponse,
                    RecordDate = model.RecordDate,
                    CoreDataPacket = model.CoreDataPacket,
                    Responses = model.Responses,
                    Lic = model.Lic,
                    Lis = model.Lis,
                    Lit = model.Lit,
                    Liy = model.Liy,
                    Vin = model.Vin,
                    Vma = model.Vma,
                    Vyr = model.Vyr,
                    Lname = model.Lname,
                    Fname = model.Fname,
                    Mname = model.Mname,
                    Dob = model.Dob,
                    Sex = model.Sex,
                    Race = model.Race,
                    Oln = model.Oln,
                    Ols = model.Ols,
                    Noh = model.Noh,
                    Nam = model.Nam,
                    Pno = model.Pno,
                    ArticleType = model.ArticleType,
                    ArticleSerial = model.ArticleSerial,
                    OwnerNumber = model.OwnerNumber,
                    Sighted = model.Sighted,
                    ArticleId = model.ArticleId,
                    QueryEntity = model.QueryEntity,
                    LNAM = model.LNAM,
                    GIV1 = model.GIV1,
                    GIV2 = model.GIV2,
                    DOB = model.DOB,
                    FOID = model.FOID,
                    GunSerial = model.GunSerial,
                    GunMake = model.GunMake,
                    GunCaliber = model.GunCaliber
                };

                SendDataToClient(updatedModel, doc);

                SendDataToClient(model, doc);
            }
            else
            {
                SendDataToClient(model, doc);
            }
        }

        public void SendDataToClient(QueryKey model, XDocument data)
        {
            _logger.LogResponse("Sending Data to client. WSID: " + model.WSID);
            try
            {
                var stream = GetCurrentClient()._stream;
                var bytes = Encoding.ASCII.GetBytes(data.ToString());
                var buffer = new CoreDataPacket
                {
                    ucType = Constants.CORE_MESSAGE_TYPE_DATA,
                    nToService = model.ToService,
                    nFromService = model.LeadsService,
                    nRequestResponse = model.RequestResponse,
                    nDataLen = bytes.Length,
                    aData = bytes,
                    aClientAddress = model.WSID
                }.BuildPacket();
                try
                {
                    stream.Write(buffer, 0, buffer.Length);
                    Console.WriteLine("Packet sent successfully.");
                }
                catch (IOException ex)
                {
                    Console.WriteLine("IO Error: " + ex.Message);
                    _logger.LogResponse("IO Error: " + ex.Message);
                }
                catch (ObjectDisposedException ex)
                {
                    Console.WriteLine("Stream has been closed: " + ex.Message);
                    _logger.LogResponse("Stream has been closed: " + ex.Message);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine("Invalid operation: " + ex.Message);
                    _logger.LogResponse("Invalid operation: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unexpected error: " + ex.Message);
                    _logger.LogResponse("Unexpected error: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                _logger.LogResponse("Error: " + ex.Message);
            }
        }

        public async Task SimulateConcurrentQueries(int clientCount, int messagesPerClient)
        {
            var foxClient = GetFoxTalkClient();
            var tasks = new List<Task>();
            var rnd = new Random();

            for (var i = 0; i < clientCount; i++)
            {
                var i1 = i;
                tasks.Add(Task.Run(async () =>
                {
                    for (var j = 0; j < messagesPerClient; j++)
                    {
                        var queryId = "8a90397e9d";
                        var mockQuery = new QueryKey
                        {
                            QueryID = queryId,
                            Query = $"<OFML><HDR><ID>{queryId}</ID></HDR><RSP>Response {j}</RSP></OFML>",
                            QueryParams = $"Sim-{i1}-{j}",
                            WSID = $"SimWS-{i1}",
                            QueryType = "Person",
                            RecordType = "Simulation",
                            Agency = 1,
                            CallID = 999,
                            CallYear = 2025,
                            RecordSequence = j + 1,
                            QueryFor = "SimTest",
                            StateUsername = "SimUser",
                            Username = "Tester",
                            ToService = 1,
                            LeadsService = 2,
                            RequestResponse = 2106
                        };

                        try
                        {
                            await foxClient.SendDataMessage(mockQuery);
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Sent message {j} from client {i1}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error from client {i1} msg {j}: {ex.Message}");
                        }

                        await Task.Delay(rnd.Next(200, 1000)); // simulate variable delay between sends
                    }
                }));
            }

            await Task.WhenAll(tasks);
            Console.WriteLine("Simulation completed.");
        }
    }
}


#region Helper Classes

public class QueryEntry
{
    public QueryEntry(QueryKey model, Timer timer)
    {
        Model = model;
        IdleTimer = timer;
    }

    public QueryKey Model { get; }
    public Timer IdleTimer { get; }
}

public class CoreDataPacket
{
    public string aClientAddress;
    public byte[] aData;
    public int nDataLen;
    public int nFromService;
    public int nRequestResponse;
    public int nToService;
    public byte ucType;

    public byte[] BuildPacket()
    {
        using (var output = new MemoryStream())
        {
            using (var binaryWriter = new BinaryWriter(output))
            {
                binaryWriter.Write(ucType);
                binaryWriter.Write(nFromService);
                binaryWriter.Write(nToService);
                binaryWriter.Write(Encoding.ASCII.GetBytes(aClientAddress.PadRight(128, char.MinValue)));
                binaryWriter.Write(nRequestResponse);
                binaryWriter.Write(nDataLen);
                binaryWriter.Write(aData);
            }

            return output.ToArray();
        }
    }

    public static class Constants
    {
        public const int MAX_LAST_ERROR = 255;
        public const int MAX_CORE_IP_PACKET = 32768;
        public const int MAX_CLIENT_DATA_BUFFER = 32768;
        public const int MAX_CLIENT_DATA = 32766;
        public const int PSIMS_LEADS_PORT = 5055;
        public const byte CORE_MESSAGE_TYPE_CMD = 1;
        public const int CORE_PACKET_SOURCE_SIZE = 128;
        public const int CORE_COMMAND_SHUTDOWN = 255;
        public const int CORE_COMMAND_SERVICE_END = 254;
        public const int CORE_COMMAND_SERVICE_READY = 1;
        public const int CORE_COMMAND_HEARTBEAT = 2;
        public const int CORE_COMMAND_ACK = 3;
        public const int CORE_SERVICE_ID = 1;
        public const int DBACCESS_OPENING = 1;
        public const int DBACCESS_OPENED = 2;
        public const int DBACCESS_CLOSING = 3;
        public const int DBACCESS_CLOSED = 4;
        public const int DBACCESS_OPEN_FAILED = 5;
        public const int RMSEXTERNAL_EVIDENCE = 1;
        public const int RMSEXTERNAL_FIREHOUSE = 2;
        public const int WM_USER_MESSAGE_ROOT = 10000;
        public const int WM_USER_SHOWCALL = 10001;
        public const int WM_USER_NEWTSTOP = 10002;
        public const int WM_USER_SHOWCASE = 10003;
        public const int WM_USER_SWITCH_VIEW = 10004;
        public const int STATE_RSP_DATA = 2106;
        public const int CALL_RQRRSP_ROOT = 3000;
        public const int MOB_RSP_STATEQUERY = 108;
        public const int WB_RSP_STATEQUERY = 1010;
        public const int WS_REQ_PERSONUPDATE = 114;
        public const int WS_REQ_VEHICLEUPDATE = 115;
        public const int CALL_REQ_COMMAND = 3003;
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
        public static byte CORE_MESSAGE_TYPE_DATA = 2;
    }
}

#endregion