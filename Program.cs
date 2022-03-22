using System;

namespace MITM
{
    class Program
    {
        static void Main(string[] args)
        {
            MITM mitm = new MITM();
            mitm.Run().Wait();
        }
    }
}
