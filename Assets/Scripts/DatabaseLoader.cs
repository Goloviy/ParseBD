using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using MySqlConnector;
using XCharts.Runtime;


public class DatabaseLoader : MonoBehaviour
{
    [SerializeField] private LineChart lineChart;
    [SerializeField] private ViewDropdown viewDropdown;
    private string server = "localhost";
    private string userId = "root";
    private string password = "Qwerty12345";
    private string database = "ahdb";

    private void Start()
    {
        lineChart.ClearData();
        lineChart.SetSize(800, 500);
        viewDropdown.OnChangeValueDropdown += CreateTable;
        viewDropdown.InitDefaultDropdown();
    }

    private async void CreateTable(Item item)
    {
        var result = await FindAuctionsByItemId(item.id);
        NormalizedPrice(result);
        var result2 = GroupItems(result);
        var minPrice = GetDailyMinPrices(result2);
        var title = lineChart.EnsureChartComponent<Title>();
        title.text = item.itemName;
        lineChart.ClearData();
        foreach (var value in minPrice)
        {
            lineChart.AddXAxisData(value.Key.ToString("dd.MM"));
            lineChart.AddData(0, value.Value.Buyout);
        }
    }


    private AuctionItem FindMinPriceAuction(List<AuctionItem> auctions)
    {
        if (auctions == null || auctions.Count == 0)
            throw new ArgumentException("Список аукционов пуст!");
        return auctions.OrderBy(a => a.Buyout).First();
    }

    private Dictionary<DateTime, AuctionItem> GetDailyMinPrices(
        Dictionary<DateTime, List<AuctionItem>> auctionsByDay)
    {
        var result = new Dictionary<DateTime, AuctionItem>();

        foreach (var dayEntry in auctionsByDay)
        {
            var minAuction = FindMinPriceAuction(dayEntry.Value);
            result.Add(dayEntry.Key, minAuction);
        }

        return result;
    }

    private Dictionary<DateTime, AuctionItem> GetDailyRepresentativeAuctions(
        Dictionary<DateTime, List<AuctionItem>> auctionsByDay)
    {
        var result = new Dictionary<DateTime, AuctionItem>();

        foreach (var dayEntry in auctionsByDay)
        {
            // Находим репрезентативный аукцион для дня
            AuctionItem representativeAuction = FindMostCommonAuction(dayEntry.Value);
            result.Add(dayEntry.Key, representativeAuction);
        }

        return result;
    }

    private void NormalizedPrice(List<AuctionItem> auctionItems)
    {
        for (var index = 0; index < auctionItems.Count; index++)
        {
            auctionItems[index].Timestamp = auctionItems[index].Timestamp.Date;
            var price = auctionItems[index].Buyout / auctionItems[index].ItemCount;
            auctionItems[index].Buyout = price;
            auctionItems[index].ItemCount = 1;
        }
    }

    private AuctionItem FindMostCommonAuction(List<AuctionItem> auctions, int tolerance = 2000)
    {
        var priceGroups = auctions
            .GroupBy(a => Mathf.Round(a.Buyout / tolerance) * tolerance)
            .OrderByDescending(g => g.Count());

        var mostCommonGroup = priceGroups.First();

        var avgPrice = mostCommonGroup.Average(a => a.Buyout);
        return mostCommonGroup.OrderBy(a => Math.Abs(a.Buyout - avgPrice)).First();
    }

    private Dictionary<DateTime, List<AuctionItem>> GroupItems(List<AuctionItem> auctionItems)
    {
        var auctionsByDay = new Dictionary<DateTime, List<AuctionItem>>();
        foreach (var item in auctionItems)
        {
            if (!auctionsByDay.ContainsKey(item.Timestamp))
            {
                auctionsByDay[item.Timestamp] = new List<AuctionItem>();
            }

            auctionsByDay[item.Timestamp].Add(item);
        }

        return auctionsByDay;
    }

    private async Task<List<AuctionItem>> FindAuctionsByItemId(string targetItemId)
    {
        var results = new List<AuctionItem>();

        var builder = new MySqlConnectionStringBuilder
        {
            Server = server,
            UserID = userId,
            Password = password,
            Database = database,
            Port = 3306,
            SslMode = MySqlSslMode.Disabled
        };

        try
        {
            using var connection = new MySqlConnection(builder.ConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT 
                    scanId,  
                    itemId, 
                    ts, 
                    seller, 
                    timeLeft, 
                    itemCount, 
                    minBid, 
                    buyout, 
                    curBid 
                FROM auctions 
                WHERE itemId = @TargetItemId;";

            command.Parameters.AddWithValue("@TargetItemId", targetItemId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var item = new AuctionItem
                {
                    ScanId = reader.GetInt32("scanId"),
                    ItemId = reader.GetString("itemId"),
                    Timestamp = reader.GetDateTime("ts"),
                    Seller = reader.GetString("seller"),
                    TimeLeft = reader.GetInt32("timeLeft"),
                    ItemCount = reader.GetInt32("itemCount"),
                    MinBid = reader.GetInt32("minBid"),
                    Buyout = reader.GetInt32("buyout"),
                    CurrentBid = reader.GetInt32("curBid")
                };
                results.Add(item);
            }
        }
        catch (MySqlException ex)
        {
            Debug.LogError($"MySQL Error: {ex.Message}");
        }

        return results;
    }
}

public class AuctionItem
{
    public int ScanId { get; set; }
    public string ItemId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Seller { get; set; }
    public int TimeLeft { get; set; }
    public int ItemCount { get; set; }
    public int MinBid { get; set; }
    public int Buyout { get; set; }
    public int CurrentBid { get; set; }
}