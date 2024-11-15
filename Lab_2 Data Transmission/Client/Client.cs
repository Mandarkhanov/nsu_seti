using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Lab2.Client;

public class Client : IDisposable
{
    private FileStream fileToSendStream;
    private TcpClient serverConnection;
    private long fileSize;
    private string fileName = null;
    private const int CHUNK_SIZE = 8192;
    private const string OK_MESSAGE = "ok";

    public Client(string path, IPAddress server, int port)
    {
        this.fileSize = new FileInfo(path).Length;
        string[] pathParts = path.Split(['\\', '/']);
        this.fileName = pathParts.Length > 1
                    ? pathParts[pathParts.Length - 1]
                    : pathParts[0];

        this.fileToSendStream = new FileStream(path, FileMode.Open);
        this.serverConnection = new TcpClient();
        this.serverConnection.Connect(server, port);
    }

    public Client(string path, DnsEndPoint server)
    {
        this.fileSize = new FileInfo(path).Length;
        string[] pathParts = path.Split(['\\', '/']);
        this.fileName = pathParts.Length > 1
                    ? pathParts[pathParts.Length - 1]
                    : pathParts[0];
        
        this.fileToSendStream = new FileStream(path, FileMode.Open, FileAccess.Read);
        this.serverConnection = new TcpClient();
        this.serverConnection.Connect(server.Host, server.Port);
    }

    public async Task HandleFile()
    {
        using NetworkStream tcpStream = this.serverConnection.GetStream();

        // Клиент первым шагом отправляет серверу имя файла и размер
        await tcpStream.WriteAsync(Encoding.Default.GetBytes(this.fileName + "/" + this.fileSize));

        // Вторым шагом, клиент ожидает получение подтверждающего сообщения от клиента
        byte[] buffer = new byte[CHUNK_SIZE];
        int readBytes = await tcpStream.ReadAsync(buffer, 0, CHUNK_SIZE);

        if (Encoding.Default.GetString(buffer, 0 , readBytes) == OK_MESSAGE)
        {
            while (await this.fileToSendStream.ReadAsync(buffer, 0, CHUNK_SIZE) > 0)
            {
                await tcpStream.WriteAsync(buffer);
            }

            Array.Clear(buffer);
            readBytes = await tcpStream.ReadAsync(buffer, 0, CHUNK_SIZE);

            if (Encoding.Default.GetString(buffer, 0, readBytes) == "done")
            {
                Console.WriteLine("Файл передан удачно");
            }
            else
            {
                Console.WriteLine("Файл не был передан удачно");
                throw new Exception("no done after work");
            }
        }
        else
        {
            Console.WriteLine(readBytes);
            throw new Exception("no ok before work");
        }
    }

    public void Dispose()
    {
        this.fileToSendStream.Close();
        this.serverConnection.Close();
    }
}