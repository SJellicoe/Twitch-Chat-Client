using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Twitch_Chat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IRC irc = new IRC();
        private Thread listen;

        public MainWindow()
        {
            InitializeComponent();
            irc.Connect();
            LoadSettings();
        }


        private void Send(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                if(irc.SendMessage(sendTextBox.Text))
                {
                    sendTextBox.Text = "";
                }
            }
        }

        private void Join(object sender, RoutedEventArgs e)
        {
            if (listen != null)
            {
                if (listen.IsAlive)
                {
                    listen.Abort();
                }
            }
            irc.JoinChannel(channelTextBox.Text);
            listen = new Thread (new ThreadStart(Listen));
            listen.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (listen != null)
            {
                if (listen.IsAlive)
                {
                    listen.Abort();
                }
            }
            base.OnClosed(e);
        }


        public void Listen()
        {
            string message = "";
            string username = "";
            
            while (true)
            {
                message = irc.Receive();
                if (message != "" && message != "PING :tmi.twitch.tv\r\n")
                {
                    if (message.Contains(".tmi.twitch.tv PRIVMSG #"))
                    {
                        username = message.Substring(message.IndexOf(':') + 1, message.IndexOf('!') - 1);
                        message = message.Remove(0, message.IndexOf(".tmi.twitch.tv PRIVMSG #"));
                        message = message.Replace(".tmi.twitch.tv PRIVMSG #" + irc.channel + " :", ": ");
                        message = username + message;
                    }
                    else if (message.Contains(".tmi.twitch.tv PART #"))
                    {
                        username = message.Substring(message.IndexOf(':') + 1, message.IndexOf('!') - 1);
                        message = username + "has left the chat.";
                    }
                    else if (message.Contains(".tmi.twitch.tv JOIN #"))
                    {
                        username = message.Substring(message.IndexOf(':') + 1, message.IndexOf('!') - 1);
                        message = username + "has joined the chat.";
                    } 
                    ThreadStart start = delegate()
                    {
                        DispatcherOperation operation = Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                          new Action<string>(UpdateText), message);
                        DispatcherOperationStatus status = operation.Status;
                        while (status != DispatcherOperationStatus.Completed)
                        {
                            status = operation.Wait(TimeSpan.FromMilliseconds(500));
                        }
                    };
                    new Thread(start).Start();
                }
                else if(message.IndexOf("PING :tmi.twitch.tv") == 0)
                {
                    message = message.Replace("PING", "PONG");
                    if (!irc.Send(message))
                    {
                        message = "Failed to respond to PING.";
                        break;
                    }
                }
            }
        }

        private void UpdateText(string message)
        {
            if (message != "")
            {
                receivedText.AppendText(message + System.Environment.NewLine);
                receivedText.ScrollToEnd();
            }
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Username = usernameTextBox.Text;
            Properties.Settings.Default.OAuth = oauthTextBox.Password;
            Properties.Settings.Default.Save();
        }

        private void LoadSettings()
        {
            usernameTextBox.Text = Properties.Settings.Default.Username;
            oauthTextBox.Password = Properties.Settings.Default.OAuth;
        }
        

    }
}
