using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SourcePorter
{
    public class VPK_Reader
    {
        // TODO: make program auto find these paths(or just ask the user to input them since i'm not 100% sure how that'd work)
        private string VPKEXE_PATH = $"\"{Program.Paths.vpk_exe}\"";
        private string VPKDIR_PATH = $"\"{Program.Paths.pak_dir}\"";
        public Process proc;
        public List<string> VPK_OUTPUT;

        // This is the value that the archive index for an entry is if it is actually stored in the dir VPK. 
        // Hexadecimal for this is 0x7FFF
        public static ushort preloadIndex = 32767;

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
                    Console.WriteLine(fulldirmaterial);
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

        VPK_Extractor vpk_extractor = new VPK_Extractor(Program.Paths.pak_dir);

        public int GrabMaterialsFromVPK(List<string> materials)
        {
            int meterialsExtracted = 0;
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

                string filepath = $"{Program.Paths.game_path}\\pak01_{digits}{entry.InfoEntry.ArchiveIndex}.vpk";

                bool isPreload = false;

                // If the archive index is the preloadIndex, that means that this entry is preloaded into the dir and is not in an external VPK.
                if (entry.InfoEntry.ArchiveIndex == preloadIndex || entry.InfoEntry.PreloadBytes > 0)
                {
                    filepath = $"{Program.Paths.pak_dir}";
                    isPreload = true;
                }
                    
                // If part of the file is stored in the dir and part is stored in another VPK, append the part stored in the external VPK to the end.
                if (entry.InfoEntry.ArchiveIndex != preloadIndex && entry.InfoEntry.PreloadBytes > 0)
                {
                    string vpkpath = $"{Program.Paths.game_path}\\pak01_{digits}{entry.InfoEntry.ArchiveIndex}.vpk";
                    using (BinaryReader reader = new BinaryReader(File.Open(vpkpath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                    {
                        reader.BaseStream.Position = entry.InfoEntry.EntryOffset;

                        byte[] fileContents = reader.ReadBytes((int)entry.InfoEntry.EntryLength);

                        List<byte> list = new List<byte>(entry.InfoEntry.PreLoadData);

                        foreach(byte b in fileContents)
                        {
                            list.Add(b);
                        }

                        entry.InfoEntry.PreLoadData = list.ToArray();

                        reader.Dispose();
                    }
                }

                using (BinaryReader reader = new BinaryReader(File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    reader.BaseStream.Position = entry.InfoEntry.EntryOffset;

                    // If file is stored in external VPK and not as a preload in the dir file, use entrylength. Otherwise use PreloadBytes.
                    if (!isPreload)
                    {
                        Directory.CreateDirectory($"source2\\{entry.DirectoryPath}");
                        
                        byte[] fileContents = reader.ReadBytes((int)entry.InfoEntry.EntryLength);
                        File.WriteAllBytes($"source2\\{entry.DirectoryPath}\\{entry.FileName}.{entry.FileExtension}", fileContents);
                        meterialsExtracted++;
                        reader.Dispose();
                    }
                    else
                    {
                        Directory.CreateDirectory($"source2\\{entry.DirectoryPath}");
                        File.WriteAllBytes($"source2\\{entry.DirectoryPath}\\{entry.FileName}.{entry.FileExtension}", entry.InfoEntry.PreLoadData);
                        meterialsExtracted++;
                        reader.Dispose();
                    }
                }
            }

            return meterialsExtracted;
        }

        public void GrabTexturesFromMaterials()
        {
            VProperty currentVmt = VdfConvert.Deserialize(File.ReadAllText("graygrid.vmt"));
            

            foreach(VToken token in currentVmt.Value)
            {
                Console.WriteLine(token.Value<VProperty>().Key);

                // Check if the value of this field is a texture in the VPK
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
            // Top level of the tree. This reads file extensions.
            while(true)
            {
                string fileExtension = ReadString(reader);

                if (fileExtension == string.Empty)
                {
                    break;
                }

                // Second level of the tree. This level contains all the file directories that are of the above file extension.
                while(true)
                {
                    string fileDir = ReadString(reader);

                    if (fileDir == string.Empty)
                    {
                        break;
                    }

                    // Third level of the tree. This level contains all the file information needed for the above file directory.
                    while(true)
                    {
                        string fileName = ReadString(reader);

                        if (fileName == string.Empty)
                        {
                            break;
                        }

                        var entry = new VPKEntryPiece
                        {
                            DirectoryPath = fileDir,
                            FileExtension = fileExtension,
                            FileName = fileName,
                            InfoEntry = new VPKDirectoryEntry
                            {
                                CRC = reader.ReadUInt32(),
                                PreloadBytes = reader.ReadUInt16(),
                                ArchiveIndex = reader.ReadUInt16(),
                                EntryOffset = reader.ReadUInt32(),
                                EntryLength = reader.ReadUInt32(),
                                Terminator = reader.ReadUInt16(),
                                PreLoadData = new byte[0]
                            }
                        };
                        VPKEntries.Add(entry);

                        //if (entry.InfoEntry.ArchiveIndex != VPK_Reader.preloadIndex)
                        if(false)
                        Console.WriteLine($"{entry.InfoEntry.CRC}__" +
                            $"{entry.InfoEntry.PreloadBytes}__" +
                            $"{entry.InfoEntry.ArchiveIndex}[{entry.InfoEntry.ArchiveIndex.ToString("X")}]__" +
                            $"{entry.InfoEntry.EntryOffset}__" +
                            $"{entry.InfoEntry.EntryLength}__" +
                            $"{entry.InfoEntry.Terminator} || " +
                            $"{ entry.DirectoryPath}/{ entry.FileName}.{ entry.FileExtension}");

                        // According to VDC, if ArchiveIndex = 0x7fff(which is 32767 as a ushort), the preload of the file is contained in the dir.
                        // Meaning the file is in this file and not another vpk.
                        // After it determines that, it moves the binary reader past the preload bytes.
                        if (entry.InfoEntry.ArchiveIndex == 32767 || entry.InfoEntry.PreloadBytes != 0)
                        {
                            entry.InfoEntry.PreLoadData = reader.ReadBytes(entry.InfoEntry.PreloadBytes);
                        }
                        else
                        {
                            
                        }
                    }
                }

                if (fileExtension == string.Empty)
                {
                    break;
                }
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

        public byte[] PreLoadData { get; set; }

        public byte[] ArchiveData { get; set; }
    }

    public class VPKEntryPiece
    {
        public string FileExtension { get; set; }
        public string DirectoryPath { get; set; }
        public string FileName { get; set; }
        public VPKDirectoryEntry InfoEntry { get; set; }

    }
}
