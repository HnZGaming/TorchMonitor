using System;
using System.Net;
using System.Linq;

namespace Quickies
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            const uint ip = 2886832273;
            Console.WriteLine($"ip: {ip}");
            Console.WriteLine(new IPAddress(ip));

            var ipReversed = BitConverter.GetBytes(ip).Reverse().ToArray();
            Console.WriteLine(new IPAddress(ipReversed));
        }
    }
}