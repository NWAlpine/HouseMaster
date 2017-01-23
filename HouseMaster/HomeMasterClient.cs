using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace HouseMaster
{
    class HomeMasterClient
    {
        private readonly string _ip;
        private readonly int _port;
        private StreamSocket _socket;
        private DataWriter _writer;
        private DataReader _reader;

        public delegate void Error(string message);
        public event Error OnError;

        public delegate void DataReceived(string message);
        public event DataReceived OnDataReceived;

        public string Ip
        {
            get { return _ip; }
        }
        public int Port
        {
            get { return _port; }
        }

        // ctor
        public HomeMasterClient(string ip, int port)
        {
            _ip = ip;
            _port = port;
        }

        /// <summary>
        /// Connect to endpoint
        /// </summary>
        public async void Connect()
        {
            try
            {
                var hostName = new HostName(Ip);
                _socket = new StreamSocket();
                await _socket.ConnectAsync(hostName, Port.ToString());

                // connected, now read
                _writer = new DataWriter(_socket.OutputStream);
                Read();

            }
            catch (Exception e)
            {
                if (OnError != null)
                    OnError(e.Message);
            }
        }

        /// <summary>
        /// Send Data
        /// </summary>
        /// <param name="message">Message to send</param>
        public async void Send(string message)
        {
            // sends the size of the string
            _writer.WriteUInt32(_writer.MeasureString(message));
            // sends the string (message)
            _writer.WriteString(message);

            try
            {
                // performs post
                await _writer.StoreAsync();

                // clean up for next message
                await _writer.FlushAsync();

            }
            catch (Exception e)
            {
                if (OnError != null)
                    OnError(e.Message);
            }
        }

        /// <summary>
        /// Read Data
        /// </summary>
        private async void Read()
        {
            _reader = new DataReader(_socket.InputStream);
            try
            {
                while(true)
                {
                    uint sizeFieldCount = await _reader.LoadAsync(sizeof(uint));
                    // if disconnected
                    if (sizeFieldCount != sizeof(uint))
                        return;

                    uint stringLength = _reader.ReadUInt32();
                    uint actualStringLength = await _reader.LoadAsync(stringLength);
                    // if disconnected
                    if (stringLength != actualStringLength)
                        return;

                    if (OnDataReceived != null)
                    {
                        OnDataReceived(_reader.ReadString(actualStringLength));
                    }
                }
            }
            catch (Exception e)
            {
                if (OnError != null)
                    OnError(e.Message);
            }
        }

        /// <summary>
        /// Close and cleanup
        /// </summary>
        public void Close()
        {
            // TODO: these need to be wrapped in a Using statement so the cleanup
            // happens when the object is out of scope
            // http://donatas.xyz/streamsocket-tcpip-client.html
            _writer.DetachStream();
            _writer.Dispose();

            _reader.DetachStream();
            _reader.Dispose();

            _socket.Dispose();
        }
    }
}
