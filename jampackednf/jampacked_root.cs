using System;
using libJAudio;


namespace jampacked
{
    class jampacked_root
    {
        static void Main(string[] args)
        {
#if DEBUG 
            args = new string[]
            {
                "pack",
                "out",
                "test.aaf"
            };
#endif
            cmdarg.cmdargs = args;
            Console.WriteLine("jampacked JAudio Archive packer / unpacker");
            var operation = cmdarg.assertArg(0, "operation");
            switch (operation)
            {
                case "unpack":
                    var aaFile = cmdarg.assertArg(1, "BAA/AAF/BX file");
                    unpack.unpack_do(aaFile);
                    break;
                case "pack":
                    var projectFile = cmdarg.assertArg(1, "Project Folder");
                    pack.pack_do(projectFile);
                    break;
                case "help":
                    HelpManifest.print_general();
                    break;
                default:
                    Console.WriteLine($"Unknown operation '{operation}'. See 'jampacked help'");
                    break;
            }
        }
    }
}
