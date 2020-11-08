using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Be.IO;
using libJAudio;
using libJAudio.Loaders;
using Newtonsoft.Json;

namespace jampacked
{
    public static partial class unpack
    {
        public static int aaf_GetSectionSize(JAIInitSection sect, BeBinaryReader br)
        {
            switch (sect.type)
            {
                /*
                // The following is true for both V1-Type banks
                case JAIInitSectionType.IBNK:
                    br.BaseStream.Position = sect.start;
                    br.ReadUInt32(); // Skip IBNK header.
                    return br.ReadInt32() + 8; // next operator is size
                case JAIInitSectionType.WSYS:
                    br.BaseStream.Position = sect.start;
                    br.ReadUInt32(); // Skip WSYS header.
                    return br.ReadInt32() + 8; // next operator is size
                   
                case JAIInitSectionType.UNKNOWN:
                    br.BaseStream.Position = sect.start;
                    br.ReadInt32();
                    return br.ReadInt32();
                     */
                default:
                    return sect.size;
            }
        }
        public static void unpack_aaf(JAIInitSection[] data, BeBinaryReader br, string projDir, string initName)
        {
            var sectList = new List<JAIInitSection>(data);
            try
            {
                Directory.CreateDirectory(projDir);
                Directory.CreateDirectory(projDir + "/include");
            }
            catch (Exception e)
            {
                cmdarg.assert("Could not create project directory: {0}", e.Message);
            }

            var pFile = new jAAFProjectFile();
            pFile.originalFile = initName;
            pFile.projectName = projDir;
            var banks = sectList.FindAll(p => p.type == JAIInitSectionType.IBNK);
            var waves = sectList.FindAll(p => p.type == JAIInitSectionType.WSYS);

            pFile.banks = new jAAFIncludeRecord[banks.Count];
            pFile.waves = new jAAFIncludeRecord[waves.Count];
            int bankIdx = 0;
            int waveIdx = 0;
            Console.WriteLine("Unpack AAF");
            Console.WriteLine("Unpacking clusters first...");
            for (int i = 0; i < banks.Count; i++)
            {
                var cSect = banks[i];
                var size = aaf_GetSectionSize(cSect, br); 
                br.BaseStream.Position = cSect.start;
                var sectionData = br.ReadBytes(size);
                var extension = util.GetFileExtension(cSect);
                File.WriteAllBytes($"{projDir}/include/{bankIdx}{extension}", sectionData);
                Console.WriteLine($"->\tWrote {bankIdx}{extension}");
                pFile.banks[i] = new jAAFIncludeRecord()
                {
                    hash = cSect.raw_header,
                    path = $"include/{bankIdx}{extension}",
                    type = cSect.type.ToString(),
                    uid = cSect.number,
                    flags = cSect.flags,
                };
                bankIdx++;
            }

            for (int i = 0; i < waves.Count; i++)
            {
                var cSect = waves[i];
                var size = aaf_GetSectionSize(cSect, br);
                br.BaseStream.Position = cSect.start;
                var sectionData = br.ReadBytes(size);
                var extension = util.GetFileExtension(cSect);
                File.WriteAllBytes($"{projDir}/include/{waveIdx}{extension}", sectionData);
                Console.WriteLine($"->\tWrote {waveIdx}{extension}");
                pFile.waves[i] = new jAAFIncludeRecord()
                {
                    hash = cSect.raw_header,
                    path = $"include/{waveIdx}{extension}",
                    type = cSect.type.ToString(),
                    uid = cSect.number,
                    flags = cSect.flags,
                };
                waveIdx++;
            }

            Console.WriteLine("Unpacking sections....");

            var wavCluster = false;
            var instCluster = false; 

            pFile.includes = new jAAFIncludeRecord[data.Length - (waves.Count + banks.Count) +2];
 
            var fileIndex = 0;
            var sectCounter = 0;
            for (int i = 0; i < data.Length; i++)
            {
                var cSect = data[i];
                if (cSect.type == JAIInitSectionType.IBNK || cSect.type == JAIInitSectionType.WSYS)
                {
                    switch (cSect.type)
                    {
                        case JAIInitSectionType.IBNK:
                            if (instCluster == true)
                                continue;
                            pFile.includes[sectCounter] = new jAAFIncludeRecord()
                            {
                                hash = cSect.number,
                                path = $"This is a marker for the 'banks' cluster.",
                                type = "INSTRUMENT_CLUSTER",
                                uid = fileIndex,
                                flags = 0xFFFFFFF,
                            };
                            sectCounter++;
                            instCluster = true;
                            continue;                            
                        case JAIInitSectionType.WSYS:
                            if (wavCluster == true)
                                continue;
                            pFile.includes[sectCounter] = new jAAFIncludeRecord()
                            {
                                hash = cSect.number,
                                path = $"This is a marker for the 'waves' cluster.",
                                type = "WAVE_CLUSTER",
                                uid = fileIndex,
                                flags = 0x7777777,
                            };
                            sectCounter++;
                            wavCluster = true;
                            continue;
                    }
                }
                var size = aaf_GetSectionSize(cSect, br); // load section size from function (because BAA omits data for a lot of this :) )
                br.BaseStream.Position = cSect.start;
                var sectionData = br.ReadBytes(size);
                var extension = util.GetFileExtension(cSect);
                File.WriteAllBytes($"{projDir}/include/{fileIndex}{extension}", sectionData);
                Console.WriteLine($"->\tWrote {fileIndex}{extension}");
                pFile.includes[sectCounter] = new jAAFIncludeRecord()
                {
                    hash = cSect.raw_header,
                    path = $"include/{fileIndex}{extension}",
                    type = cSect.type.ToString(),
                    uid = cSect.number,
                    flags = cSect.flags,
                };
                sectCounter++;
                fileIndex++;
            }
            var convertedData = JsonConvert.SerializeObject(pFile, Formatting.Indented);
            File.WriteAllText($"{projDir}/project.json", convertedData);
            Console.WriteLine("Done.");
        }
    }
}
