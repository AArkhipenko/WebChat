using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebChat.Models
{
    public class MessageModel: PacketModel
    {
        public string src_user { get; set; }
        public string message { get; set; }
        public string dst_user { get; set; }
        public MessageModel()
        {
            packetType = PacketType.Message;
        }
    }
}
