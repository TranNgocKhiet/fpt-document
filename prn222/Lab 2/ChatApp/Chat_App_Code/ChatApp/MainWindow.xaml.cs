using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System.IO; 
using System.ComponentModel;
using System.Diagnostics;

namespace ChatApp
{
    public class StringToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(value as string) ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class MainWindow : Window
    {
        TcpClient client;
        NetworkStream stream;
        Thread receiveThread;


        string myName = "";
        bool isDarkMode = false;

        public MainWindow()
        {
            InitializeComponent();

            // Expand UI base on screen size
            this.Width = SystemParameters.PrimaryScreenWidth * 0.8;
            this.Height = SystemParameters.PrimaryScreenHeight * 0.8;
            this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
            this.Top = (SystemParameters.PrimaryScreenHeight - this.Height) / 2;
        }

        private void ToggleDarkMode_Click(object sender, RoutedEventArgs e)
        {
            isDarkMode = !isDarkMode;
            var bc = new BrushConverter();

            if (isDarkMode)
            {
                this.Resources["WindowBgBrush"] = (Brush)bc.ConvertFrom("#121212");

                lstChat.Background = (Brush)bc.ConvertFrom("#121212");
                (sender as Button).Content = new TextBlock { Text = "☀️", Foreground = Brushes.White, FontSize = 20 };
            }
            else
            {
                this.Resources["WindowBgBrush"] = (Brush)bc.ConvertFrom("#F5F7FB");

                lstChat.Background = Brushes.Transparent;
                (sender as Button).Content = new TextBlock { Text = "🌙", Foreground = (Brush)bc.ConvertFrom("#4F46E5"), FontSize = 20 };
            }
        }

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
                // Deactive the connect-to-server components
                btnConnect.IsEnabled = false;
                txtUsername.IsEnabled = false;
                btnConnect.Foreground = Brushes.Gray;

                // Active function buttons and textbox
                btnSend.IsEnabled = true;
                btnFile.IsEnabled = true;
                txtMessage.IsEnabled = true;
                btnEmoji.IsEnabled = true;

                // Connect and create a stream to server
                client = new TcpClient("26.82.61.76", 5000);
                stream = client.GetStream();

                // Send user name to server
                byte[] nameBuffer = Encoding.UTF8.GetBytes(myName);
                stream.Write(nameBuffer, 0, nameBuffer.Length);

                // Get a thread from server
                receiveThread = new Thread(ReceiveMessages);
                receiveThread.IsBackground = true;
                receiveThread.Start();

                AppendMessage("System: Connect Successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection error: " + ex.Message);
            }
        }

        private async void ReceiveMessages()
        {
            try
            {
                byte[] buffer = new byte[1024];
                while (client.Connected)
                {
                    // Await this to avoid blocking the UI but stay in order
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead <= 0) break;

                    string fullData = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (fullData.StartsWith("__FILE_START__|"))
                    {
                        // header include __FILE_START__|sender_name|file_name|file_size
                        string[] parts = fullData.Split('|');
                        string fileSender = parts[1];

                        // Skip the bytes for the sender
                        if (fileSender == myName)
                        {
                            long totalFileSize = long.Parse(parts[3]);
                            await SkipFileBytes(totalFileSize, fullData, bytesRead);
                        }
                        else
                        {
                            await HandleLargeFileDownload(fullData, buffer, bytesRead);
                        }
                    }
                    // skip enter message for the enterer
                    else if (!fullData.Contains($"System: {myName} has entered chat room."))
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Dispatcher.Invoke(() => AppendMessage(message));
                    }
                }
            }
            catch { /* Connection lost logic */ }
        }

        private async Task SkipFileBytes(long totalFileSize, string header, int initialRead)
        {
            // Calculate how many bytes of the file were already read in the header chunk
            int headerByteCount = Encoding.UTF8.GetByteCount(header.Substring(0, header.LastIndexOf('|') + 1));
            int leftover = initialRead - headerByteCount;

            long received = Math.Max(0, leftover);
            byte[] skipBuffer = new byte[81920];

            // Drain the stream until the file data is gone
            while (received < totalFileSize)
            {
                int toRead = (int)Math.Min(skipBuffer.Length, totalFileSize - received);
                int read = await stream.ReadAsync(skipBuffer, 0, toRead);
                if (read == 0) break;
                received += read;
            }
        }

        private async Task HandleLargeFileDownload(string header, byte[] initialBuffer, int initialRead)
        {
            try
            {
                string[] parts = header.Split('|');
                string fileSender = parts[1];  
                string fileName = parts[2];    
                long totalFileSize = long.Parse(parts[3]); 
                int headerByteCount = Encoding.UTF8.GetByteCount(header.Substring(0, header.LastIndexOf('|') + 1));

                ChatMessage chatMsg = null;
                Dispatcher.Invoke(() => {
                    chatMsg = new ChatMessage
                    {
                        // Get the actual sender's name
                        Sender = parts[0].Replace("__FILE_START__", "").Trim(), 
                        Message = $"📥 Receiving: {fileName}...",
                        Time = DateTime.Now.ToString("HH:mm"),
                        Alignment = HorizontalAlignment.Left,
                        BubbleColor = Brushes.LightGreen,
                        Progress = 0.1
                    };
                    lstChat.Items.Add(chatMsg);
                    lstChat.ScrollIntoView(chatMsg);
                });

                string tempPath = Path.Combine(Path.GetTempPath(), fileName);
                using (FileStream fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                {
                    int leftover = initialRead - headerByteCount;
                    if (leftover > 0) await fs.WriteAsync(initialBuffer, headerByteCount, leftover);

                    long received = Math.Max(0, leftover);
                    byte[] fileChunk = new byte[81920];

                    while (received < totalFileSize)
                    {
                        int toRead = (int)Math.Min(fileChunk.Length, totalFileSize - received);
                        int read = await stream.ReadAsync(fileChunk, 0, toRead);
                        if (read == 0) break;

                        await fs.WriteAsync(fileChunk, 0, read);
                        received += read;
                        double pct = (double)received / totalFileSize * 100;
                        _ = Dispatcher.BeginInvoke(new Action(() => chatMsg.Progress = pct));
                    }
                }

                Dispatcher.Invoke(() => {
                    chatMsg.Progress = 100;
                    chatMsg.Message = $"📁 {fileName} (Download file)";
                    chatMsg.FilePath = tempPath;
                });
            }
            catch (Exception ex) { Dispatcher.Invoke(() => AppendMessage("Error: " + ex.Message)); }
        }
        private void AppendMessage(string fullText)
        {
            var chatMsg = new ChatMessage { Time = DateTime.Now.ToString("HH:mm") };

            if (fullText.StartsWith("System:"))
            {
                chatMsg.Sender = "";
                chatMsg.Message = fullText.Replace("System:", "").Trim();
                chatMsg.Alignment = HorizontalAlignment.Center;
                chatMsg.BubbleColor = Brushes.LightGray;
            }
            else if (fullText.StartsWith(myName + ":"))
            {
                chatMsg.Sender = "You";
                chatMsg.Message = fullText.Substring(myName.Length + 1).Trim();
                chatMsg.Alignment = HorizontalAlignment.Right;
                chatMsg.BubbleColor = Brushes.LightSkyBlue;
            }
            else
            {
                int colonIndex = fullText.IndexOf(':');
                chatMsg.Sender = colonIndex != -1 ? fullText.Substring(0, colonIndex) : "Unknown";
                chatMsg.Message = colonIndex != -1 ? fullText.Substring(colonIndex + 1).Trim() : fullText;
                chatMsg.Alignment = HorizontalAlignment.Left;
                chatMsg.BubbleColor = Brushes.LightGreen;
            }

            if (chatMsg.Sender == "Unknown") return;

            lstChat.Items.Add(chatMsg);
            lstChat.ScrollIntoView(chatMsg);
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            if (stream != null && !string.IsNullOrWhiteSpace(txtMessage.Text))
            {
                string fullMessage = $"{myName}: {txtMessage.Text}";
                byte[] buffer = Encoding.UTF8.GetBytes(fullMessage);

                lock (stream)
                {
                    stream.Write(buffer, 0, buffer.Length);
                }

                AppendMessage(fullMessage);
                txtMessage.Clear();
            }
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

            if (btn.Content is Emoji.Wpf.TextBlock emojiBlock)
            {
                txtMessage.Text += emojiBlock.Text;
            }
            else
            {
                txtMessage.Text += btn.Content.ToString();
            }

            txtMessage.Focus();
            txtMessage.CaretIndex = txtMessage.Text.Length;
            popEmoji.IsOpen = false;
        }

        private void BtnSendFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                // parallel processing
                Task.Run(() => ProcessFileTransfer(filePath));
            }
        }

        private async Task ProcessFileTransfer(string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                string fileName = fileInfo.Name;
                long fileSize = fileInfo.Length;

                ChatMessage chatMsg = null;
                string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
                bool isImage = imageExtensions.Contains(Path.GetExtension(filePath).ToLower());
                byte[] localImageData = isImage ? File.ReadAllBytes(filePath) : null;
                Dispatcher.Invoke(() => {
                    chatMsg = new ChatMessage
                    {
                        Sender = "You",
                        ImageData = localImageData,
                        Message = isImage ? "" : $"📤 Sending: {Path.GetFileName(filePath)}...",
                        Time = DateTime.Now.ToString("HH:mm"),
                        Alignment = HorizontalAlignment.Right,
                        BubbleColor = Brushes.LightSkyBlue,
                        Progress = 0.1,
                        // Point to the local file
                        FilePath = filePath 
                    };
                    lstChat.Items.Add(chatMsg);
                    lstChat.ScrollIntoView(chatMsg);
                });

                byte[] header = Encoding.UTF8.GetBytes($"__FILE_START__|{myName}|{fileName}|{fileSize}");
                await stream.WriteAsync(header, 0, header.Length);

                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
                {
                    byte[] fileBuffer = new byte[81920];
                    int bytesRead;
                    long totalSent = 0;
                    while ((bytesRead = await fs.ReadAsync(fileBuffer, 0, fileBuffer.Length)) > 0)
                    {
                        await stream.WriteAsync(fileBuffer, 0, bytesRead);
                        totalSent += bytesRead;
                        double pct = (double)totalSent / fileSize * 100;
                        _ = Dispatcher.BeginInvoke(new Action(() => chatMsg.Progress = pct));
                    }
                }

                Dispatcher.Invoke(() => {
                    chatMsg.Progress = 100;
                    chatMsg.Message = $"📁 {fileName}\n(Download file)"; 
                    // Keep original path for reference
                    chatMsg.FilePath = Path.GetTempPath() + fileName;
                });
            }
            catch (Exception ex) { Dispatcher.Invoke(() => MessageBox.Show("Error: " + ex.Message)); }
        }

        private void OnMessageMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                var border = sender as Border;
                if (border?.DataContext is ChatMessage chatMsg && !string.IsNullOrEmpty(chatMsg.FilePath))
                {
                    // We "Download" it by  where to save it.
                    if (chatMsg.FilePath.Contains(Path.GetTempPath()))
                    {
                        SaveFileDialog saveFileDialog = new SaveFileDialog();
                        saveFileDialog.FileName = Path.GetFileName(chatMsg.FilePath);

                        if (saveFileDialog.ShowDialog() == true)
                        {
                            File.Copy(chatMsg.FilePath, saveFileDialog.FileName, true);
                            // Update path to the new permanent location
                            chatMsg.FilePath = saveFileDialog.FileName;
                            chatMsg.Message = $"📁 {Path.GetFileName(chatMsg.FilePath)} (Saved)";
                            MessageBox.Show("File downloaded and saved!");
                        }
                    }
                    else
                    {
                        if (File.Exists(chatMsg.FilePath))
                        {
                            Process.Start("explorer.exe", $"/select, \"{chatMsg.FilePath}\"");
                        }
                        else
                        {
                            MessageBox.Show("Original file no longer exists at this path.");
                        }
                    }
                }
            }
        }

        public class ChatMessage : INotifyPropertyChanged
        {
            public string Sender { get; set; }
            private string _message;
            public string Message 
            {
                get => _message;
                set {
                    _message = value;
                    OnPropertyChanged(nameof(Message));
                }
            }
            public string Time { get; set; }
            public HorizontalAlignment Alignment { get; set; }
            public Brush BubbleColor { get; set; }
            public string FilePath { get; set; }
            public string FileName { get; set; }
            public long FileSize { get; set; }

            private double _progress;
            public double Progress
            {
                get => _progress;
                set
                {
                    _progress = value;
                    OnPropertyChanged(nameof(Progress));
                    OnPropertyChanged(nameof(IsTransferring));
                }
            }

            private byte[] _imageData;
            public byte[] ImageData
            {
                get => _imageData;
                set { _imageData = value; OnPropertyChanged(nameof(ImageData)); }
            }

            public bool HasImage => ImageData != null;
            public bool IsTransferring => Progress > 0 && Progress < 100;

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}