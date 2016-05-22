using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Twitch_Chat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IRC irc = new IRC();
        private API api = new API();
        private Task _listen;
        private CancellationTokenSource _tokenSource;
        private CancellationToken _cancellationToken;
        private string _accessToken = "";

        public MainWindow()
        {
            InitializeComponent();
            irc.Connect();
            LoadSettings();
        }


        private async void Send(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string message = sendTextBox.Text;
                if (message.StartsWith("/api|"))
                {
                    message = message.Replace("/api|", "");
                    var messageValues = new List<string>(message.Split('|'));

                    string endpoint = messageValues.FirstOrDefault();
                    messageValues.Remove(endpoint);

                    string value = string.Join("|", messageValues.ToArray());
                    
                    await api.CallMethod(endpoint, value, _accessToken);
                }
                else if (await irc.SendMessage(message))
                {
                    UpdateText($"{usernameTextBox.Text}: {message}");
                    sendTextBox.Text = "";
                }
            }
        }

        private async void Join(object sender, RoutedEventArgs e)
        {
            if (_listen != null)
            {
                _tokenSource.Cancel();
                await Task.Delay(1000);
            }
            if (await irc.JoinChannel(channelTextBox.Text))
            {
                _tokenSource = new CancellationTokenSource();
                _cancellationToken = _tokenSource.Token;
                _listen = Task.Factory.StartNew(Listen, _cancellationToken);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_listen != null)
            {
                if (!_listen.IsCanceled)
                {
                    _tokenSource.Cancel();
                }
            }
            base.OnClosed(e);
        }


        public async Task Listen()
        {
            string message = "";
            string username = "";

            while (!_cancellationToken.IsCancellationRequested)
            {
                message = await irc.Receive(_cancellationToken);
                if (message != "" && message != "PING :tmi.twitch.tv\r\n")
                {
                    if (message.Contains(".tmi.twitch.tv PRIVMSG #"))
                    {
                        username = message.Substring(message.IndexOf(':') + 1, message.IndexOf('!') - 1);
                        message = message.Remove(0, message.IndexOf(".tmi.twitch.tv PRIVMSG #"));
                        message = message.Replace(".tmi.twitch.tv PRIVMSG #" + irc.Channel + " :", ": ");
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
                    ThreadStart start = delegate ()
                    {
                        DispatcherOperation operation = Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                          new Action<string>(UpdateText), message);
                        DispatcherOperationStatus status = operation.Status;
                        while (status != DispatcherOperationStatus.Completed)
                        {
                            status = operation.Wait(TimeSpan.FromMilliseconds(100));
                        }
                    };
                    new Thread(start).Start();
                }
                else if (message.IndexOf("PING :tmi.twitch.tv") == 0)
                {
                    message = message.Replace("PING", "PONG");
                    if (!await irc.Send(message))
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
            Dictionary<string, string> settings = new Dictionary<string, string>();
            settings.Add("username", usernameTextBox.Text);
            settings.Add("oauth", oauthTextBox.Password);
            UpdateSettings(settings);
        }

        private void LoadSettings()
        {
            usernameTextBox.Text = ConfigurationManager.AppSettings["username"];
            oauthTextBox.Password = ConfigurationManager.AppSettings["oauth"];
        }

        private void UpdateSettings(Dictionary<string, string> keys)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            foreach (KeyValuePair<string, string> kvp in keys)
            {
                configuration.AppSettings.Settings[kvp.Key].Value = kvp.Value;
            }

            configuration.Save();

            ConfigurationManager.RefreshSection("appSettings");
        }

        private void Connect(object sender, RoutedEventArgs e)
        {
            string endpoint = $"https://api.twitch.tv/kraken/oauth2/authorize?response_type=token&client_id={ConfigurationManager.AppSettings["clientId"]}&redirect_uri=http://twitch.chat.localhost&scope=channel_editor&force_verify=true";
            browser.Navigate(endpoint);
        }

        private void OnBrowserNavigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (e.Uri != null)
            {
                if (e.Uri.Fragment.Contains("#access_token"))
                {
                    _accessToken = e.Uri.Fragment.Split('&')[0].Split('=')[1];
                }
            }
        }
    }
}
