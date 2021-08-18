using AutoMapper;
using Azure.Api.Data.DTOs.Document;
using Azure.Api.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Api.Data.Mapper
{
    public class DocumentTypeProfile : Profile
    {
        public DocumentTypeProfile()
        {
            CreateMap<DocumentType, DocumentTypeDTO>()
                .ReverseMap();
        }
    }
}
