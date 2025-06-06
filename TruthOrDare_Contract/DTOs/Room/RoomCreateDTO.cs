﻿using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruthOrDare_Contract.Models;
using TruthOrDare_Contract.DTOs.Player;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace TruthOrDare_Contract.DTOs.Room
{
    public class RoomCreateDTO
    {
        public string RoomId { get; set; }
        [Required(ErrorMessage = "Room name is required.")]
        public string RoomName { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public string RoomPassword { get; set; }
        public int PlayerCount { get; set; }
        public int MaxPlayer { get; set; }
        public List<PlayerCreateRoomDTO> Players { get; set; } = new List<PlayerCreateRoomDTO>();
        public string Status { get; set; }
        public string AgeGroup { get; set; }
        public string Mode { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
