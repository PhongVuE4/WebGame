using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Contract.DTOs.UploadImageAndVideo
{
    public class UploadChallengeRequest
    {
        public IFormFile File { get; set; }
        public string RoomId { get; set; }
        public string PlayerId { get; set; }
    }
}
