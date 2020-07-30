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

            const string torrentClientPath = @"C:\Program Files\qBittorrent\qbittorrent.exe";
            const string horribleSubs1080pLink = "http://www.horriblesubs.info/rss.php?res=1080";
            string choice;

            WebClient client = new WebClient();
            WatchListManager watchList = new WatchListManager();
            List<Anime> animeList = new List<Anime>();
            
            var downloadedXml = client.DownloadString(horribleSubs1080pLink);
            ParseItemsXml(ref downloadedXml, animeList, watchList);

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
                        Process.Start(torrentClientPath, animeList[result].Link);
                        watchList.SetAnimeAsDownloadedByAnime(animeList[result]);
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
                    watchList.AddToWatchList(values, animeList);
                }
                else if (choice == "dw") // Display watch list
                {
                    while (true)
                    {
                        watchList.DisplayWatchList();

                        Console.WriteLine("Choices: [ q - go back to main window ] [ 0-... - download anime ] [ r - remove from watchlist (eg. 0-...)] [ mr - multiple removal (eg. 1 5 10 30 ...) ]");
                        Console.Write("Pick a choice: ");
                        choice = Console.ReadLine();
                        int index;
                        if(int.TryParse(choice, out index))
                        {
                            if (index < watchList.WatchListCount)
                            {
                                if (string.IsNullOrEmpty(watchList.GetWatchListItemLink(index)))
                                {
                                    DisplayError("ERROR: This anime has no link");
                                    continue;
                                }
                                Process.Start(torrentClientPath, watchList.GetWatchListItemLink(index));
                                watchList.SetAnimeAsDownloadedByWatchListIndex(index);
                            }
                            else
                                DisplayError($"ERROR: THE NUMBER PROVIDED IS TOO LARGE");
                        }
                        else if (choice == "r")
                        {
                            if (watchList.WatchListCount == 0)
                            {
                                DisplayError($"ERROR: THERE ARE NO ENTRIES IN THE WATCHLIST");
                                continue;
                            }
                            Console.Write("Add anime to be removed: ");
                            choice = Console.ReadLine();

                            if (string.IsNullOrEmpty(choice))
                                continue;
                            else if (int.TryParse(choice, out index))
                                watchList.RemoveEntryFromWatchList(index, animeList);
                            else
                                DisplayError($"ERROR: NO VALID PARAMATER GIVEN");
                        }
                        else if (choice == "mr")
                        {
                            if (watchList.WatchListCount == 0)
                            {
                                DisplayError($"ERROR: THERE ARE NO ENTRIES IN THE WATCHLIST");
                                continue;
                            }
                            Console.Write("Add animes to be removed: ");
                            choice = Console.ReadLine();

                            if (string.IsNullOrEmpty(choice))
                                continue;
                            var values = choice.Split(null);
                            watchList.RemoveMultipleEntriesFromWatchList(values, animeList);
                        }
                        else if (choice == "q")
                            break;
                    }
                }
                else
                {
                    // Quit console
                    watchList.WriteWatchListFile();
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
        private static void DisplayAnimeList(List<Anime> animeList)
        {
            Console.WriteLine("CURRENT ANIME LIST:");
            Console.ForegroundColor = ConsoleColor.Green;

            for (int i = 0; i < animeList.Count; i++)
            {
                if (animeList[i].IsInWatchList)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("{0,2}| {1,-90} {2}", i, animeList[i].Title, animeList[i].PubDate.ToShortDateString());
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                else if (animeList[i].IsReleasedToday)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("{0,2}| {1,-90} {2}", i, animeList[i].Title, animeList[i].PubDate.ToShortDateString());
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                else
                    Console.WriteLine("{0,2}| {1,-90} {2}", i, animeList[i].Title, animeList[i].PubDate.ToShortDateString());
            }
            Console.ResetColor();
        }
        private static void ParseItemsXml(ref string downloadedXml, List<Anime> animeList, WatchListManager watchList)
        {
            var tokens = downloadedXml.Split(new string[] { "<item>", "</channel>" }, StringSplitOptions.None);
            for (int i = 0; i < tokens.Length; i++)
            {
                if (!tokens[i].EndsWith("</item>")) // Unneeded token, skipping it
                    continue;

                var tmpTitle = ExtractString(ref tokens[i], "<title>", "</title>");
                var tmpDate = DateTime.Parse(ExtractString(ref tokens[i], "<pubDate>", "</pubDate>"));
                var tmpLink = ExtractString(ref tokens[i], "<link>", "</link>");
                animeList.Add(new Anime()
                {
                    Title = tmpTitle,
                    Link = tmpLink,
                    PubDate = tmpDate,
                    IsInWatchList = watchList.ContainsInWatchList(new Anime { Title = tmpTitle, Link = tmpLink, PubDate = tmpDate }),
                    IsReleasedToday = tmpDate.Date == DateTime.Now.Date
                });
            }
            // Sort the list with possibly updated DateTime values
            watchList.SortWatchList();
        }  
        private static string ExtractString(ref string str, string startingTag, string endingTag)
        {
            StringBuilder builder = new StringBuilder();
            var _str = str.ToCharArray();
            var _startingTag = startingTag.ToCharArray();
            var _endingTag = endingTag.ToCharArray();

            for (int i = 0; i < _str.Length; i++)
            {
                bool foundStart = true;
                for (int j = 0; j < _startingTag.Length; j++)
                {
                    if (_str[i + j] != _startingTag[j])
                    {
                        foundStart = false;
                        break;
                    }
                }

                if (foundStart)
                {
                    for (int l = i + _startingTag.Length; l < str.Length; l++)
                    {
                        bool foundEnd = true;

                        for (int j = 0; j < _endingTag.Length; j++)
                        {
                            if (_str[l + j] != _endingTag[j])
                            {
                                foundEnd = false;
                                break;
                            }
                        }

                        if (!foundEnd)
                            builder.Append(_str[l]);
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
