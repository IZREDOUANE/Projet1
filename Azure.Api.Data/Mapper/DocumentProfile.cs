using AutoMapper;
using Azure.Api.Data.DTOs;
using Azure.Api.Data.Models;
using System.Linq;

namespace Azure.Api.Data.Mapper
{
    public class DocumentProfile: Profile
    {
        public DocumentProfile()
        {
            CreateMap<Document, DocumentDTO>()
                .ForMember(dest => dest.FileCategory, act => act.Ignore())
                .ForMember(dest => dest.GUID, opt => opt.MapFrom(src => src.GUID));

            CreateMap<DocumentDTO, Document>()
                .ForMember(dest => dest.FileCategory, act => act.Ignore())
                .ForMember(dest => dest.GUID, opt => opt.MapFrom(src => src.GUID));

            CreateMap<DocumentVersion, DocumentVersionDTO>();

            CreateMap<DocumentVersionDTO, DocumentVersion>();

            CreateMap<Document, DocumentViewDTO>()
                .ForMember(dest => dest.Owners, opt => opt.MapFrom(src => src.DocumentAccessFkNav.Select(n => n.AllowedSfId).ToList()))
                .ForMember(dest => dest.FileCategory, act => act.Ignore());

            CreateMap<DocumentViewDTO, Document>()
                .ForMember(dest => dest.FileCategory, act => act.Ignore());
        }
    }
}
