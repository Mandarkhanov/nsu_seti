using System.Net;
using System.Net.Sockets;

namespace Lab2.Server;

public class Server
{
    private int port;

    public Server(int port)
    {
        this.port = port;
        SetupDirectoryForFiles();
    }

    private void SetupDirectoryForFiles()
    {
        if (!Directory.Exists("uploads"))
        {
            Directory.CreateDirectory("uploads");
        }
    }

    public void StartServer()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, this.port);
        try
        {
            listener.Start();
            Console.WriteLine("The server is running, waiting for connections\n");
            
            while (true)
            { 
                TcpClient newClient = listener.AcceptTcpClientAsync().Result;
                FileHandler handler = new FileHandler(newClient);
                Task.Run(handler.HandleConnection);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            listener.Stop();
            Console.WriteLine("The server was stopped\n");
        }
    }
}