using System;
using System.Configuration;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Twitch_Chat
{
    public class IRC
    {
        private const int PORT = 6667;
        private const string SERVER = "irc.chat.twitch.tv";
        private TcpClient _client;
        private NetworkStream _stream;
        private string _username = "";
        private string _channel = "";
        public string Channel
        {
            get { return _channel; }
        }


        public IRC()
        {
            
        }

        public async Task<bool> Connect()
        {
            bool success = false;
            string buffer;

            _username = ConfigurationManager.AppSettings["username"];

            try
            {
                _client = new TcpClient(SERVER, PORT);
                _stream = _client.GetStream();
            }
            catch(Exception)
            {
                return success;
            }


            if (await Send($"PASS oauth:{ConfigurationManager.AppSettings["oauth"]}") && await Send($"NICK {_username}"))
            {
                buffer = await Receive();

                if (buffer.Contains($":tmi.twitch.tv 376 {_username} :"))
                {
                    success = true;
                }
            }

            return success;
        }

        public async Task<bool> JoinChannel(string channelName)
        {
            bool success = false;
            string buffer;

            if (_channel != "")
            {
                if (await Send($"PART #{_channel}\r\n"))
                {
                    buffer = await Receive();
                }
            }

            if(await Send("JOIN #" + channelName + "\r\n"))
            {
                buffer = await Receive();

                if (buffer != "" && buffer.Contains(".tmi.twitch.tv 353") &&
                buffer.Contains(".tmi.twitch.tv 366") && buffer.Contains("End of /NAMES list"))
                {
                    success = true;
                    _channel = channelName;
                }
            }

            if(await Send("CAP REQ :twitch.tv/membership"))
            {
                buffer = await Receive();
            }

            return success;
        }

        public async Task<bool> Send(string message)
        {
            bool success = false;
            byte[] send;

            if (_client.Connected)
            {
                try
                {
                    send = Encoding.UTF8.GetBytes(message + "\r\n");
                    await _stream.WriteAsync(send, 0, send.Length);
                    success = true;
                }
                catch(Exception)
                {

                }
            }

            return success;
        }

        public async Task<bool> SendMessage(string message)
        {
            bool success = false;

            if(await Send($"PRIVMSG #{_channel} :{message}"))
            {
                success = true;
            }

            return success;
        }

        public async Task<string> Receive()
        {
            return await Receive(CancellationToken.None);
        }

        public async Task<string> Receive(CancellationToken token)
        {
            byte[] receive = new byte[16384];
            string buffer = "";

            if (_client.Connected)
            {
                try
                {
                    using (token.Register(() => _stream.Close()))
                    {
                        int numBytes = await _stream.ReadAsync(receive, 0, receive.Length, token);
                        buffer = Encoding.UTF8.GetString(receive, 0, numBytes);
                    }
                }
                catch (ObjectDisposedException)
                {
                    if (!await Connect())
                    {
                        throw;
                    }
                }
            }

            return buffer;
        }


    }
}
