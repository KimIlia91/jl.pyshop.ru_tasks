using AutoMapper;
using Billing.Data;
using Billing.Models;
using Grpc.Core;

namespace Billing.Services
{
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

        public override Task<Response> CoinsEmission(EmissionAmount request, ServerCallContext context)
        {
            var usersList = _db.Users.ToList();
            var coinsToDistribute = request.Amount;
            var tulup = GetCoinsAndUsersToUpdate(coinsToDistribute, usersList);
            _db.UserCoins.AddRange(tulup.Item1);
            _db.Users.UpdateRange(tulup.Item2);
            _db.SaveChanges();
            return Task.FromResult(new Response
            {
                Status = Response.Types.Status.Ok,
                Comment = $"Emission {tulup.Item1.Count} coins."
            });
        }

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
                movedCoin.History += $" + {dstUser.Name}";
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

        public override Task<Coin> LongestHistoryCoin(None request, ServerCallContext context)
        {
            var userCoin = _db.UserCoins.OrderByDescending(c => c.History.Length).FirstOrDefault();
            if (userCoin == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "No coins found"));
            }
            var coin = _mapper.Map<Coin>(userCoin);
            return Task.FromResult(coin);
        }

        private static (List<UserCoin>, List<User>) GetCoinsAndUsersToUpdate(
            long coinsToDistribute,
            List<User> usersList)
        {
            var totalRating = usersList.Sum(u => u.Rating);
            var coinsEmissionList = new List<UserCoin>();
            var usersToUpdateList = new List<User>();
            foreach (var user in usersList)
            {
                var coinsForUser = Math.Round((double)coinsToDistribute * user.Rating / totalRating, 1);
                if (coinsForUser < 1) coinsForUser = 1;
                for (var i = 0.5; i <= coinsForUser; i++)
                {
                    if (coinsToDistribute <= coinsEmissionList.Count) break;
                    var coin = new UserCoin { History = $"Issued to {user.Name}", UserId = user.Id };
                    user.Amount++;
                    coinsEmissionList.Add(coin);
                    usersToUpdateList.Add(user);
                }
            }
            return (coinsEmissionList, usersToUpdateList);
        }
    }
}
