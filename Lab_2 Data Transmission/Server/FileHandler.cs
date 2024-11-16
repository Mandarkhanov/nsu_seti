using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Lab2.Server;

public class FileHandler : IDisposable
{
    private readonly TcpClient client;
    private FileStream fileStream;
    private string clientFileName;
    private string serverFileName;
    private long fileSize;
    private long readSize = 0L;
    private long lastReadSize;
    private TimeOnly startTime;
    private const int CHUNK_SIZE = 8192;
    private const int MAX_FILE_NAME_BYTE_SIZE = 4096;
    private const string OK_MESSAGE = "ok";
    private const int TIMER_DUE_TIME = 0;
    private const int TIMER_PERIOD = 3000;
    
    public FileHandler(TcpClient client)
    {
        this.client = client;
    }

    public async void HandleConnection()
    {
        using (this)
        {
            NetworkStream tcpStream = this.client.GetStream();
            byte[] buffer = new byte[CHUNK_SIZE];
            await tcpStream.ReadAsync(buffer, 0, CHUNK_SIZE);
            
            ParseFileMetadata(buffer);
            CreateFile();

            await tcpStream.WriteAsync(Encoding.Default.GetBytes(OK_MESSAGE), 0, OK_MESSAGE.Length);

            this.startTime = TimeOnly.FromDateTime(DateTime.Now);
            Timer timer = new Timer((object? o) => { PrintSpeed(); }, null, TIMER_DUE_TIME, TIMER_PERIOD);

            while (this.readSize < this.fileSize)
            {
                int count = tcpStream.ReadAsync(buffer, 0, CHUNK_SIZE).Result;
                await this.fileStream.WriteAsync(buffer, 0, count);
                this.readSize += count;
            }
            
            timer.Change(Timeout.Infinite, Timeout.Infinite);

            await tcpStream.WriteAsync(Encoding.Default.GetBytes("done"), 0, "done".Length);

            PrintSpeed();
            Console.WriteLine("Файл " + this.clientFileName + " [" + this.client.Client.RemoteEndPoint + "] " + " получен удачно");
        }
    }

    private void ParseFileMetadata(byte[] buffer)
    {
        string stringifiedBuffer = Encoding.Default.GetString(buffer);
        string[] data = stringifiedBuffer.Split('/');
        this.clientFileName = data[0];
        this.fileSize = long.Parse(data[1]);
    }

    private void CreateFile()
    {
        if (!File.Exists("uploads/" + this.clientFileName))
        {
            this.serverFileName = "uploads/" + this.clientFileName;
        }
        else
        {
            this.serverFileName = "uploads/" + Random.Shared.Next()  + "_" + this.clientFileName;
        }
        this.fileStream = new FileStream(this.serverFileName, FileMode.CreateNew);
    }

    private void PrintSpeed()
    {
        TimeOnly now = TimeOnly.FromDateTime(DateTime.Now);

        double totalSpeed = this.readSize / (now - this.startTime).TotalSeconds / 1024 / 1024;
        double currentSpeed = (double)(this.readSize - this.lastReadSize) / double.Min((now - this.startTime).TotalSeconds, 3) / 1024 / 1024;
        this.lastReadSize = this.readSize;

        if (totalSpeed < 0.001) {
            Console.WriteLine("Файл {0} [{1}] начал скачиваться", this.serverFileName, this.client.Client.RemoteEndPoint);
        }
        else
        {
            Console.WriteLine("Файл {2} [{3}]\n\t общая скорость {0:F3} мнгновенная скорость {1:F3} МБ", totalSpeed, currentSpeed, this.serverFileName, this.client.Client.RemoteEndPoint);
        }
    }

    public void Dispose()
    {
        this.fileStream.Close();
        this.client.Close();
    }
}