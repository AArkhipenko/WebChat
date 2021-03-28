using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebChat.Models
{
    public class ClientListModel: PacketModel
    {
        public List<string> clientList { get; set; }

        public ClientListModel()
        {
            packetType = PacketType.List;
            clientList = new List<string>();
        }
    }
}
