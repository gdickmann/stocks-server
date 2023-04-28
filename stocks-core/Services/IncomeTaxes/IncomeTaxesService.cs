using Microsoft.Extensions.Logging;
using stocks.Clients.B3;
using stocks.Models;
using stocks.Repositories;
using stocks.Requests;
using stocks_core.Business;
using stocks_core.Calculators.Assets;
using stocks_core.Constants;
using stocks_core.DTOs.B3;
using stocks_core.Response;
using stocks_core.Services.AverageTradedPrice;
using stocks_infrastructure.Models;
using stocks_infrastructure.Repositories.AverageTradedPrice;

namespace stocks.Services.IncomeTaxes;

public class IncomeTaxesService : IIncomeTaxesService
{

    private readonly IAverageTradedPriceService _averageTradedPriceService;
    private IIncomeTaxesCalculator _incomeTaxCalculator;

    private readonly IGenericRepository<Account> _genericRepositoryAccount;
    private readonly IGenericRepository<stocks_infrastructure.Models.IncomeTaxes> _genericRepositoryIncomeTaxes;
    private readonly IAverageTradedPriceRepostory _averageTradedPriceRepository;

    private readonly IB3Client _client;

    private readonly ILogger<IncomeTaxesService> _logger;

    public IncomeTaxesService(IAverageTradedPriceService averageTradedPriceService,
        IIncomeTaxesCalculator calculator,
        IGenericRepository<Account> genericRepositoryAccount,
        IGenericRepository<stocks_infrastructure.Models.IncomeTaxes> genericRepositoryIncomeTaxes,
        IAverageTradedPriceRepostory averageTradedPriceRepository,
        IB3Client b3Client,
        ILogger<IncomeTaxesService> logger
        )
    {
        _averageTradedPriceService = averageTradedPriceService;
        _genericRepositoryAccount = genericRepositoryAccount;
        _averageTradedPriceRepository = averageTradedPriceRepository;
        _client = b3Client;
        _incomeTaxCalculator = calculator;
        _logger = logger;
    }

    #region Calcula o imposto de renda a ser pago no mês atual
    public async Task<AssetIncomeTaxes?> CalculateCurrentMonthAssetsIncomeTaxes(Guid accountId)
    {
        string mockedCpf = "97188167044";

        // A consulta da B3 apenas funciona em D-1, ou seja, as consultas sempre são feitas com base
        // no dia anterior.
        var referenceStartDate = DateTime.Now.ToString("yyyy-MM-01");
        var referenceEndDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

        try
        {
            // Movement.Root? httpClientResponse = await _client.GetAccountMovement(mockedCpf, "2022-01-01", "2022-02-20")!;
            Movement.Root? httpClientResponse = new();
            httpClientResponse.Data = new();
            httpClientResponse.Data.EquitiesPeriods = new();
            httpClientResponse.Data.EquitiesPeriods.EquitiesMovements = new();

            httpClientResponse.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "PETR4",
                MovementType = "Compra",
                OperationValue = 10.43,
                EquitiesQuantity = 2,
            });

            httpClientResponse.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "PETR4",
                MovementType = "Venda",
                OperationValue = 13.12,
                EquitiesQuantity = 5,
            });

            httpClientResponse.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "PETR4",
                MovementType = "Venda",
                OperationValue = 9.32,
                EquitiesQuantity = 3,
            });

            AssetIncomeTaxes? response = null;

            if (InvestorSoldAnyAsset(httpClientResponse))
            {
                await AddAllRequiredIncomeTaxesToObject(httpClientResponse, response, accountId);
            }

            return response;

        } catch (Exception _)
        {
            throw;
        }
    }

    private async Task AddAllRequiredIncomeTaxesToObject(Movement.Root httpClientResponse, AssetIncomeTaxes? response, Guid accountId)
    {
        var movements = httpClientResponse.Data.EquitiesPeriods.EquitiesMovements;

        var stocks = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.Stocks));
        var etfs = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.ETFs));
        var fiis = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.FIIs));
        var bdrs = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.BDRs));
        var gold = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.Gold));
        var fundInvestments = movements.Where(x => x.AssetType.Equals(AssetMovementTypes.FundInvestments));

        _incomeTaxCalculator = new StocksIncomeTaxes();
        _incomeTaxCalculator.CalculateCurrentMonthIncomeTaxes(response, stocks, accountId);

        _incomeTaxCalculator = new ETFsIncomeTaxes();
        _incomeTaxCalculator.CalculateCurrentMonthIncomeTaxes(response, etfs, accountId);

        // _incomeTaxCalculator = new FIIsIncomeTaxes();
        _incomeTaxCalculator.CalculateCurrentMonthIncomeTaxes(response, fiis, accountId);

        // _incomeTaxCalculator = new BDRsIncomeTaxes();
        _incomeTaxCalculator.CalculateCurrentMonthIncomeTaxes(response, bdrs, accountId);

        // _incomeTaxCalculator = new GoldIncomeTaxes();
        _incomeTaxCalculator.CalculateCurrentMonthIncomeTaxes(response, gold, accountId);

        // _incomeTaxCalculator = new FundInvestmentsIncomeTaxes();
        _incomeTaxCalculator.CalculateCurrentMonthIncomeTaxes(response, fundInvestments, accountId);
    }

    private static bool InvestorSoldAnyAsset(Movement.Root httpClientResponse)
    {
        var allMovements = httpClientResponse.Data.EquitiesPeriods.EquitiesMovements;

        var sellOperationMovements = httpClientResponse.Data.EquitiesPeriods.EquitiesMovements.Where(
            asset => asset.MovementType.Equals(B3ServicesConstants.Sell)).FirstOrDefault();

        bool investorSoldAnyAsset = allMovements.Contains(sellOperationMovements!);

        // Mockado por enquanto porque a B3 não possui movimentos de venda em ambiente
        // de certificação.
        return true;
    }
    #endregion

    #region Calcula o imposto de renda a ser pago de 01/11/2019 até D-1.
    public async Task CalculateIncomeTaxesForEveryMonth(Guid accountId, List<CalculateIncomeTaxesForEveryMonthRequest> request)
    {
        try
        {
            // TODO: calculate runtime
            if (AccountAlreadyHasAverageTradedPrice(accountId)) return;

            string minimumAllowedStartDateByB3 = "2019-11-01";
            string referenceEndDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            // Movement.Root? response = await _client.GetAccountMovement("97188167044", minimumAllowedStartDateByB3, referenceEndDate)!;
            Movement.Root? response = new();
            response.Data = new();
            response.Data.EquitiesPeriods = new();
            response.Data.EquitiesPeriods.EquitiesMovements = new();

            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "PETR4",
                CorporationName = "Petróleo Brasileiro S.A.",
                MovementType = "Compra",
                OperationValue = 10,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2023, 01, 16),
                UnitPrice = 10
            });
            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "PETR4",
                CorporationName = "Petróleo Brasileiro S.A.",
                MovementType = "Compra",
                OperationValue = 11,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2023, 01, 17),
                UnitPrice = 11
            });
            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "PETR4",
                CorporationName = "Petróleo Brasileiro S.A.",
                MovementType = "Compra",
                OperationValue = 12,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2023, 02, 17),
                UnitPrice = 12
            });
            response.Data.EquitiesPeriods.EquitiesMovements.Add(new Movement.EquitMovement
            {
                AssetType = "Ações",
                TickerSymbol = "PETR4",
                CorporationName = "Petróleo Brasileiro S.A.",
                MovementType = "Compra",
                OperationValue = 10,
                EquitiesQuantity = 1,
                ReferenceDate = new DateTime(2023, 02, 17),
                UnitPrice = 10
            });

            BigBang bigBang = new(_genericRepositoryIncomeTaxes, _genericRepositoryAccount);
            await bigBang.Calculate(response, _incomeTaxCalculator, accountId);
        }
        catch (Exception e)
        {
            _logger.LogError("Uma exceção ocorreu ao executar o método {1}, classe {2}. Exceção: {3}",
                nameof(CalculateIncomeTaxesForEveryMonth), nameof(AverageTradedPriceService), e.Message);
            throw;
        }

        bool AccountAlreadyHasAverageTradedPrice(Guid accountId)
        {
            return _averageTradedPriceRepository.AccountAlreadyHasAverageTradedPrice(accountId);
        }
    }
    #endregion
}
