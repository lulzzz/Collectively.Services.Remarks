using System;

namespace Coolector.Services.Remarks.Shared.Dto
{
    public class FileDto
    {
        public Guid GroupId { get; set; }
        public string Name { get; set; }
        public string Size { get; set; }
        public string Url { get; set; }
        public string Metadata { get; set; }
    }
}