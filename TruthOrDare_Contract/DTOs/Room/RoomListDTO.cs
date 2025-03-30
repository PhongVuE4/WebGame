using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Contract.DTOs.Room
{
    public class RoomListDTO
    {
        public string RoomId { get; set; }
        public string RoomName { get; set; }
        public string HostName { get; set; }
        public int PlayerCount { get; set; }
        public int MaxPlayer {  get; set; }
        public bool HasPassword { get; set; }
        public string Status { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }
}
