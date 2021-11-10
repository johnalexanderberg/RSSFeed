using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Security.Permissions;
using System.Xml.Linq;

namespace RSSFeed
{
    public class Feed
    {
        public string Url;
        public string Title;
        public XDocument Document;
    }
    public class Item
    {
        public Feed Feed;
        public string Title;
        public DateTime DateTime;

    }


    internal class Program
    {
        private static HttpClient http = new HttpClient();

        public static string[] urls = new[]
        {
            "https://www.theguardian.com/uk/film/rss",
            "https://www.cinemablend.com/rss/topic/news/movies",
            "https://www.comingsoon.net/feed",
            "https://screencrush.com/feed/"
        };

        private static async Task<XDocument> LoadDocumentAsync(string url)
        {
            // This is just to simulate a slow/large data transfer and make testing easier.
            // Remove it if you want to.
            await Task.Delay(1000);
            var response = await http.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync();
            var feed = XDocument.Load(stream);
            return feed;
        }



        public static async Task Main(string[] args)
        {
            bool running = true;

            while (running)
            {
                var selection = Utils.ShowMenu("What do you want to do?", new[]
                {
                    "Add RSS Feed",
                    "Show RSS Feed",
                    "Show All RSS Feeds",
                    "Exit"
                });

                if (selection == 0)
                {
                    AddFeed();
                }
                else if (selection == 1)
                {
                    await ShowFeed();
                }
                else if (selection == 2)
                {
                    await ShowAllFeeds();
                }
                else
                {
                    running = false;
                }
                
                
            }
            

        }

        private static async Task ShowAllFeeds()
        {
            var allItems = new List<Item>();

            var feedTasks = urls.Select(GetFeed).ToList();

            while (feedTasks.Count > 0)
            {
                var task = await Task.WhenAny(feedTasks);
                allItems.AddRange(GetItems(await task));
                feedTasks.Remove(task);
            }

            allItems = allItems.OrderByDescending(x => x.DateTime).ToList();

            ShowItems(allItems);
        }

        private static void AddFeed()
        {
            var urlList = urls.ToList();
            urlList.Add(Utils.ReadString("Enter RSS Feed Url:"));
            urls = urlList.ToArray();
        }

        private static async Task ShowFeed()
        {
            int selection = Utils.ShowMenu("Select feed:", urls);

            var feed = await GetFeed(urls[selection]);

            var items = GetItems(feed);

            ShowItems(items);
        }

        private static void ShowItems(IEnumerable<Item> items)
        {
            foreach (var item in items)
            {
                Utils.WriteHeading(item.Title);
                Console.WriteLine(item.DateTime);
                Console.WriteLine(item.Feed.Title);
                Console.WriteLine();
            }
        }

        private static IEnumerable<Item> GetItems(Feed feed)
        {
            var itemsXElements = feed.Document.Descendants("item");
            var items = new List<Item>();

            foreach (var xElement in itemsXElements)
            {
                var item = new Item
                {
                    Feed = feed,
                    Title = xElement.Descendants("title").First().Value,
                    DateTime = DateTime.ParseExact(xElement.Descendants("pubDate").First().Value.Substring(0, 25),
                        "ddd, dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture)
                };
                items.Add(item);
            }

            return items;
        }

        private static async Task<Feed> GetFeed(string url)
        {
            
            var documentTask = LoadDocumentAsync(url);
            var document = await documentTask;
            
            var feed = new Feed();

            feed.Title = document.Descendants("title").First().Value;
            feed.Url = url;
            feed.Document = document;
            
            return feed;
        }
    }
}