using System;
using System.Collections.Generic;
using System.Text;

namespace jampacked
{
    public class jBAAProjectFile
    {
        public string originalFile;
        public string projectName;
        public string format = "BAA";
        public jBAAIncludeRecord[] includes;
    }

    public class jBAAIncludeRecord
    {
        public int hash;
        public string type;
        public string path;
        public int uid;
        public int flags;
    }



    public class jAAFProjectFile
    {
        public string originalFile;
        public string projectName;
        public string format = "AAF";
        public jAAFIncludeRecord[] banks;
        public jAAFIncludeRecord[] waves;
        public jAAFIncludeRecord[] includes;
    }

    public class jAAFIncludeRecord
    {
        public int hash;
        public string type;
        public string path;
        public int uid;
        public int flags;
    }
}
