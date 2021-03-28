using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebChat.Models
{
    public enum PacketType
    {
        Init = 1,
        Message,
        List
    }
    public class PacketModel
    {
        public PacketType packetType { get; set; }
    }
}
