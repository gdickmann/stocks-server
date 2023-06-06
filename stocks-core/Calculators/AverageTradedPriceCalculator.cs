﻿using stocks_common.Models;
using stocks_core.Constants;
using stocks_core.DTOs.B3;

namespace stocks_core.Business
{
    /// <summary>
    /// Classe responsável por calcular o preço médio e o lucro de movimentações de compra e venda.
    /// Também leva em consideração movimentos de bonificação, desdobramento e agrupamento.
    /// </summary>
    public abstract class AverageTradedPriceCalculator
    {
        private static readonly Dictionary<string, TickerAverageTradedPrice> assetAverageTradedPrice = new();

        /// <summary>
        /// Calcula o preço médio e lucro dos ativos que foram operados.
        /// </summary>
        public static (Dictionary<string, OperationDetails> dayTrade, Dictionary<string, OperationDetails> swingTrade) CalculateMovements
            (IEnumerable<Movement.EquitMovement> movements)
        {
            Dictionary<string, OperationDetails> dayTrade = new();
            Dictionary<string, OperationDetails> swingTrade = new();

            foreach (var movement in movements)
            {
                switch (movement.MovementType)
                {
                    case B3ResponseConstants.Buy:
                        CalculateBuyOperation(movement);
                        break;
                    case B3ResponseConstants.Sell:
                        CalculateSellOperation(dayTrade, swingTrade, movement, movements);
                        break;
                    case B3ResponseConstants.Split:
                        CalculateSplitOperation(movement);
                        break; 
                    case B3ResponseConstants.ReverseSplit:
                        CalculateReverseSplitOperation(movement);
                        break;
                    case B3ResponseConstants.BonusShare:
                        CalculateBonusSharesOperation(movement);
                        break;
                }
            }

            return (dayTrade, swingTrade);
        }

        private static void AddTickerIntoDictionary(
            Dictionary<string, OperationDetails> dayTradeResponse,
            Dictionary<string, OperationDetails> swingTradeResponse,
            Movement.EquitMovement movement
        )
        {
            if (dayTradeResponse.ContainsKey(movement.TickerSymbol) || swingTradeResponse.ContainsKey(movement.TickerSymbol)) return;

            if (movement.DayTraded)
                dayTradeResponse.Add(movement.TickerSymbol, new OperationDetails(movement.CorporationName, movement.DayTraded));

            if (!movement.DayTraded)
                swingTradeResponse.Add(movement.TickerSymbol, new OperationDetails(movement.CorporationName, movement.DayTraded));
        }

        public static decimal CalculateIncomeTaxes(double swingTradeProfit, double dayTradeProfit, int aliquot)
        {
            decimal swingTradeTaxes = 0;
            decimal dayTradeTaxes = 0;

            if (swingTradeProfit > 0)
                swingTradeTaxes = (aliquot / 100m) * (decimal)swingTradeProfit;

            if (dayTradeProfit > 0)
                dayTradeTaxes = (AliquotConstants.IncomeTaxesForDayTrade / 100m) * (decimal)dayTradeProfit;

            decimal totalTaxes = swingTradeTaxes + dayTradeTaxes;

            return totalTaxes;
        }

        public static IEnumerable<(string, string)> ToDto(IEnumerable<Movement.EquitMovement> movements)
        {
            var tradedTickers = movements.Select(x => (x.TickerSymbol, x.CorporationName)).Distinct();
            return tradedTickers;
        }

        private static void CalculateBuyOperation(Movement.EquitMovement movement)
        {
            bool tickerHasAverageTradedPrice = assetAverageTradedPrice.ContainsKey(movement.TickerSymbol);

            if (tickerHasAverageTradedPrice)
            {
                var ticker = assetAverageTradedPrice[movement.TickerSymbol];

                double totalBought = ticker.TotalBought + movement.OperationValue;
                double quantity = ticker.TradedQuantity + movement.EquitiesQuantity;

                ticker.UpdateValues(totalBought, (int)quantity);
            }
            else
            {
                assetAverageTradedPrice.Add(movement.TickerSymbol, new TickerAverageTradedPrice(
                    movement.CorporationName,
                    averageTradedPrice: movement.OperationValue / movement.EquitiesQuantity,
                    totalBought: movement.OperationValue,
                    tradedQuantity: (int)movement.EquitiesQuantity
                ));
            }            
        }

        protected static Dictionary<string, TickerAverageTradedPrice> GetAssetDetails()
        {
            return assetAverageTradedPrice;
        }

        private static void CalculateSellOperation(
            Dictionary<string, OperationDetails> dayTradeResponse,
            Dictionary<string, OperationDetails> swingTradeResponse,
            Movement.EquitMovement movement,
            IEnumerable<Movement.EquitMovement> movements
        )
        {
            AddTickerIntoDictionary(dayTradeResponse, swingTradeResponse, movement);

            OperationDetails? asset = null;

            if (movement.DayTraded)
                asset = dayTradeResponse[movement.TickerSymbol];
            else
                asset = swingTradeResponse[movement.TickerSymbol];

            if (AssetBoughtAfterB3MinimumDate(movement))
            {
                double averageTradedPrice = assetAverageTradedPrice[movement.TickerSymbol].AverageTradedPrice;
                double profitPerShare = movement.UnitPrice - averageTradedPrice;
                double totalProfit = profitPerShare * movement.EquitiesQuantity;

                if (totalProfit > 0)
                {
                    // TO-DO (MVP?): calcular IRRFs (e.g dedo-duro).
                }

                asset.UpdateTotalProfit(totalProfit);

                // TO-DO (MVP?): calcular emolumentos.
            }
            else
            {
                // Se um ticker está sendo vendido e não consta no Dictionary de compras (ou seja, foi comprado antes ou em 01/11/2019 e a API não reconhece),
                // o usuário manualmente precisará inserir o preço médio do ticker.

                asset.UpdateTickerBoughtBeforeB3DateRange(boughtBeforeB3DateRange: true);
                movements = movements.Where(x => x.TickerSymbol != movement.TickerSymbol).ToList();
            }
        }

        private static bool AssetBoughtAfterB3MinimumDate(Movement.EquitMovement movement)
        {
            return assetAverageTradedPrice.ContainsKey(movement.TickerSymbol);
        }

        private static void CalculateSplitOperation(Movement.EquitMovement movement)
        {
            // É necessário calcular os desdobramentos de um ativo pois a sua relação de preço/quantidade alteram. Caso elas se alterem,
            // o cálculo do preço médio pode ser afetado.

            // TO-DO: entrar em contato com a B3 e tirar a dúvida de como funciona o response de desdobramento.
            throw new NotImplementedException();
        }

        private static void CalculateReverseSplitOperation(Movement.EquitMovement movement)
        {
            // É necessário calcular os agrupamentos de um ativo pois a sua relação de preço/quantidade alteram. Caso elas se alterem,
            // o cálculo do preço médio pode ser afetado.

            // TO-DO: entrar em contato com a B3 e tirar a dúvida de como funciona o response de agrupamento.
            throw new NotImplementedException();
        }

        private static void CalculateBonusSharesOperation(Movement.EquitMovement movement)
        {
            // É necessário calcular as bonificações de um ativo pois a sua relação de preço/quantidade alteram. Caso elas se alterem,
            // o cálculo do preço médio pode ser afetado.

            // TO-DO: entrar em contato com a B3 e tirar a dúvida de como funciona o response de bonificação.
            throw new NotImplementedException();
        }
    }    
}
