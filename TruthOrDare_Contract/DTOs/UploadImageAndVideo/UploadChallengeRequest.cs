﻿using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Contract.DTOs.UploadImageAndVideo
{
    public class UploadChallengeRequest
    {
        public string RoomId { get; set; }
        public string PlayerId { get; set; }
        public string QuestionId { get; set; }
        public IFormFile File { get; set; }
    }
}
