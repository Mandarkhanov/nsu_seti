class Program
{
    private const int SEND_LIFE_TIME = 1000;
    private const int PRINT_LIFE_TIME = 4000;

    static void Main(string[] args)
    {
        Searcher searcher;
        if (args.Length != 0)
        {
            searcher = new Searcher(args[0]);
        }
        else
        {
            searcher = new Searcher();
        }

        Timer sendTimer = new Timer((object? obj) => {searcher.Send();}, null, 0, SEND_LIFE_TIME);
        Timer printTimer = new Timer((object? obj) => {searcher.PrintAliveCopies(); }, null, 0, PRINT_LIFE_TIME);
        searcher.GetCopies();

        while (true) 
        { 
            Thread.Sleep(1000); 
        }
    }
}