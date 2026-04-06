using System.Net.Sockets;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

while (true)
{
    Console.Write("Enter category or ALL (type EXIT to quit): ");
    string? command = Console.ReadLine();

    if (string.Equals(command, "EXIT", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    if (string.IsNullOrWhiteSpace(command))
    {
        Console.WriteLine("Command must not be empty.");
        continue;
    }

    try
    {
        using TcpClient client = new();
        await client.ConnectAsync("127.0.0.1", 5000);

        using NetworkStream stream = client.GetStream();

        byte[] requestBytes = Encoding.UTF8.GetBytes(command);
        await stream.WriteAsync(requestBytes);
        await stream.FlushAsync();

        client.Client.Shutdown(SocketShutdown.Send);

        using MemoryStream memoryStream = new();
        byte[] buffer = new byte[1024];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
        {
            await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead));
        }

        string response = Encoding.UTF8.GetString(memoryStream.ToArray());
        Console.WriteLine("Server response:");
        Console.WriteLine(response);
    }
    catch (SocketException ex)
    {
        Console.WriteLine($"Socket error: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unexpected error: {ex.Message}");
    }

    Console.WriteLine();
}
