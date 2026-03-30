using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace ChatApp.Hubs
{
    public class ChatHub : Hub
    {
        private static readonly Dictionary<string, string> ConnectedUsers = new();
        private static readonly ConcurrentDictionary<string, FileTransferInfo> ActiveTransfers = new();
        private const long MaxFileSize = 2L * 1024 * 1024 * 1024; // 2GB limit

        public async Task JoinChat(string username)
        {
            ConnectedUsers[Context.ConnectionId] = username;
            await Groups.AddToGroupAsync(Context.ConnectionId, "ChatRoom");
            await Clients.Others.SendAsync("UserJoined", username);
            await Clients.Caller.SendAsync("JoinConfirmed", username);
        }

        public async Task SendMessage(string username, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", username, message, DateTime.Now.ToString("HH:mm"));
        }

        public async Task SendTyping(string username)
        {
            await Clients.Others.SendAsync("UserTyping", username);
        }

        public async Task StopTyping(string username)
        {
            await Clients.Others.SendAsync("UserStoppedTyping", username);
        }

        public async Task NotifyFileUploaded(string username, string fileName, string fileType, long fileSize, string fileId)
        {
            // Notify all other clients that a file has been uploaded
            await Clients.Others.SendAsync("FileUploaded", username, fileName, fileType, fileSize, fileId, DateTime.Now.ToString("HH:mm"));
        }

        public async Task StartFileTransfer(string username, string fileName, string fileType, long fileSize, string fileId)
        {
            if (fileSize > MaxFileSize)
            {
                await Clients.Caller.SendAsync("FileTransferError", "File too large. Maximum size is 2GB.");
                return;
            }

            // Store transfer info
            ActiveTransfers[fileId] = new FileTransferInfo
            {
                FileName = fileName,
                FileType = fileType,
                FileSize = fileSize,
                Username = username,
                StartTime = DateTime.Now,
                Chunks = new ConcurrentDictionary<int, string>()
            };

            // Notify all clients
            await Clients.All.SendAsync("FileTransferStarted", username, fileName, fileType, fileSize, fileId, DateTime.Now.ToString("HH:mm"));
        }

        public async Task SendFileChunk(string username, string fileName, string fileData, string fileType, int chunkIndex, int totalChunks, string fileId)
        {
            try
            {
                if (!ActiveTransfers.ContainsKey(fileId))
                {
                    await Clients.Caller.SendAsync("FileTransferError", "File transfer not found. Please restart the upload.");
                    return;
                }

                var transfer = ActiveTransfers[fileId];
                
                // Don't store chunks - just relay them immediately for speed
                // Only track count for completion detection
                transfer.Chunks.TryAdd(chunkIndex, ""); // Just mark as received, don't store data

                // Send chunk to other clients immediately (not back to sender)
                await Clients.Others.SendAsync("ReceiveFileChunk", username, fileName, fileData, fileType, chunkIndex, totalChunks, fileId, transfer.FileSize, DateTime.Now.ToString("HH:mm"));

                // Check if all chunks received
                if (transfer.Chunks.Count == totalChunks)
                {
                    // File complete - send completion signal
                    await Clients.All.SendAsync("FileTransferCompleted", username, fileName, fileId, totalChunks);
                    
                    // Clean up immediately after completion
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30));
                        ActiveTransfers.TryRemove(fileId, out var _);
                    });
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("FileTransferError", $"Error processing chunk: {ex.Message}");
            }
        }

        public async Task GetFileChunks(string fileId, int startChunk, int endChunk)
        {
            if (ActiveTransfers.TryGetValue(fileId, out var transfer))
            {
                for (int i = startChunk; i <= endChunk; i++)
                {
                    if (transfer.Chunks.TryGetValue(i, out var chunkData))
                    {
                        await Clients.Caller.SendAsync("ReceiveFileChunk", transfer.Username, transfer.FileName, 
                            chunkData, transfer.FileType, i, transfer.Chunks.Count, fileId, transfer.FileSize, 
                            transfer.StartTime.ToString("HH:mm"));
                    }
                }
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (ConnectedUsers.TryGetValue(Context.ConnectionId, out var username))
            {
                ConnectedUsers.Remove(Context.ConnectionId);
                await Clients.Others.SendAsync("UserLeft", username);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }

    public class FileTransferInfo
    {
        public string FileName { get; set; } = "";
        public string FileType { get; set; } = "";
        public long FileSize { get; set; }
        public string Username { get; set; } = "";
        public DateTime StartTime { get; set; }
        public ConcurrentDictionary<int, string> Chunks { get; set; } = new();
    }
}