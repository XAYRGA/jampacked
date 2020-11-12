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
        public static void unpack_do(string file)
        {
            var projectdir = cmdarg.assertArg(2, "Output Directory"); // Check / assert for project directory

            byte[] jFileData = null;  // Initialize file data buffer
            MemoryStream jStreamData = null; // Initialize file data stream 
            try
            {
                jFileData = File.ReadAllBytes(file); // Try to read
                jStreamData = new MemoryStream(jFileData); // wrap stream around read bytes

            } catch (Exception E)
            {
                cmdarg.assert("Cannot open JAIInitFile {0}", E.Message); // Assert if there's an exception.
            }

            var jBinaryReader = new BeBinaryReader(jStreamData); // Wrap binaryreader around stream
            var jvType = JAIInitTypeDetector.checkVersion(ref jFileData); // Check bank version
           // var jvSys = JASystemLoader.loadJASystem(ref jFileData); // load / parse the full bank. 
           
            switch (jvType)
            {
                case JAIInitType.AAF:
                    {
                        var iJSystemInit = new JA_AAFLoader();
                        var sect = iJSystemInit.load(ref jFileData);
                        unpack_aaf(sect, jBinaryReader, projectdir, file);
                        break;
                    }
                case JAIInitType.BAA:
                    {
                        var iJSystemInit = new JA_BAALoader();
                        var sect = iJSystemInit.load(ref jFileData);
                        unpack_baa(sect, jBinaryReader, projectdir, file);
                        break;
                    }
                case JAIInitType.BX:
                    break;
            }

        }
    }
}
