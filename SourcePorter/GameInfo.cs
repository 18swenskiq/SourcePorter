using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SourcePorter
{
    public class GameInfo
    {

        public List<string> SearchPaths { get; set; }

        public GameInfo()
        {
            var gameinfoFile = File.ReadAllLines(@"H:\SteamLibrary\steamapps\common\Counter-Strike Global Offensive\csgo\gameinfo.txt");
            var gameinfoLines = new List<string>(gameinfoFile);
            bool searchPathFlag = false;
            var searchpaths = new List<string>();

            for (int i = 0; i < gameinfoLines.Count - 1; i++)
            {
                if(gameinfoLines[i].Trim().ToLower() == "searchpaths")
                {
                    searchPathFlag = true;
                    continue;
                }
                if(searchPathFlag)
                {
                    if (gameinfoLines[i].Trim().Contains('}'))
                    {
                        break;
                    }
                    if (gameinfoLines[i].Trim().Contains("//"))
                    {
                        continue;
                    }
                    var thisline = gameinfoLines[i].Split(null);
                    thisline = thisline.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    if(thisline[0].ToLower() == "game")
                    {
                        string path = thisline[1];
                        if( Path.IsPathRooted(path))
                        {
                            searchpaths.Add(thisline[1]);
                        }
                    }
                }
            }
            searchpaths.Add("H:\\SteamLibrary\\steamapps\\common\\Counter-Strike Global Offensive\\csgo");
            SearchPaths = searchpaths;

        }
    }
}
