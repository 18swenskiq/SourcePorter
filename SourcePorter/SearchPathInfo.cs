using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SourcePorter
{
    public class SearchPathInfo
    {

        public Dictionary<string, string> SearchPathFindingsModels { get; set; }
        public Dictionary<string, string> SearchPathFindingsMaterials { get; set; }

        public SearchPathInfo(List<string> searchpaths, List<string> models, List<string> materials)
        {
            SearchPathFindingsModels = new Dictionary<string, string>();
            SearchPathFindingsMaterials = new Dictionary<string, string>();
            foreach (var searchpath in searchpaths)
            {
                foreach(var model in models)
                {
                    var newpath = Path.Combine(searchpath, model);
                    if(File.Exists(newpath))
                    {
                        if (!SearchPathFindingsModels.ContainsKey(model))
                        {
                            SearchPathFindingsModels.Add(model, newpath);
                        }
                    }
                }

                foreach(var material in materials)
                {
                    var newpath = Path.Combine(searchpath, material);
                    if(File.Exists(newpath))
                    {
                        if (!SearchPathFindingsMaterials.ContainsKey(material))
                        {
                            SearchPathFindingsMaterials.Add(material, newpath);
                        }
                    }
                }
            }
        }
    }
}
