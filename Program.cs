using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
namespace HorribleSubsXML_Parser
{
    class Program
    {
        static void Main()
        {
            Console.SetWindowSize(160, 60);

            string choice;
            WebClient client = new WebClient();
            WatchListManager watchlist = new WatchListManager();
            List<Item> animeList = new List<Item>();

            var downloadedXml = client.DownloadString("http://www.horriblesubs.info/rss.php?res=1080");
            ParseItemsXml(ref downloadedXml, animeList, watchlist);
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.ResetColor();
            
                DisplayAnimeList(animeList);
                Console.WriteLine("Choices: [ Any other key - quit ] [ 0-... - anime to be downloaded ] [ w - add to watch list (eg. 0 11 43 ...)] [ dw - display watchlist ]");
                Console.Write("Pick a choice: ");
                choice = Console.ReadLine();
                if (int.TryParse(choice, out int result)) // Download anime
                {
                    if (result < animeList.Count)
                    {
                        Process.Start(@"C:\Program Files\qBittorrent\qbittorrent.exe", animeList[result].Link);
                        watchlist.SetAnimeAsDownloadedByName(animeList[result]);
                    }
                    else
                        DisplayError($"ERROR: THE NUMBER PROVIDED IS TOO LARGE");
                }
                else if (choice == "w") // Add to watch list
                {
                    Console.Write("Add animes to be added: ");
                    choice = Console.ReadLine();
                    if (choice == string.Empty)
                        continue;
                    var values = choice.Split(null);
                    watchlist.AddToWatchList(values, animeList);
                }
                else if (choice == "dw") // Display watch list
                {
                    while (true)
                    {
                        watchlist.DisplayWatchList();

                        Console.WriteLine("Choices: [ q - go back to main window ] [ 0-... - download anime ] [ r - remove from watchlist (eg. 0-...)] [ mr - multiple removal (eg. 1 5 10 30 ...) ]");
                        Console.Write("Pick a choice: ");
                        choice = Console.ReadLine();
                        int index;
                        if(int.TryParse(choice, out index))
                        {
                            if (index < watchlist.WatchListCount)
                            {
                                Process.Start(@"C:\Program Files\qBittorrent\qbittorrent.exe", watchlist.GetWatchListItemLink(index));
                                watchlist.SetAnimeAsDownloadedByWatchListIndex(index);
                            }
                            else
                                DisplayError($"ERROR: THE NUMBER PROVIDED IS TOO LARGE");
                        }
                        else if (choice == "r")
                        {
                            if (watchlist.WatchListCount == 0)
                            {
                                DisplayError($"ERROR: THERE ARE NO ENTRIES IN THE WATCHLIST");
                                continue;
                            }
                            Console.Write("Add anime to be removed: ");
                            choice = Console.ReadLine();

                            if (string.IsNullOrEmpty(choice))
                                continue;
                            else if (int.TryParse(choice, out index))
                                watchlist.RemoveEntryFromWatchList(index, animeList);
                            else
                                DisplayError($"ERROR: NO VALID PARAMATER GIVEN");
                        }
                        else if (choice == "mr")
                        {
                            if (watchlist.WatchListCount == 0)
                            {
                                DisplayError($"ERROR: THERE ARE NO ENTRIES IN THE WATCHLIST");
                                continue;
                            }
                            Console.Write("Add animes to be removed: ");
                            choice = Console.ReadLine();

                            if (string.IsNullOrEmpty(choice))
                                continue;
                            var values = choice.Split(null);
                            watchlist.RemoveMultipleEntriesFromWatchList(values, animeList);
                        }
                        else if (choice == "q")
                            break;
                    }
                }
                else
                {
                    // Quit console
                    watchlist.WriteWatchListFile();
                    break;
                }
            }
        }

        public static void DisplayError(string infoText)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(infoText);
            Console.ResetColor();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }
        private static void DisplayAnimeList(List<Item> itemList)
        {
            Console.WriteLine("CURRENT ANIME LIST:");
            Console.ForegroundColor = ConsoleColor.Green;

            for (int i = 0; i < itemList.Count; i++)
            {
                if (itemList[i].IsInWatchList)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("{0,2}| {1,-90} {2}", i, itemList[i].Title, itemList[i].PubDate.ToShortDateString());
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                else if (itemList[i].IsReleasedToday)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("{0,2}| {1,-90} {2}", i, itemList[i].Title, itemList[i].PubDate.ToShortDateString());
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                else
                    Console.WriteLine("{0,2}| {1,-90} {2}", i, itemList[i].Title, itemList[i].PubDate.ToShortDateString());
            }
            Console.ResetColor();
        }
        private static void ParseItemsXml(ref string downloadedXml, List<Item> itemList, WatchListManager manager)
        {
            var tokens = downloadedXml.Split(new string[] { "<item>", "</channel>" }, StringSplitOptions.None);
            for (int i = 0; i < tokens.Length; i++)
            {
                if (!tokens[i].EndsWith("</item>")) // Unneeded token, skipping it
                    continue;

                var tmpTitle = ExtractString(ref tokens[i], "<title>", "</title>");
                var tmpDate = DateTime.Parse(ExtractString(ref tokens[i], "<pubDate>", "</pubDate>"));
                var tmpLink = ExtractString(ref tokens[i], "<link>", "</link>");
                itemList.Add(new Item()
                {
                    Title = tmpTitle,
                    Link = tmpLink,
                    PubDate = tmpDate,
                    IsInWatchList = manager.ContainsInWatchList(new Item { Title = tmpTitle, Link = tmpLink, PubDate = tmpDate }),
                    IsReleasedToday = tmpDate.Date == DateTime.Now.Date
                });
            }
            // Sort the list with possibly updated DateTime values
            manager.SortWatchList();
        }  
        private static string ExtractString(ref string str, string startingBracket, string endingBracket)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < str.Length; i++)
            {
                bool foundStart = true;
                for (int j = 0; j < startingBracket.Length; j++)
                {
                    if (str[i + j] != startingBracket[j])
                    {
                        foundStart = false;
                        break;
                    }
                }

                if (foundStart)
                {
                    for (int l = i + startingBracket.Length; l < str.Length; l++)
                    {
                        bool foundEnd = true;

                        for (int j = 0; j < endingBracket.Length; j++)
                        {
                            if (str[l + j] != endingBracket[j])
                            {
                                foundEnd = false;
                                break;
                            }
                        }

                        if (!foundEnd)
                            builder.Append(str[l]);
                        else
                        {
                            return builder.ToString();
                        }
                    }
                }
            }
            return string.Empty;
        }
    }
}
