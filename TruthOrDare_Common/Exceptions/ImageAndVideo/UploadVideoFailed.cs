﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruthOrDare_Common.Exceptions.ImageAndVideo
{
    public class UploadVideoFailed : Exception
    {
        public UploadVideoFailed() : base("Upload video failed.")
        {
        }
    }
}
