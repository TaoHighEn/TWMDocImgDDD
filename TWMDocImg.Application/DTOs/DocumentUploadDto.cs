using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TWMDocImg.Application.DTOs;

public class DocumentUploadDto
{
    public Guid FileId { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required byte[] FileContent { get; set; }
}
