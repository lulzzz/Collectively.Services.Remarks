using System;
using System.Collections.Generic;

namespace Collectively.Services.Remarks.Dto
{
    public class TagDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public IEnumerable<TranslatedTagDto> Translations { get; set; }
    }
}