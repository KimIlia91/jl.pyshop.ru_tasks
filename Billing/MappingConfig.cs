using AutoMapper;
using Billing.Models;

namespace Billing
{
    /// <summary>
    /// Класс мапинг для трансфомарции моделей для передачи в БД и наоборот для передачи из БД
    /// </summary>
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            CreateMap<UserProfile, User>().ReverseMap();
            CreateMap<Coin, UserCoin>().ReverseMap();
        }
    }
}
