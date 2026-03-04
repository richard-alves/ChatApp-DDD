using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace ChatApp.Bot.Services;

public interface IStockService
{
    Task<StockQuote?> GetStockQuoteAsync(string stockCode, CancellationToken cancellationToken = default);
}

public record StockQuote(string Symbol, decimal? Price, string DisplayMessage);

public class StockRecord
{
    [Name("Symbol")] public string Symbol { get; set; } = string.Empty;
    [Name("Date")] public string Date { get; set; } = string.Empty;
    [Name("Time")] public string Time { get; set; } = string.Empty;
    [Name("Open")] public string Open { get; set; } = string.Empty;
    [Name("High")] public string High { get; set; } = string.Empty;
    [Name("Low")] public string Low { get; set; } = string.Empty;
    [Name("Close")] public string Close { get; set; } = string.Empty;
    [Name("Volume")] public string Volume { get; set; } = string.Empty;
}

public class StockService(HttpClient httpClient, ILogger<StockService> logger) : IStockService
{
    private const string BaseUrl = "https://stooq.com/q/l/?s={0}&f=sd2t2ohlcv&h&e=csv";

    public async Task<StockQuote?> GetStockQuoteAsync(string stockCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = string.Format(BaseUrl, Uri.EscapeDataString(stockCode.ToLower()));
            logger.LogInformation("Fetching stock quote for {StockCode} from {Url}", stockCode, url);

            var response = await httpClient.GetStringAsync(url, cancellationToken);

            if (string.IsNullOrWhiteSpace(response))
            {
                logger.LogWarning("Empty response for stock code: {StockCode}", stockCode);
                return new StockQuote(stockCode, null, $"{stockCode.ToUpper()} quote is not available.");
            }

            using var reader = new StringReader(response);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null
            };
            using var csv = new CsvReader(reader, config);

            var records = csv.GetRecords<StockRecord>().ToList();
            if (!records.Any())
            {
                logger.LogWarning("No records found for stock code: {StockCode}", stockCode);
                return new StockQuote(stockCode, null, $"{stockCode.ToUpper()} quote is not available.");
            }

            var record = records.First();

            if (decimal.TryParse(record.Close, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) && price > 0)
            {
                var message = $"{record.Symbol.ToUpper()} quote is ${price:F2} per share.";
                return new StockQuote(record.Symbol, price, message);
            }

            return new StockQuote(stockCode, null, $"{stockCode.ToUpper()} quote is not available at this time.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error fetching stock {StockCode}", stockCode);
            return new StockQuote(stockCode, null, $"Could not fetch quote for {stockCode.ToUpper()}. Please try again later.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error fetching stock {StockCode}", stockCode);
            return new StockQuote(stockCode, null, $"An error occurred while fetching quote for {stockCode.ToUpper()}.");
        }
    }
}
