using AutoMapper;
using Billing;
using Billing.Data;
using Billing.Models;
using Billing.Services;
using Grpc.Core;

namespace Billing.Services
{
    /// <summary>
    /// Сервис Биллинга монет
    /// </summary>
    public class BillingService : Billing.BillingBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public BillingService(
            ApplicationDbContext db,
            IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        /// <summary>
        /// возвращает поток списка пользователей с их счётом
        /// </summary>
        /// <param name="request">отправляем пустой запрос None()</param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task ListUsers(
            None request,
            IServerStreamWriter<UserProfile> responseStream,
            ServerCallContext context)
        {
            var usersList = _db.Users.ToList();
            var userProfilesList = _mapper.Map<List<UserProfile>>(usersList);
            foreach (var userProfile in userProfilesList)
            {
                await responseStream.WriteAsync(userProfile);
            }
        }

        /// <summary>
        /// Эимиссия монет монеты распределяются между всеми согласно рейтингу пользователей
        /// каждый получает хотябы одну монету
        /// </summary>
        /// <param name="request">надо передать количество монет для эмиссии</param>
        /// <param name="context"></param>
        /// <returns>Возвращает ответ: Статус и комментарий</returns>
        public override Task<Response> CoinsEmission(EmissionAmount request, ServerCallContext context)
        {
            var usersList = _db.Users.ToList();
            var coinsToDistribute = request.Amount;
            if (usersList.Count < 1 || coinsToDistribute < 1) 
                return Task.FromResult(new Response
                {
                    Status = Response.Types.Status.Failed,
                    Comment = $"There are no users or coins to emission."
                });
            var tupleCoinsEmissionUserCoins = GetCoinsEmissionAndUsersCoins(coinsToDistribute, usersList);
            _db.UserCoins.AddRange(tupleCoinsEmissionUserCoins.Item1);
            _db.Users.UpdateRange(tupleCoinsEmissionUserCoins.Item2);
            _db.SaveChanges();
            return Task.FromResult(new Response
            {
                Status = Response.Types.Status.Ok,
                Comment = $"Emission {tupleCoinsEmissionUserCoins.Item1.Count} coins."
            });
        }

        /// <summary>
        /// Перемещение монет от одного пользователя к другому
        /// Перемещение сохраняютнся в истории монеты
        /// </summary>
        /// <param name="request">Запрос в ктором указанно от кого и кому, а также количество монет</param>
        /// <param name="context"></param>
        /// <returns>Возвращает ответ: Статус и комментарий</returns>
        public override Task<Response> MoveCoins(MoveCoinsTransaction request, ServerCallContext context)
        {
            Response response = new Response();

            var srcUser = _db.Users.FirstOrDefault(u => u.Name == request.SrcUser);
            var dstUser = _db.Users.FirstOrDefault(u => u.Name == request.DstUser);

            if (srcUser == null || dstUser == null)
            {
                response.Status = Response.Types.Status.Unspecified;
                response.Comment = "Source or destination user not found.";
                return Task.FromResult(response);
            }

            if (srcUser.Amount < request.Amount)
            {
                response.Status = Response.Types.Status.Failed;
                response.Comment = "Source user does not have enough coins.";
                return Task.FromResult(response);
            }

            var coinsToMove = _db.UserCoins.Where(c => c.UserId == srcUser.Id).Take((int)request.Amount);
            var coinsMoveList = new List<UserCoin>();

            foreach (var movedCoin in coinsToMove)
            {
                movedCoin!.UserId = dstUser.Id;
                movedCoin.History += $"-{dstUser.Name}";
                coinsMoveList.Add(movedCoin);
                srcUser.Amount--;
                dstUser.Amount++;
            }

            _db.Users.UpdateRange(srcUser, dstUser);
            _db.UserCoins.UpdateRange(coinsMoveList);
            _db.SaveChanges();
            response.Status = Response.Types.Status.Ok;
            response.Comment = $"Move {request.Amount} coins.";
            return Task.FromResult(response);
        }

        /// <summary>
        /// Монета с самой длинной историей начиная с эмиссии.
        /// Если длина истории одинаковая у нескольких монет, то вернет первую в списке из этих монет
        /// </summary>
        /// <param name="request">получает пустой запрос</param>
        /// <param name="context"></param>
        /// <returns>
        /// Возвращаем монету с самой длиной историей, если есть с такой же длиной, то возвращаем первую в списке монету
        /// </returns>
        public override Task<Coin> LongestHistoryCoin(None request, ServerCallContext context)
        {
            var userCoinsList = _db.UserCoins.ToList();
            var longestHistoryStr = GetLongestHistoryOfCoin(userCoinsList);
            if (longestHistoryStr == null)
            {
                return Task.FromResult(new Coin { History = "No coins found" });
            }
            var userCoin = userCoinsList.FirstOrDefault(c => c.History == longestHistoryStr);
            var coin = _mapper.Map<Coin>(userCoin);
            return Task.FromResult(coin);
        }

        /// <summary>
        /// Метод для получения самой длиной истории среди монет.
        /// </summary>
        /// <param name="userCoinsList">Получаем список монет пользователя</param>
        /// <returns>возвращаем самую длиную инсторию монеты</returns>
        private static string GetLongestHistoryOfCoin(List<UserCoin> userCoinsList)
        {
            var longestHistoryArray = userCoinsList
                            .Select(i => i.History)
                            .MaxBy(i => i.Split("-").Length);   
            return longestHistoryArray;
        }

        /// <summary>
        /// Функция которая возвращает кортеж Список выпущенных монет и 
        /// список пользователей которые получили новые монеты. 
        /// Распеделяет монеты по рейтингу, если монет на распределения не осталось согласно рейтингу,
        /// то досоздаёт по одной монете для зачисления в случае если пользователь еще не получил монет при эмиссии
        /// </summary>
        /// <param name="coinsToDistribute">количество монет для эимсcии</param>
        /// <param name="usersList">Список всех пользователей для получения монет</param>
        /// <returns>Возвращаем кортеж список эмиссии Монет пользователей и список самих пользователей с обновленным количеством монет(Amount)</returns>
        private static (List<UserCoin>, List<User>) GetCoinsEmissionAndUsersCoins(
            long coinsToDistribute,
            List<User> usersList)
        {
            var totalRating = usersList.Sum(u => u.Rating);
            var coinsEmissionList = new List<UserCoin>();
            var userCoinsList = new List<User>();
            foreach (var user in usersList)
            {
                var coinsForUser = Math.Round((double)coinsToDistribute * user.Rating / totalRating, 1);
                if (coinsForUser < 1) coinsForUser = 1;
                for (var i = 0.5; i <= coinsForUser; i++)
                {
                    if (coinsToDistribute <= coinsEmissionList.Count && coinsForUser != 1) break;
                    var coin = new UserCoin { History = $"Issued to {user.Name}", UserId = user.Id };
                    user.Amount++;
                    coinsEmissionList.Add(coin);
                    userCoinsList.Add(user);
                }
            }
            return (coinsEmissionList, userCoinsList);
        }
    }
}