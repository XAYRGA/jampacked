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
    public static partial class pack
    {

        public class baaSectionHeaderInfo
        {
            public int size; 
        }
        public static baaSectionHeaderInfo baa_GetSectionHeaderInfo(jBAAIncludeRecord inc)
        {
            var ret = new baaSectionHeaderInfo();
            switch (inc.hash)
            {
                
                case libJAudio.Loaders.JA_BAALoader.WS:
                case libJAudio.Loaders.JA_BAALoader.BMS:
                    ret.size = 3;
                    break;
                case libJAudio.Loaders.JA_BAALoader.BSFT:
                case libJAudio.Loaders.JA_BAALoader.BFCA:
                    ret.size = 1;
                    break;
                case libJAudio.Loaders.JA_BAALoader.BST:
                case libJAudio.Loaders.JA_BAALoader.BSTN:
                case libJAudio.Loaders.JA_BAALoader.BAAC:
                case libJAudio.Loaders.JA_BAALoader.BNK:
                case libJAudio.Loaders.JA_BAALoader.BSC:
                    ret.size = 2;
                    break;
                default:
                    cmdarg.assert($"cannot pack section type {inc.hash:X5}");
                    return null;
            }
            return ret;
        }

        public static void baa_PackSection(int start, int end, jBAAIncludeRecord inc, BeBinaryWriter blockWrite ) 
        {
            blockWrite.Write(inc.hash); // sprawl out hash 
            switch (inc.hash)
            {

                case libJAudio.Loaders.JA_BAALoader.WS:
                    blockWrite.Write(inc.uid);
                    blockWrite.Write(start);
                    blockWrite.Write(inc.flags);
                    break;
                case libJAudio.Loaders.JA_BAALoader.BNK:
                    blockWrite.Write(inc.uid);
                    blockWrite.Write(start);
                    break;
                case libJAudio.Loaders.JA_BAALoader.BSFT:
                case libJAudio.Loaders.JA_BAALoader.BFCA:
                    blockWrite.Write(start);
                    break;
                case libJAudio.Loaders.JA_BAALoader.BST:
                case libJAudio.Loaders.JA_BAALoader.BSTN:
                case libJAudio.Loaders.JA_BAALoader.BAAC:
                
                case libJAudio.Loaders.JA_BAALoader.BSC:
                    blockWrite.Write(start);
                    blockWrite.Write(end);
                    break;
                case libJAudio.Loaders.JA_BAALoader.BMS:
                    blockWrite.Write(inc.uid);
                    blockWrite.Write(start);
                    blockWrite.Write(end);
                    break;
                default:
                    cmdarg.assert($"cannot pack section type {inc.hash:X5}");
                    break;
            }
        }
       
        public static void pack_baa(string projectDir, jBAAProjectFile project,string fileName)
        {
            if (fileName == null)
                fileName = project.originalFile;

            var blockStrm = File.OpenWrite(fileName);
            var blockWrite = new BeBinaryWriter(blockStrm);


            Console.WriteLine("Prewriting BAA header data");
            // Prewrite header data so it's length is absolute
            blockWrite.Write(libJAudio.Loaders.JA_BAALoader.BAA_Header);
            for (int i=0; i < project.includes.Length; i++)
            {
                var w = project.includes[i];
                var sz = baa_GetSectionHeaderInfo(w);
                blockWrite.Write(w.hash);
                for (int k = 0; k < sz.size ;k++)
                    blockWrite.Write((int)0x00);
            }
            blockWrite.Write(libJAudio.Loaders.JA_BAALoader.BAA_Footer);
            blockWrite.Flush();
            var head_anchor = 4l; // Start past the AA_< 
            var tail_anchor = blockWrite.BaseStream.Position;
            Console.WriteLine($"Header ends at 0x{tail_anchor:X}");
            Console.WriteLine($"-> Project has {project.includes.Length} includes.");
            Console.WriteLine("-> Building project...");
            for (int i = 0; i < project.includes.Length; i++)
            {
                
                var dep = project.includes[i]; // load include
                var data = File.ReadAllBytes($"{projectDir}/{dep.path}"); // read include data
                Console.WriteLine($"->\t{projectDir}/{dep.path}\tL:0x{data.Length:X} added.");
                var sPos = tail_anchor; // set start pos to tail anchor
                blockWrite.Write(data); // sprawl data into file 
                //util.padTo(blockWrite, 8); // pad to 32
                while ((blockWrite.BaseStream.Position  & 0xF) != 8)
                {
                    //Console.WriteLine("padding...");
                    // Console.WriteLine(blockWrite.BaseStream.Position % 16);
                    blockWrite.Write((byte)0x00); // oh god im sorry 
                    blockWrite.Flush();
                }
                var ePos = blockWrite.BaseStream.Position; // store end position
                tail_anchor = ePos; // set tail anchor to end pos
                blockWrite.BaseStream.Position = head_anchor; // jump to head anchor 
                baa_PackSection((int)sPos, (int)ePos, dep, blockWrite); // write section header
                head_anchor = blockWrite.BaseStream.Position; // update head anchor. 
                blockWrite.BaseStream.Position = tail_anchor; // reseek to tail anchor. 
                // repeat :)          
            }

            Console.WriteLine($"-> Flushing into {fileName}");
            blockWrite.Flush();
            blockStrm.Flush();
            blockWrite.Close();
            blockStrm.Close();
            Console.WriteLine("Done.");

        }
    }
}
