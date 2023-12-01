using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Riptide;

namespace SpaceKarts.Managers
{
    internal class NetworkManager
    {
        internal static Client Client { get; set; }

        public static void Connect(string ip, int port)
        {
            Client = new Client();
            Client.ClientConnected += (s, e) => Debug.WriteLine($"connected as {e.Id}");
            Client.ClientDisconnected += (s, e) => Player.List.Remove(e.Id);

            bool connected = Client.Connect($"{ip}:{port}");

           
        }

        
    }
}