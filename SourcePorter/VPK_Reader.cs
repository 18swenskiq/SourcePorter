using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SourcePorter
{
    public class VPK_Reader
    {
        private string VPKEXE_PATH = "\"H:\\SteamLibrary\\steamapps\\common\\Counter-Strike Global Offensive\\bin\\vpk.exe\"";
        private string VPKDIR_PATH = "\"H:\\SteamLibrary\\steamapps\\common\\Counter-Strike Global Offensive\\csgo\\pak01_dir.vpk\"";
        public Process proc;
        public List<string> VPK_OUTPUT;
        public VPK_Reader()
        {
            string arguments = $"l {VPKDIR_PATH}";
            proc = new Process();
            proc.StartInfo.FileName = VPKEXE_PATH;
            proc.StartInfo.Arguments = arguments;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.CreateNoWindow = true;

            proc.Start();
            VPK_OUTPUT = new List<string>();
            while (!proc.StandardOutput.EndOfStream)
            {
                string line = proc.StandardOutput.ReadLine();
                if (line.StartsWith("materials/") || line.StartsWith("models/"))
                {
                    VPK_OUTPUT.Add(line);
                }
            }
            proc.Close();
        }

        public List<string> GetMaterialsUsedInVPK(List<string> materials)
        {
            var materialsUsed = new List<string>();
            foreach (var material in materials)
            {
                var fulldirmaterial = "materials/" + material + ".vmt";
                if (VPK_OUTPUT.Contains(fulldirmaterial))
                {
                    materialsUsed.Add(fulldirmaterial);
                }
            }
            return materialsUsed;
        }

        public List<string> GetModelsUsedInVPK(List<string> models)
        {
            var modelsUsed = new List<string>();
            foreach (var model in models)
            {
                if (VPK_OUTPUT.Contains(model))
                {
                    modelsUsed.Add(model);
                }
            }
            return modelsUsed;
        }

        public void GrabMaterialsFromVPK(List<string> materials)
        {
            var vpk_extractor = new VPK_Extractor("H:\\SteamLibrary\\steamapps\\common\\Counter-Strike Global Offensive\\csgo\\pak01_dir.vpk");
            foreach(var entry in vpk_extractor.VPKEntries)
            {
                var entrystring = $"{entry.DirectoryPath}/{entry.FileName}.{entry.FileExtension}";
                if(!materials.Contains(entrystring))
                {
                    continue;
                }
                string digits = "";
                if(entry.InfoEntry.ArchiveIndex < 100)
                {
                    if(entry.InfoEntry.ArchiveIndex < 10)
                    {
                        digits = "00";
                    }
                    else
                    {
                        digits = "0";
                    }
                }
                string filepath = $"H:\\SteamLibrary\\steamapps\\common\\Counter-Strike Global Offensive\\csgo\\pak01_{digits}{entry.InfoEntry.ArchiveIndex}.vpk";
                using (BinaryReader reader = new BinaryReader(File.Open(filepath, FileMode.Open)))
                {
                    reader.ReadBytes((int)entry.InfoEntry.EntryOffset);
                    var filedata = reader.ReadBytes((int)entry.InfoEntry.EntryLength);
                    Directory.CreateDirectory($"C:\\Users\\Quinton\\source\\repos\\SourcePorter\\SourcePorter\\bin\\Debug\\netcoreapp3.0\\source2\\{entry.DirectoryPath}");
                    File.WriteAllBytes($"C:\\Users\\Quinton\\source\\repos\\SourcePorter\\SourcePorter\\bin\\Debug\\netcoreapp3.0\\source2\\{entry.DirectoryPath}\\{entry.FileName}.{entry.FileExtension}", filedata);
                }
            }
        }



    }

    public class VPK_Extractor
    {
        public VPK_Extractor(string vpkfile)
        {

            using (BinaryReader reader = new BinaryReader(File.Open(vpkfile, FileMode.Open)))
            {
                Signature = reader.ReadUInt32();
                Version = reader.ReadUInt32();
                TreeSize = reader.ReadUInt32();
                FileDataSectionSize = reader.ReadUInt32();
                ArchiveMD5SectionSize = reader.ReadUInt32();
                OtherMD5SectionSize = reader.ReadUInt32();
                SignatureSectionSize = reader.ReadUInt32();

                while(true)
                {
                    VPKEntries = new List<VPKEntryPiece>();
                    if(!ReadDirectory(reader))
                    {
                        break;
                    }
                }          
            }
        }

        public uint Signature { get; set; }
        public uint Version { get; set; }
        public uint TreeSize { get; set; }
        public uint FileDataSectionSize { get; set; }
        public uint ArchiveMD5SectionSize { get; set; }
        public uint OtherMD5SectionSize { get; set; }
        public uint SignatureSectionSize { get; set; }
        public List<VPKEntryPiece> VPKEntries { get; set; }

        public bool ReadDirectory(BinaryReader reader)
        {
            while (true)
            {
                string extension = ReadString(reader);
                if (extension == "\0")
                {
                    continue;
                }
                while (true)
                {
                    string myPath = ReadString(reader);
                    if (myPath == "\0")
                    {
                        break;
                    }
                    while (true)
                    {
                        string myFileName = ReadString(reader);
                        if (myFileName == "\0")
                        {
                            break;
                        }
                        var newpiece = new VPKEntryPiece
                        {
                            DirectoryPath = myPath,
                            FileExtension = extension,
                            FileName = myFileName,
                            InfoEntry = new VPKDirectoryEntry
                            {
                                CRC = reader.ReadUInt32(),
                                PreloadBytes = reader.ReadUInt16(),
                                ArchiveIndex = reader.ReadUInt16(),
                                EntryOffset = reader.ReadUInt32(),
                                EntryLength = reader.ReadUInt32(),
                                Terminator = reader.ReadUInt16()
                            }
                        };
                        VPKEntries.Add(newpiece);
                    }
                }
                if (VPKEntries.Count == 71315) break;

            }
            return false;
        }

        public string ReadString(BinaryReader reader)
        {
            string mystring = "";
            while (true)
            {
                char mychar = reader.ReadChar();
                if(mychar == '\0')
                {
                    return mystring;
                }
                mystring += mychar;
            }
        }

    }

    public class VPKDirectoryEntry
    {
        public uint CRC { get; set; }
        public ushort PreloadBytes { get; set; }
        public ushort ArchiveIndex { get; set; }
        public uint EntryOffset { get; set; }
        public uint EntryLength { get; set; }
        public ushort Terminator { get; set; }
    }

    public class VPKEntryPiece
    {
        public string FileExtension { get; set; }
        public string DirectoryPath { get; set; }
        public string FileName { get; set; }
        public VPKDirectoryEntry InfoEntry { get; set; }

    }
}
