using Microsoft.Extensions.Logging;
using stocks.Clients.B3;
using stocks.Models;
using stocks.Repositories;
using stocks_core.Constants;
using stocks_core.DTOs.AverageTradedPrice;
using stocks_core.DTOs.B3;
using stocks_infrastructure.Repositories.AverageTradedPrice;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("stocks-unit-tests")]
namespace stocks_core.Services.AverageTradedPrice
{
    public class AverageTradedPriceService : IAverageTradedPriceService
    {
        private readonly IGenericRepository<Account> _genericAccountRepository;
        private readonly IAverageTradedPriceRepository _averageTradedPriceRepository;

        private readonly IB3Client _client;

        private readonly ILogger<AverageTradedPriceService> _logger;

        public AverageTradedPriceService(IGenericRepository<Account> genericAccountRepository,
            IAverageTradedPriceRepository averageTradedPriceRepository,
            IB3Client client,
            ILogger<AverageTradedPriceService> logger)
        {
            _genericAccountRepository = genericAccountRepository;
            _averageTradedPriceRepository = averageTradedPriceRepository;
            _client = client;

            _logger = logger;
        }

        public async Task Insert(Guid accountId)
        {
            try
            {
                if (AccountAlreadyHasAverageTradedPrice(accountId)) return;

                string minimumAllowedStartDateByB3 = "2019-11-01";
                string referenceEndDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

                Movement.Root? response = await _client.GetAccountMovement("97188167044", minimumAllowedStartDateByB3, referenceEndDate)!;

                var buyOperations = response.Data.EquitiesPeriods.EquitiesMovements
                    .Where(x => x.MovementType == B3ServicesConstants.Buy);

                var sellOperations = response.Data.EquitiesPeriods.EquitiesMovements
                    .Where(x => x.MovementType == B3ServicesConstants.Sell);

                // Ao calcular as compras e vendas, é necessário identificar caso tenha havido 
                // desdobramento das ações. Caso não seja identificado, uma ação pode passar a ter um número maior de
                // papéis e o cálculo do preço médio estará incorreto.
                var splitsOperations = response.Data.EquitiesPeriods.EquitiesMovements
                    .Where(x => x.MovementType == B3ServicesConstants.Split);

                Dictionary<string, AverageTradedPriceCalculator> averageTradedPrices = CalculateAverageTradedPrice(buyOperations, sellOperations);
            }
            catch (Exception e)
            {
                _logger.LogError("Uma exceção ocorreu ao executar o método {1}, classe {2}. Exceção: {3}",
                    nameof(Insert), nameof(AverageTradedPriceService), e.Message);
                throw;
            }
        }

        internal static Dictionary<string, AverageTradedPriceCalculator> CalculateAverageTradedPrice(
            IEnumerable<Movement.EquitMovement> buyOperations,
            IEnumerable<Movement.EquitMovement> sellOperations
        )
        {
            Dictionary<string, AverageTradedPriceCalculator> total = new();

            // TODO: calcular desdobramento

            foreach (var movement in buyOperations)
            {
                if (total.ContainsKey(movement.TickerSymbol))
                {
                    var asset = total[movement.TickerSymbol];

                    asset.CurrentPrice += movement.OperationValue;
                    asset.CurrentQuantity += movement.EquitiesQuantity;
                } else
                {
                    total.Add(movement.TickerSymbol, new AverageTradedPriceCalculator(movement.OperationValue, movement.EquitiesQuantity));
                }
            }

            foreach (var movement in sellOperations)
            {
                if (total.ContainsKey(movement.TickerSymbol))
                {
                    var asset = total[movement.TickerSymbol];

                    asset.CurrentPrice -= movement.OperationValue;
                    asset.CurrentQuantity -= movement.EquitiesQuantity;

                    if (asset.CurrentQuantity == 0) total.Remove(movement.TickerSymbol);
                }
            }

            foreach(var movement in total)
            {
                var totalSpent = movement.Value.CurrentPrice;
                var assetQuantity = movement.Value.CurrentQuantity;

                movement.Value.AverageTradedPrice = Math.Round(totalSpent / assetQuantity, 2, MidpointRounding.AwayFromZero);
            }

            return total;
        }

        private bool AccountAlreadyHasAverageTradedPrice(Guid accountId)
        {
            return _averageTradedPriceRepository.AccountAlreadyHasAverageTradedPrice(accountId);
        }
    }
}
