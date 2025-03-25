﻿using System;
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
        public int PlayerCount { get; set; }
        public bool HasPassword { get; set; }
    }
}
