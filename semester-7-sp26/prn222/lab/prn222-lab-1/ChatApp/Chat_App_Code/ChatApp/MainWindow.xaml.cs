using System;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading;

namespace ChatApp
{
    public partial class MainWindow : Window
    {
        TcpClient client;
        NetworkStream stream;
        Thread receiveThread;

        public MainWindow()
        {
            InitializeComponent();
        }

        string myName = "";

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            myName = txtUsername.Text;
            if (string.IsNullOrEmpty(myName))
            {
                MessageBox.Show("Please enter your name!");
                return;
            }
            try
            {
                client = new TcpClient("26.82.61.76", 5000);
                stream = client.GetStream();

                byte[] nameBuffer = Encoding.UTF8.GetBytes(myName);
                stream.Write(nameBuffer, 0, nameBuffer.Length);

                receiveThread = new Thread(ReceiveMessages);
                receiveThread.IsBackground = true;
                receiveThread.Start();

                btnConnect.IsEnabled = false;
                AppendMessage("System: Connect Successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection error: " + ex.Message);
            }
        }

        private void ReceiveMessages()
        {
            try
            {
                byte[] buffer = new byte[1024];
                while (true)
                {
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    if (bytes > 0)
                    {
                        string msg = Encoding.UTF8.GetString(buffer, 0, bytes);

                        Dispatcher.Invoke(() => AppendMessage(msg));
                    }
                }
            }
            catch
            {
                Dispatcher.Invoke(() => AppendMessage("System: Server connection lost"));
            }
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            if (stream != null && !string.IsNullOrEmpty(txtMessage.Text))
            {
                string fullMessage = $"{myName}: {txtMessage.Text}";
                byte[] buffer = Encoding.UTF8.GetBytes(fullMessage);
                stream.Write(buffer, 0, buffer.Length);

                txtMessage.Clear();
            }
        }

        private void AppendMessage(string message)
        {
            lstChat.Items.Add($"{DateTime.Now:HH:mm} - {message}");
            lstChat.ScrollIntoView(lstChat.Items[lstChat.Items.Count - 1]);
        }

        private void TxtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnSend_Click(this, new RoutedEventArgs());
            }
        }
        private void BtnEmoji_Click(object sender, RoutedEventArgs e)
        {
            popEmoji.IsOpen = !popEmoji.IsOpen;
        }
        private void AddEmoji_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            txtMessage.Text += btn.Content.ToString(); 
            txtMessage.Focus();
            txtMessage.CaretIndex = txtMessage.Text.Length; 
            popEmoji.IsOpen = false; 
        }
    }
}