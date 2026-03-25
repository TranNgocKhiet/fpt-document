using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml.Linq;

class Server
{
    static List<TcpClient> clients = new List<TcpClient>();

    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        TcpListener server = new TcpListener(IPAddress.Any, 5000);
        server.Start();
        Console.WriteLine("Server run on port 5000...");

        while (true)
        {
            // Accept connection from client and add client to list
            TcpClient client = server.AcceptTcpClient();
            clients.Add(client);
            Console.WriteLine("New client has connected!");
            // Create a thread for connected client to serve each client individually
            Thread ctThread = new Thread(() => HandleClient(client));
            ctThread.Start();
        }
    }

    static void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        // Use a larger buffer for files
        byte[] buffer = new byte[81920]; 
        string clientName = "";

        try
        {
            // Get the client name first
            int nameBytes = stream.Read(buffer, 0, buffer.Length);
            clientName = Encoding.UTF8.GetString(buffer, 0, nameBytes);
            Console.WriteLine($"==> {clientName} connected!");
            Broadcast(Encoding.UTF8.GetBytes($"System: {clientName} has entered chat room."));

            int byteCount;
            while ((byteCount = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                string preview = Encoding.UTF8.GetString(buffer, 0, Math.Min(byteCount, 100));

                Broadcast(buffer, byteCount, client);
            }
        }
        catch
        {
            clients.Remove(client);
        }
    }

    static void Broadcast(byte[] data, int count, TcpClient excludeClient)
    {
        foreach (var c in clients.ToArray())
        {
            // Skip the person who sent the meassage so they don't get duplicated message
            if (c == excludeClient) continue;

            try
            {
                if (c.Connected)
                {
                    NetworkStream stream = c.GetStream();
                    stream.Write(data, 0, count);
                }
            }
            catch
            {
                clients.Remove(c);
            }
        }
    }

    // Helper for strings (System messages)
    static void Broadcast(byte[] data) => Broadcast(data, data.Length, null);
}