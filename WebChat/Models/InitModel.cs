using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebChat.Models
{
    public class InitModel: PacketModel
    {
        public string socketId { get; set; }
        public InitModel()
        {
            packetType = PacketType.Init;
        }
    }
}
