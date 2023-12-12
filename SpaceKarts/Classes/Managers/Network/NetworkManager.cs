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
        public static bool Enabled = false;
        internal static Client Client { get; set; }

        public static void Connect(string ip, int port)
        {
            Client = new Client();
            Client.ClientConnected += (s, e) => Debug.WriteLine($"connected as {e.Id}");
            Client.ClientDisconnected += (s, e) => Player.List.Remove(e.Id);

            bool connected = Client.Connect($"{ip}:{port}");

           
        }
        public static void UpdateClient()
        {
            if(!Enabled) return;
            Client.Update();
        }
        public static void DisconnectClient()
        {
            if (!Enabled) return;
            if(Client != null && Client.IsConnected)
                Client.Disconnect();
        }
    }
}