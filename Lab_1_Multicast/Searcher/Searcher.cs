using System.Net;
using System.Net.Sockets;
using System.Text;

class Searcher
{
    private string multicastAddress;
    private int port;
    private IPEndPoint localEndPoint;
    private IPEndPoint remoteEndPoint; 
    private Dictionary<string, DateTime> aliveCopies = new Dictionary<string, DateTime>();
    private UdpClient udpClient = new UdpClient();
    private const int COPY_LIFE_TIME = 3;

    public string GetMulticastAddress()
    {
        return multicastAddress;
    }

    public int GetPort()
    {
        return port;
    }

    public IPEndPoint GetIPEndPoint()
    {
        return localEndPoint;
    }

    public Searcher()
    {
        this.multicastAddress = "224.0.0.1";
        this.port = 12345;
        this.localEndPoint = new IPEndPoint(IPAddress.Any, this.port);
        this.remoteEndPoint = new IPEndPoint(IPAddress.Parse(this.multicastAddress), this.port);
        InitUdpClient(); 
    }

    public Searcher(string multicastAddress)
    {
        this.multicastAddress = multicastAddress;
        this.port = 12345;
        this.localEndPoint = new IPEndPoint(IPAddress.Any, port);
        this.remoteEndPoint = new IPEndPoint(IPAddress.Parse(multicastAddress), port);
        InitUdpClient();
    }

    public void InitUdpClient()
    {
        this.udpClient.ExclusiveAddressUse = false;
        this.udpClient.Client.SetSocketOption(SocketOptionLevel.Socket,
                                            SocketOptionName.ReuseAddress,
                                            true);
        this.udpClient.Client.Bind(this.localEndPoint);
        this.udpClient.JoinMulticastGroup(IPAddress.Parse(this.multicastAddress));
    }

    public void RegisterCopy(string endPoint)
    {
        lock (aliveCopies) {
            if (!aliveCopies.ContainsKey(endPoint))
            {
                aliveCopies.Add(endPoint, DateTime.Now);
            }
            else
            {
                aliveCopies[endPoint] = DateTime.Now;
            }
        }
    }

    public void PrintAliveCopies()
    {
        lock (aliveCopies)
        {
            DateTime now = DateTime.Now;
            Dictionary<string, DateTime> checkedAliveCopies = new Dictionary<string, DateTime>();
            foreach (var pair in aliveCopies)
            {
                if ((now - pair.Value).TotalSeconds < COPY_LIFE_TIME)
                {
                    checkedAliveCopies.Add(pair.Key, pair.Value);
                }
            }
            aliveCopies = checkedAliveCopies;
            foreach (var pair in aliveCopies)
            {
                Console.WriteLine($"{pair.Key} still alive");
            }
            Console.WriteLine();
        }
    }

    public async void GetCopies()
    {
        while (true)
        {
            var result = await udpClient.ReceiveAsync();
            IPEndPoint receivedEndPoint = result.RemoteEndPoint;
            RegisterCopy(receivedEndPoint.ToString());
        }
    }

    public void Send()
    {
        byte[] message = Encoding.UTF8.GetBytes("Hello multicast!"); 
        udpClient.Send(message, message.Length, remoteEndPoint);
    }
}