using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TWMDocImg.Application.Configurations
{
    public class FileUploadOptions
    {
        public string[] AllowedExtensions { get; set; } = [];
        public long MaxFileSize { get; set; }
    }
}
