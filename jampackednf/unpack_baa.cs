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
        public static int baa_GetSectionSize(JAIInitSection sect, BeBinaryReader br)
        {
            switch (sect.type)
            {
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
                default:
                    return sect.size;
            }
        }
        public static void unpack_baa(JAIInitSection[] data, BeBinaryReader br, string projDir, string initName)
        {
            try
            {
                Directory.CreateDirectory(projDir);
                Directory.CreateDirectory(projDir + "/include");
            }
            catch (Exception e)
            {
                cmdarg.assert("Could not create project directory: {0}", e.Message);
            }

            var pFile = new jBAAProjectFile();
            pFile.originalFile = initName;
            pFile.projectName = projDir;
            pFile.includes = new jBAAIncludeRecord[data.Length];
            Console.WriteLine("Unpack BAA");
            var fileIndex = 0;
            for (int i = 0; i < data.Length; i++)
            {
                var cSect = data[i];
                var size = baa_GetSectionSize(cSect, br); // load section size from function (because BAA omits data for a lot of this :) )
                br.BaseStream.Position = cSect.start;
                var sectionData = br.ReadBytes(size);
                var extension = util.GetFileExtension(cSect);
                File.WriteAllBytes($"{projDir}/include/{fileIndex}{extension}", sectionData);
                Console.WriteLine($"->\tWrote {fileIndex}{extension}");
                pFile.includes[i] = new jBAAIncludeRecord()
                {
                    hash = cSect.raw_header,
                    path = $"include/{fileIndex}{extension}",
                    type = cSect.type.ToString(),
                    uid = cSect.number,
                    flags = cSect.flags,
                };
                
                fileIndex++;
            }
            var convertedData = JsonConvert.SerializeObject(pFile, Formatting.Indented);
            File.WriteAllText($"{projDir}/project.json", convertedData);
        }
    }
}
