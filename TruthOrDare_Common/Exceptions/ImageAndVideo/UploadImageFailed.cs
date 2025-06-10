using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.ImageAndVideo
{
    public class UploadImageFailed : Exception
    {
        public UploadImageFailed() : base($"Upload image failed. Please try again later.")
        {
        }
    }
}
