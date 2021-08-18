using AutoMapper;
using Azure.Api.Data.DTOs;
using Azure.Api.Data.Models;

namespace Azure.Api.Data.Mapper
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, UserDTO>();

            CreateMap<UserDTO, User>();

            CreateMap<User, AccountEntrepriseDTO>()
                .ForMember(dest => dest.Nom, opt => opt.MapFrom(u => u.LastName))
                .ForMember(dest => dest.Prenom, opt => opt.MapFrom(u => u.FirstName));

            CreateMap<AccountEntrepriseDTO, User>()
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(u => u.Nom))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(u => u.Prenom));

            CreateMap<User, AuthenticationDTO>();

            CreateMap<AuthenticationDTO, User>();
        }
    }
}
