using AutoMapper;
using Billing.Models;

namespace Billing
{
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            CreateMap<UserProfile, User>().ReverseMap();
            CreateMap<Coin, UserCoin>().ReverseMap();
        }
    }
}
