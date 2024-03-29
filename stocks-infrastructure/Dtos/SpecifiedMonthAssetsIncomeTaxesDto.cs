﻿namespace stocks_infrastructure.Dtos
{
    public record SpecifiedMonthAssetsIncomeTaxesDto
    {
        public SpecifiedMonthAssetsIncomeTaxesDto(double taxes, double totalSold, double swingTradeProfit, double dayTradeProfit, string tradedAssets, int assetTypeId, string assetName)
        {
            Taxes = taxes;
            TotalSold = totalSold;
            SwingTradeProfit = swingTradeProfit;
            DayTradeProfit = dayTradeProfit;
            TradedAssets = tradedAssets;
            AssetTypeId = assetTypeId;
            AssetName = assetName;
        }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public SpecifiedMonthAssetsIncomeTaxesDto() { }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.


        public double Taxes { get; init; }
        public double TotalSold { get; init; }
        public double SwingTradeProfit { get; init; }
        public double DayTradeProfit { get; init; }
        public string TradedAssets { get; init; }
        public int AssetTypeId { get; init; }
        public string AssetName { get; init; }
    }
}
