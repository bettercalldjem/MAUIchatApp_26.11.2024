using Microsoft.Maui.Controls;
using SQLite;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.ObjectModel;

namespace ChatApp
{
    public partial class ChatPage : ContentPage
    {
        private UdpService udpService;
        private DatabaseService databaseService;
        private string username;

        // ��������� ���������, ������� ����� ��������� � ListView
        public ObservableCollection<Message> Messages { get; set; }

        public ChatPage(string username)
        {
            InitializeComponent();
            this.username = username;

            udpService = new UdpService();
            udpService.OnMessageReceived += (message) => DisplayReceivedMessage(message);

            string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "chat.db");
            databaseService = new DatabaseService(dbPath);

            // ��������� ����������� ��������� �� ���� ������
            var savedMessages = databaseService.GetMessages();
            Messages = new ObservableCollection<Message>(savedMessages);

            // ������������� �������� ������
            BindingContext = this;

            Task.Run(() => udpService.ListenForMessages());
        }

        private async void OnSendMessageClicked(object sender, EventArgs e)
        {
            string messageText = MessageEntry.Text;
            if (!string.IsNullOrEmpty(messageText))
            {
                // ���������� � ���� ������
                var message = new Message
                {
                    Username = username,
                    Text = messageText,
                    Timestamp = DateTime.Now
                };

                // ��������� � ����
                databaseService.SaveMessage(message);

                // ��������� ��������� � ���������
                Messages.Add(message);

                // �������� ����� UDP
                await udpService.SendMessage(messageText);

                // �������� ���� �����
                MessageEntry.Text = string.Empty;
            }
        }

        private void DisplayReceivedMessage(string message)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                // ��������� ���������� ��������� � ���������
                Messages.Add(new Message
                {
                    Username = "������ ������������",
                    Text = message,
                    Timestamp = DateTime.Now
                });
            });
        }
    }

    // ����� ��� ���������
    public class Message
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Username { get; set; }
        public string Text { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // ������ ��� ������ � ����� ������
    public class DatabaseService
    {
        private SQLiteConnection _connection;

        public DatabaseService(string dbPath)
        {
            _connection = new SQLiteConnection(dbPath);
            _connection.CreateTable<Message>();
        }

        public void SaveMessage(Message message)
        {
            _connection.Insert(message);
        }

        public List<Message> GetMessages()
        {
            return _connection.Table<Message>().OrderBy(m => m.Timestamp).ToList();
        }
    }

    // ������ ��� �������� � ��������� ��������� ����� UDP
    public class UdpService
    {
        private UdpClient udpClient;
        private IPEndPoint endPoint;

        public UdpService()
        {
            udpClient = new UdpClient();
            endPoint = new IPEndPoint(IPAddress.Broadcast, 12345); // ����������������� ��������
        }

        public async Task SendMessage(string message)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            await udpClient.SendAsync(messageBytes, messageBytes.Length, endPoint);
        }

        public async Task ListenForMessages()
        {
            while (true)
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync();
                string receivedMessage = Encoding.UTF8.GetString(result.Buffer);
                OnMessageReceived(receivedMessage);
            }
        }

        public event Action<string> OnMessageReceived = delegate { };
    }
}
