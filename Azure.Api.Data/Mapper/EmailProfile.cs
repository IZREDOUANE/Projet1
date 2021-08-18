using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Azure.Api.Data.DTOs;
using Azure.Api.Data.Models;

namespace Azure.Api.Data.Mapper
{
    public class EmailProfile : Profile
    {
        public EmailProfile()
        {
            CreateMap<EmailDTO, Email>()
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Body))
                .ForMember(dest => dest.Object, opt => opt.MapFrom(src => src.Subject))
                .ForMember(dest => dest.Recipient, opt => opt.MapFrom(src => src.To))
                .ForMember(dest => dest.Title, opt => opt.NullSubstitute(""));

            CreateMap<Email, EmailDTO>()
                .ForMember(dest => dest.Body, opt => opt.MapFrom(src => src.Content))
                .ForMember(dest => dest.Subject, opt => opt.MapFrom(src => src.Object))
                .ForMember(dest => dest.To, opt => opt.MapFrom(src => src.Recipient));
        }
    }
}
