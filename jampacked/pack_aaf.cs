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

        public class aafSectionHeaderInfo
        {
            public int size; 
        }
        public static aafSectionHeaderInfo aaf_GetSectionHeaderInfo(jAAFIncludeRecord inc)
        {
            var ret = new aafSectionHeaderInfo();
            switch (inc.hash)
            {
                //* Regular section looks like
                // i32_type,i32_offset,i32_size,i32_flags
                case 1: // Sound Table
                case 4: // BGM collection
                case 5: // Stream Map
                case 6: // Unknown 
                case 7: // Unknown
                    ret.size = 3;
                    break;
                case 2:
                case 3:
                case 0:
                    return null;
                default:
                    cmdarg.assert($"cannot pack section type {inc.hash:X5}");
                    return null;
            }
            return ret;
        }

        public static void aaf_PackSection(int start, int size, jAAFIncludeRecord inc, BeBinaryWriter blockWrite ) 
        {

            switch (inc.hash)
            {

                case 1: // Sound Table
                case 4: // BGM collection
                case 5: // Stream Map
                case 6: // Unknown 
                case 7: // Unknown
                    blockWrite.Write(inc.hash); // sprawl out hash 
                    blockWrite.Write(start);
                    blockWrite.Write(size);
                    blockWrite.Write(inc.flags);
                    
                    break;
                default:
                    cmdarg.assert($"cannot pack section type {inc.hash:X5}");
                    break;
            }
        }

        public static void aaf_PackSection_instCluster()
        {

        }
       
        public static void pack_aaf(string projectDir, jAAFProjectFile project,string fileName)
        {
            if (fileName == null)
                fileName = project.originalFile;

            var blockStrm = File.OpenWrite(fileName);
            var blockWrite = new BeBinaryWriter(blockStrm);


            Console.WriteLine("Prewriting AAF header data");

            for (int i=0; i < project.includes.Length; i++)
            {
                var w = project.includes[i];
                var sz = aaf_GetSectionHeaderInfo(w);
                switch (w.type) {
                    case "INSTRUMENT_CLUSTER":
                        Console.WriteLine("INST CLUSTER.");
                        blockWrite.Write(2);
                        for (int k = 0; k < project.banks.Length; k++)
                        {
                            blockWrite.Write((int)0x01); // Offset
                            blockWrite.Write((int)0x02); // Size
                            blockWrite.Write((int)0x03); // Flags 
                        }     
                        blockWrite.Write((int)0x00); // Cluster end indicator.
                        break;
                    case "WAVE_CLUSTER":
                        blockWrite.Write(3);
                        for (int k = 0; k < project.waves.Length; k++)
                        {
                            blockWrite.Write((int)0x04); // Offset
                            blockWrite.Write((int)0x05); // Size
                            blockWrite.Write((int)0x06); // Flags 
                        }
                        blockWrite.Write((int)0x00); // Cluster end indicator.
                        break;
                    default:
                        blockWrite.Write(w.hash);
                        for (int k = 0; k < sz.size; k++)
                            blockWrite.Write((int)0x00);
                        break;
                }
              
            }

            while ((blockWrite.BaseStream.Position & 0xF) != 0)
            {
                blockWrite.Write((byte)0x00); // oh god im sorry 
                blockWrite.Flush();
            }

            //blockWrite.Flush();
            //Environment.Exit(0);

            var head_anchor = 0L; 
            var tail_anchor = blockWrite.BaseStream.Position;
            Console.WriteLine($"Header ends at 0x{tail_anchor:X}");
            Console.WriteLine($"-> Project has {project.includes.Length} includes.");
            Console.WriteLine("-> Building project...");
            for (int i = 0; i < project.includes.Length; i++)
            {
                var dep = project.includes[i]; // load include
                switch (dep.type)
                {
                    case "INSTRUMENT_CLUSTER":
                        blockWrite.BaseStream.Position = head_anchor;
                        blockWrite.Write((int)2); // Cluster end indicator.
                        head_anchor = blockWrite.BaseStream.Position;
                        blockWrite.BaseStream.Position = tail_anchor;

                        for (int k = 0; k < project.banks.Length; k++)
                        {
                            var waveDep = project.banks[k];
                            var waveData = File.ReadAllBytes($"{projectDir}/{waveDep.path}"); // read include data
                            Console.WriteLine($"->\t{projectDir}/{waveDep.path}\t(bnk)L:0x{waveData.Length:X} added.");
                            var startPos = tail_anchor; // set start pos to tail anchor
                            blockWrite.Write(waveData); // sprawl data into file 
                            while ((blockWrite.BaseStream.Position & 0xF) != 0)
                            {
                                blockWrite.Write((byte)0x00); // oh god im sorry 
                                blockWrite.Flush();
                            }
                            var endPos = waveData.Length; // store end position
                            tail_anchor = blockWrite.BaseStream.Position; // set tail anchor to end pos
                            blockWrite.BaseStream.Position = head_anchor; // jump to head anchor 
                            blockWrite.Write((int)startPos);
                            blockWrite.Write((int)endPos);
                            blockWrite.Write((int)waveDep.flags);
                            head_anchor = blockWrite.BaseStream.Position; // update head anchor. 
                            blockWrite.BaseStream.Position = tail_anchor; // reseek to tail anchor. 
                        }
                        blockWrite.BaseStream.Position = head_anchor;
                        blockWrite.Write((int)0x00); // Cluster end indicator.
                        head_anchor = blockWrite.BaseStream.Position;
                        blockWrite.BaseStream.Position = tail_anchor;
                        continue;
                    case "WAVE_CLUSTER":
                        {
                            blockWrite.BaseStream.Position = head_anchor;
                            blockWrite.Write((int)3); // Cluster end indicator.
                            head_anchor = blockWrite.BaseStream.Position;
                            blockWrite.BaseStream.Position = tail_anchor;
                            for (int k = 0; k < project.waves.Length; k++)
                            {
                                var waveDep = project.waves[k];
                                var waveData = File.ReadAllBytes($"{projectDir}/{waveDep.path}"); // read include data
                                Console.WriteLine($"->\t{projectDir}/{waveDep.path}\t(wsy)L:0x{waveData.Length:X} added.");
                                var startPos = tail_anchor; // set start pos to tail anchor
                                blockWrite.Write(waveData); // sprawl data into file 
                                while ((blockWrite.BaseStream.Position & 0xF) != 0)
                                {
                                    blockWrite.Write((byte)0x00); // oh god im sorry 
                                    blockWrite.Flush();
                                }
                                var endPos = waveData.Length; // store end position
                                tail_anchor = blockWrite.BaseStream.Position; // set tail anchor to end pos
                                blockWrite.BaseStream.Position = head_anchor; // jump to head anchor 
                                blockWrite.Write((int)startPos);
                                blockWrite.Write((int)endPos);
                                blockWrite.Write((int)waveDep.flags);
                                head_anchor = blockWrite.BaseStream.Position; // update head anchor. 
                                blockWrite.BaseStream.Position = tail_anchor; // reseek to tail anchor. 
                            }
                            blockWrite.BaseStream.Position = head_anchor;
                            blockWrite.Write((int)0x00); // Cluster end indicator.
                            head_anchor = blockWrite.BaseStream.Position;
                            blockWrite.BaseStream.Position = tail_anchor;
                            continue;
                        }
  
                }

    
                var data = File.ReadAllBytes($"{projectDir}/{dep.path}"); // read include data
                Console.WriteLine($"->\t{projectDir}/{dep.path}\tL:0x{data.Length:X} added.");
                var sPos = tail_anchor; // set start pos to tail anchor
                blockWrite.Write(data); // sprawl data into file 
                while ((blockWrite.BaseStream.Position  & 0xF) != 0)
                {
                    blockWrite.Write((byte)0x00); // oh god im sorry 
                    blockWrite.Flush();
                }
                var ePos = blockWrite.BaseStream.Position; // store end position
                tail_anchor = ePos; // set tail anchor to end pos
                blockWrite.BaseStream.Position = head_anchor; // jump to head anchor 
                aaf_PackSection((int)sPos, (int)data.Length, dep, blockWrite); // write section header
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
