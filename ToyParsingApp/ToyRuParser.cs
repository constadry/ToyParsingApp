using System.Globalization;
using System.Text;
using AngleSharp;
using AngleSharp.Dom;
using CsvHelper;

namespace ToyParsingApp;

public class ToyRuParser
{
    private IBrowsingContext? _context;
    private readonly List<Toy> _toys;
    public ToyRuParser()
    {
        var config = Configuration.Default.WithDefaultLoader();
        _context = BrowsingContext.New(config);
        _toys = new List<Toy>();
    }

    public async Task ParsePages()
    {
        string url = "https://www.toy.ru/catalog/boy_transport/?filterseccode%5B0%5D=transport&PAGEN_8=";
        const int maxPageNumber = 11;
        for (var i = 1; i <= maxPageNumber; i++)
        {
            await ParsePage(url, i);
        }

        await WriteToCsv();
    }
    
    public async Task ParsePage(string urlPattern, int pageNumber)
    {
        urlPattern += pageNumber.ToString();
        using var doc = await _context.OpenAsync(urlPattern);
        
        Console.WriteLine("link: " + urlPattern);

        var items = doc.QuerySelectorAll("a[class='d-block img-link text-center gtm-click']");

        foreach (var item in items)
        {
            var link = item.GetAttribute("href");
            await ParseToy(link);
        }
    }

    private async Task WriteToCsv()
    {
        await using var writer = new StreamWriter(
            @"C:\Users\behap\Desktop\заметки\toys.csv",
            false,
            Encoding.UTF8);
        await using var csvWriter = new CsvWriter(writer, CultureInfo.CurrentCulture);

        csvWriter.WriteHeader<Toy>();
        await csvWriter.NextRecordAsync();
        await csvWriter.WriteRecordsAsync(_toys);

        await writer.FlushAsync();
    }

    private async Task ParseToy(string link)
    {
        try
        {
            const string pagePrefix = "https://www.toy.ru";
            var itemUrl = pagePrefix + link;
            using var doc = await _context.OpenAsync(itemUrl);
            var toy = new Toy
            {
                RegionName = doc.QuerySelectorAll("a[data-src='#region']").First().Text().Trim(),
                ToyName = doc.QuerySelectorAll("h1[class='detail-name']").First().Text().Trim(),
            
                //TODO: not existing items should be checked
                Price = Price(doc),
                OldPrice = OldPrice(doc),
                Sections = Sections(doc),
                Availability = Availability(doc),
                ImageUrls = ImgUrls(doc),
                ItemUrl = itemUrl
            };
            
            Console.WriteLine(toy.ToyName + " " + toy.Price);

            _toys.Add(toy);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
    }

    private string? Price(IParentNode doc)
    {
        try
        {
            return doc.QuerySelectorAll("span[class='price']").First().Text().Trim();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private string Availability(IParentNode doc)
    {
        try
        {
            return doc.QuerySelectorAll("span[class='ok']").First().Text().Trim();
        }
        catch (InvalidOperationException)
        {
            return doc.QuerySelectorAll("div[class='net-v-nalichii']").First().Text().Trim();
        }
    }

    private string? OldPrice(IParentNode doc)
    {
        try
        {
            return doc.QuerySelectorAll("span[class='old-price']").First().Text().Trim();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private string Sections(IParentNode doc)
    {
        var sections = string.Empty;
        var breadcrumb = doc.QuerySelectorAll("nav[class='breadcrumb']").First();
        foreach (var section in breadcrumb.Children)
        {
            if (section.Text().Trim() == "Вернуться")
                break;
            sections += section.Text().Trim() + " ";
        }

        return sections;
    }

    private List<string> ImgUrls(IParentNode doc)
    {
        var imgCollection = doc.QuerySelectorAll("img[class='img-fluid']");

        return imgCollection.Select(element => element.Attributes.GetNamedItem("src").Value).ToList();
    }
}