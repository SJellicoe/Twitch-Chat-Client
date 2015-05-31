using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Chat
{
    class IRC
    {
        private int port = 6667;
        private string server = "irc.twitch.tv";
        public string channel { get; set; }
        private Socket sock;

        public IRC()
        {
            
        }

        public bool Connect()
        {
            bool success = false;
            string buffer;

            try
            {
                sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
                sock.Connect(server, port);
                sock.ReceiveTimeout = 1000;
            }
            catch(Exception e)
            {
            }


            if (Send("PASS oauth:" + Properties.Settings.Default.OAuth + "\r\n" + "NICK " + Properties.Settings.Default.Username + "\r\n"))
            {
                do
                {
                    buffer = Receive();
                } while (buffer != "" && !buffer.Contains(":tmi.twitch.tv 376 " + Properties.Settings.Default.Username + " :"));
                if (buffer.Contains(":tmi.twitch.tv 376 " + Properties.Settings.Default.Username + " :"))
                {
                    success = true;
                }
            }

            return success;
        }

        public bool JoinChannel(string channelName)
        {
            bool success = false;
            string buffer;
            ASCIIEncoding encoder = new ASCIIEncoding();

            if (channel != "")
            {
                if (Send("PART #" + channelName + "\r\n"))
                {
                    do
                    {
                        buffer = Receive();
                    } while (buffer != "" && buffer.Contains("PART #" + channelName));
                    success = true;
                }
            }

            channel = channelName;

            if(Send("JOIN #" + channelName + "\r\n"))
            {
                do
                {
                    buffer = Receive();
                } while (buffer != "" && buffer.Contains(":jtv MODE #" + channelName +" +o"));
                success = true;
            }

            return success;
        }

        public bool Send(string message)
        {
            bool success = false;
            byte[] send;
            ASCIIEncoding encoder = new ASCIIEncoding();

            if (sock.Connected)
            {
                try
                {
                    send = encoder.GetBytes(message + "\r\n");
                    sock.Send(send);
                    success = true;
                }
                catch(Exception e)
                {
                }
            }

            return success;
        }

        public bool SendMessage(string message)
        {
            bool success = false;

            if(Send("PRIVMSG #" + channel + " :" + message))
            {
                success = true;
            }

            return success;
        }

        public string Receive()
        {
            byte[] receive = new byte[16384];
            string buffer = "";
            ASCIIEncoding encoder = new ASCIIEncoding();

            if (sock.Connected)
            {
                try
                {
                    sock.Receive(receive);
                    buffer = encoder.GetString(receive);
                    buffer = buffer.TrimEnd('\0');
                }
                catch(Exception e)
                {
                }
            }

            return buffer;
        }


    }
}
