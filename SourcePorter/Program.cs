﻿using Newtonsoft.Json;
using SourcePorter.Structs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using VMFParser;
using VMTParser;

namespace SourcePorter
{
    class Program
    {
        // Static variables ooOOooOO scary
        // What are you gonna do, cry
        // Piss your pants maybe

        // This contains all paths needed, loaded from the local file "paths.json"
        public static JSONPaths Paths { get; set; }

        static void Main(string[] args)
        {
            Console.WriteLine("Squidski's Source 1 -> Source 2 Project Environment Creator");

            // Create json if it doesn't exist, and load json into memory if it does.
            JSONInit();

            // If any of the paths in paths.json are invalid, stop the program.
            if (!File.Exists(Paths.game_info) || 
                !Directory.Exists(Paths.game_path) || 
                !Directory.Exists(Paths.main_path) || 
                !File.Exists(Paths.pak_dir) || 
                !File.Exists(Paths.vpk_exe))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("One or more of the paths in paths.json are invalid!");
                Console.ForegroundColor = ConsoleColor.White;
                Console.ReadKey();

                // Close. Environment.Exit(0) also tells the operating system the program was closed normally and didn't just crash.
                Environment.Exit(0);
            }

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            if(args.Length < 1)
            {
                Console.WriteLine("No parameters provided");
                Console.ReadKey();
                Environment.Exit(1);
            }

            Console.WriteLine("Parsing VMF...");
            string[] vmftext = File.ReadAllLines(args[0]);
            var userVMF = new VMF(vmftext);
            Console.WriteLine("VMF Successfully Parsed!");

            var entityUtil = new EntityUtil();

            Console.WriteLine("Reading entities...");
            var S1entityListinVMF = entityUtil.GetEntityList(userVMF);
            Console.WriteLine($"Found {S1entityListinVMF.Count} entities in VMF");

            Console.WriteLine("Getting list of prop models...");
            var S1propModelListinVMF = entityUtil.GetPropModelList(S1entityListinVMF);
            Console.WriteLine($"Found {S1propModelListinVMF.Count} unique prop models in VMF");

            Console.WriteLine("Getting list of decal materials...");
            var S1decalMaterialListinVMF = entityUtil.GetDecalMaterialList(S1entityListinVMF);
            Console.WriteLine($"Found {S1decalMaterialListinVMF.Count} decal materials in VMF");

            Console.WriteLine("Getting list of overlay materials...");
            var S1overlayMaterialListinVMF = entityUtil.GetOverlayMaterialList(S1entityListinVMF);
            Console.WriteLine($"Found {S1overlayMaterialListinVMF.Count} overlay materials in VMF");

            Console.WriteLine("Getting list of materials used on brush entities...");
            var S1brushEntityMaterialListinVMF = entityUtil.GetBrushEntityMaterialList(S1entityListinVMF);
            Console.WriteLine($"Found {S1brushEntityMaterialListinVMF.Count} materials used on brush entities in VMF");

            var materialReader = new MaterialReader();

            Console.WriteLine("Reading materials used on world geometry...");
            var S1brushMaterialsinVMF = materialReader.GetWorldGeoMaterials(userVMF);
            Console.WriteLine($"Found {S1brushMaterialsinVMF.Count} materials used on world geometry in VMF");

            Console.WriteLine("Combining material lists...");
            var S1usedMaterialsNoModels = materialReader.CombineMaterials(S1decalMaterialListinVMF, S1overlayMaterialListinVMF, S1brushEntityMaterialListinVMF, S1brushMaterialsinVMF);
            Console.WriteLine($"Found {S1usedMaterialsNoModels.Count} unique materials (excluding models)");

            Console.WriteLine("Getting asset information from VPK...");
            var vpk = new VPK_Reader();
            Console.WriteLine($"Successfully retrieved information on {vpk.VPK_OUTPUT.Count} assets inside VPK");

            Console.WriteLine("Finding decal, overlay, and brush textures used that are included in VPK...");
            var S1materialsInVPKNoModels = vpk.GetMaterialsUsedInVPK(S1usedMaterialsNoModels);
            Console.WriteLine($"Found {S1materialsInVPKNoModels.Count} materials (Missing {S1usedMaterialsNoModels.Count - S1materialsInVPKNoModels.Count})");

            Console.WriteLine("Finding models used that are included in the VPK...");
            var S1modelsInVPK = vpk.GetModelsUsedInVPK(S1propModelListinVMF);
            Console.WriteLine($"Found {S1modelsInVPK.Count} models (Missing {S1propModelListinVMF.Count - S1modelsInVPK.Count})");

            Console.WriteLine("Reading gameinfo.txt for additional searchpaths...");
            var gameinfo = new GameInfo();
            Console.WriteLine($"Found {gameinfo.SearchPaths.Count} additional searchpaths");

            Console.WriteLine("Scanning searchpaths for additional assets...");
            var S1modelsNotInVPK = GetListDifference(S1propModelListinVMF, S1modelsInVPK);
            var S1materialsNotInVPK = GetListDifference(S1usedMaterialsNoModels, S1materialsInVPKNoModels, true);

            var searchPathInfo = new SearchPathInfo(gameinfo.SearchPaths, S1modelsNotInVPK, S1materialsNotInVPK);
            Console.WriteLine($"Found {searchPathInfo.SearchPathFindingsMaterials.Count} Materials and {searchPathInfo.SearchPathFindingsModels.Count} Models in SearchPaths");

            if(S1materialsNotInVPK.Count - searchPathInfo.SearchPathFindingsMaterials.Count != 0)
            {
                Console.WriteLine($"Missing {S1materialsNotInVPK.Count - searchPathInfo.SearchPathFindingsMaterials.Count} materials after all searches");
            }
            if (S1modelsNotInVPK.Count - searchPathInfo.SearchPathFindingsModels.Count != 0)
            {
                Console.WriteLine($"Missing {S1modelsNotInVPK.Count - searchPathInfo.SearchPathFindingsModels.Count} models after all searches");
            }

            Console.WriteLine("Creating project directory structure...");
            Directory.CreateDirectory("source2/models");
            Directory.CreateDirectory("source2/materials");
            Console.WriteLine("Project directory structure sucessfully created!");


            Console.WriteLine("Getting materials from VPK");
            int materialsExtracted = vpk.GrabMaterialsFromVPK(S1materialsInVPKNoModels);
            Console.WriteLine($"{materialsExtracted} materials extracted successfully!");

            Console.WriteLine("Getting textures from materials");
            vpk.GrabTexturesFromMaterials();

            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            Console.WriteLine($"Completed in {ts.Seconds}.{ts.Milliseconds} seconds");
            Console.ReadKey();
        }

        // Creates "paths.json" file if it is not created and fills it will empty strings which will make the program fail until you manually input the paths
        // Then creates a JSONPaths object from the current paths.json and sets the Paths variable to it.
        public static void JSONInit()
        {
            // If file doesn't exist, create default paths.json
            if (!File.Exists("paths.json"))
            {
                JSONPaths paths = new JSONPaths();

                string fileText = JsonConvert.SerializeObject(paths, Formatting.Indented);

                File.WriteAllText("paths.json", fileText);

                Paths = paths;
            }
            else
            {
                Paths = JsonConvert.DeserializeObject<JSONPaths>(File.ReadAllText("paths.json"));
            }
        }

        static List<string> GetListDifference(List<string> List1, List<string> List2, bool isMaterials = false)
        {
            var DifferenceList = new List<string>();

            if(isMaterials)
            {
                List<string> ListCopy = List1.GetRange(0, List1.Count);
                foreach(string entry in ListCopy)
                {
                    var getindex = List1.IndexOf(entry);
                    List1[getindex] = "materials/" + entry + ".vmt";
                }
            }

            IEnumerable<string> differenceQuery = List1.Except(List2);

            foreach(string diff in differenceQuery)
            {
                DifferenceList.Add(diff);
            }
            return DifferenceList;
        }
    }
}
