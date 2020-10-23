using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Be.IO;
using libJAudio;
using libJAudio.Loaders;
using Newtonsoft.Json;
using Newtonsoft;
using Newtonsoft.Json.Linq;

namespace jampacked
{
    public static partial class pack
    {
        public static void pack_do(string file)
        {
            string projectFileData = null;  // Initialize file data buffer
            if (!File.Exists($"{file}/project.json"))
                cmdarg.assert($"Cannot open project.json in folder '{file}'");
            try
            {
                projectFileData = File.ReadAllText($"{file}/project.json"); // Try to read
            }
            catch (Exception E)
            {
                cmdarg.assert("Cannot open project file {0}", E.Message); // Assert if there's an exception.
            }

            JToken data = null;
            JToken dType = null;
            string arcType = null; 
            try
            {
                data = JsonConvert.DeserializeObject<JToken>(projectFileData);
            } catch (Exception E) { cmdarg.assert("Project manifest deserialization failure: ", E.Message);  }
  
            try
            {
                dType = data["format"]; 
            } catch { cmdarg.assert("Couldn't retrieve archive type from project file. Invalid project file.");  }

            arcType = dType.ToString();

            Console.WriteLine($"Pack project type {arcType}");
            switch (arcType)
            {
                case "BAA":                    
                    var projectFile = JsonConvert.DeserializeObject<jBAAProjectFile>(projectFileData);
                    var outName = cmdarg.tryArg(2, "outName");
                    if (outName == null)
                        outName = projectFile.originalFile;

                    pack.pack_baa(file, projectFile, outName);
                    break;
                case "AAF":

                    break;
                case "BX":

                    break;
                default:
                    cmdarg.assert($"Unknown project format: {arcType}");
                    break;
            }

        }
    }
}
