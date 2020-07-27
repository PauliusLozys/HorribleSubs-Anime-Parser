using System;
using System.Collections.Generic;
using System.IO;

namespace HorribleSubsXML_Parser
{
    class WatchListManager
    {
        private List<WatchListItem> WatchList { get; set; }
        private readonly string fileName = "animeListing.txt";
        private readonly string seperator = new string('-', Console.WindowWidth - 1);

        public int WatchListCount { get { return WatchList.Count; } }

        public WatchListManager()
        {
            WatchList = new List<WatchListItem>();
            // Read a watchlist file
            ReadWatchListFile();
        }

        public void AddToWatchList(string[] animes, List<Item> animeList)
        {
            foreach (var item in animes)
            {
                int index = int.Parse(item);
                string title = animeList[index].Title;

                for (int i = title.Length - 1; i != 0; i--)
                {
                    if (title[i] == '-')
                    {
                        WatchList.Add(new WatchListItem()
                        {
                            Title = title.Remove(i),
                            LatestEpisode = 0,
                            IsDownloaded = false,
                            ReleaseDay = animeList[index].PubDate
                        }); ;
                        break;
                    }
                }
            }
            // Rechecks the whole list for containing animes
            foreach (var anime in animeList)
            {
                anime.IsInWatchList = ContainsInWatchList(anime);
            }

            WatchList.Sort();
        }
        public void DisplayWatchList()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            if (WatchList.Count == 0)
            {
                Console.WriteLine("There is no anime in here :(");
                Console.ResetColor();

                return;
            }
            Console.WriteLine(seperator);

            for (int i = 0; i < WatchList.Count; i++)
            {
                if(!WatchList[i].IsDownloaded)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("{0,2}| {1,-80} Latest Episode: {2,-3}| Next episode release date: {3} {4}:{5}",
                        i,
                        WatchList[i].Title,
                        WatchList[i].LatestEpisode,
                        WatchList[i].ReleaseDay.AddDays(7).ToShortDateString(),
                        WatchList[i].ReleaseDay.Hour,
                        WatchList[i].ReleaseDay.Minute < 30 ? "00" : "30"
                        );
                    Console.ForegroundColor = ConsoleColor.Cyan;
                }
                else
                    Console.WriteLine("{0,2}| {1,-80} Latest Episode: {2,-3}| Next episode release date: {3} {4}:{5}",
                        i,
                        WatchList[i].Title,
                        WatchList[i].LatestEpisode,
                        WatchList[i].ReleaseDay.AddDays(7).ToShortDateString(),
                        WatchList[i].ReleaseDay.Hour,
                        WatchList[i].ReleaseDay.Minute < 30 ? "00" : "30"
                        );
                Console.WriteLine(seperator);
            }
            Console.ResetColor();
        }
        public bool ContainsInWatchList(Item anime)
        {
            foreach (var item in WatchList)
            {
                if (anime.Title.Contains(item.Title))
                {
                    int episodeNumber = GetAnimesEpisodeNumber(anime.Title);
                    if(episodeNumber > item.LatestEpisode)
                    {
                        // Newer version of the episode was found
                        item.LatestEpisodeLink = anime.Link;
                        item.LatestEpisode = episodeNumber;
                        item.IsDownloaded = false;
                        item.ReleaseDay = anime.PubDate;
                    }
                    else if (episodeNumber == item.LatestEpisode)
                    {
                        item.LatestEpisodeLink = anime.Link;
                    }
                    return true;
                }
            }
            return false;
        }
        public void WriteWatchListFile()
        {
            if (WatchList.Count == 0)
                return;

            using var fs = new StreamWriter(fileName,false);
            foreach (var item in WatchList)
            {
                fs.WriteLine($"{item.Title};{item.LatestEpisode};{item.IsDownloaded};{item.ReleaseDay}");
            }
        }
        public void SetAnimeAsDownloadedByName(Item anime)
        {
            foreach (var item in WatchList)
            {
                if (anime.Title.Contains(item.Title))
                {
                    int episodeNumber = GetAnimesEpisodeNumber(anime.Title);
                    if(episodeNumber == item.LatestEpisode)
                    {
                        item.IsDownloaded = true;
                        item.ReleaseDay = anime.PubDate;
                        return;
                    }
                }
            }
        }
        public void SetAnimeAsDownloadedByWatchListIndex(int index) => WatchList[index].IsDownloaded = true;
        public void RemoveEntryFromWatchList(int index, List<Item> animeList)
        {
            if(index >= WatchList.Count)
            {
                Program.DisplayError($"ERROR: INVALID INDEX {index}, SO IT WAS IGNORED");
                return;
            }
            
            WatchList.RemoveAt(index);

            // Rechecks the whole list for containing animes
            foreach (var anime in animeList)
            {
                anime.IsInWatchList = ContainsInWatchList(anime);
            }
        }
        public void RemoveMultipleEntriesFromWatchList(string[] indexes, List<Item> animeList)
        {
            List<WatchListItem> tmp = new List<WatchListItem>(indexes.Length);

            foreach (var item in indexes)
            {
                if (int.TryParse(item, out int index) && index < WatchList.Count)
                {
                    tmp.Add(new WatchListItem()
                    {
                       Title = WatchList[index].Title,
                       LatestEpisode = WatchList[index].LatestEpisode,
                       IsDownloaded = WatchList[index].IsDownloaded
                    });        
                }
                else
                {
                    Program.DisplayError($"ERROR: INDEX {item} DOES NOT EXIST, STOPPING THE REMOVAL");
                    return;
                }
            }

            foreach (var item in tmp)
            {
                WatchList.Remove(new WatchListItem() 
                { 
                    Title = item.Title, 
                    LatestEpisode = item.LatestEpisode,
                    IsDownloaded = item.IsDownloaded 
                });
            }
            foreach (var anime in animeList)
            {
                anime.IsInWatchList = ContainsInWatchList(anime);
            }
        }
        public string GetWatchListItemLink(int index) => WatchList[index].LatestEpisodeLink;
        public void SortWatchList() => WatchList.Sort();

        private int GetAnimesEpisodeNumber(string AnimeName)
        {
            for (int i = AnimeName.Length - 1; i != 0; i--)
            {
                if (AnimeName[i] == '-')
                {
                    var newSplit = AnimeName.Substring(i).Split(' ');// FORMAT: [-][episodeNumber][othershit]...
                    if (int.TryParse(newSplit[1], out int number))
                        return number;
                    else
                        return -1;
                }
            }
            return -1;
        }
        private void ReadWatchListFile()
        {
            if (!File.Exists(fileName))
                return;

            using var fs = new StreamReader(fileName);
            while (!fs.EndOfStream)
            {
                //Format title;latest episode;is downloaded
                var line = fs.ReadLine().Split(';');
                if(line.Length != 4)
                {
                    Program.DisplayError("Error happened while reading the watchlist file");
                    return;
                }    
                WatchList.Add(new WatchListItem()
                {
                    Title = line[0],
                    LatestEpisode = int.Parse(line[1]),
                    IsDownloaded = bool.Parse(line[2]),
                    ReleaseDay = DateTime.Parse(line[3])
                });
            }
        }
    }
}
