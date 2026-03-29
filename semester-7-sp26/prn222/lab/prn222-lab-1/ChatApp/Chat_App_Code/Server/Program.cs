using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

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
            TcpClient client = server.AcceptTcpClient();
            clients.Add(client);
            Console.WriteLine("New client has connected!");

            Thread ctThread = new Thread(() => HandleClient(client));
            ctThread.Start();
        }
    }

    static void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        string clientName = "";

        try
        {
            int nameBytes = stream.Read(buffer, 0, buffer.Length);
            clientName = Encoding.UTF8.GetString(buffer, 0, nameBytes);

            Console.WriteLine($"==> {clientName} is connected!");
            Broadcast($"System: {clientName} has entered chat room.");

            int byteCount;
            while ((byteCount = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, byteCount);

                Console.WriteLine($"[{DateTime.Now:HH:mm}] {message}");

                Broadcast(message);
            }
        }
        catch
        {
            if (!string.IsNullOrEmpty(clientName))
            {
                Console.WriteLine($"<-- {clientName} has left chat room.");
                Broadcast($"System: {clientName} has left chat room.");
            }
            clients.Remove(client);
        }
    }
    static void Broadcast(string message)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        foreach (var c in clients.ToArray())
        {
            try
            {
                if (c.Connected) 
                {
                    NetworkStream stream = c.GetStream();
                    stream.Write(buffer, 0, buffer.Length);
                }
            }
            catch
            {
                clients.Remove(c);
            }
        }
    }
}