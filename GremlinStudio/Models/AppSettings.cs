using System;
using System.Collections.Generic;
using System.Text;

namespace GremlinStudio
{
    public class AppSettings
    {
        public string GremlinEndpoint { get; set; }
        public string AuthKey { get; set; }
        public string Db { get; set; }
        public string Collection { get; set; }
        public int IntegerSetting { get; set; }

        public bool BooleanSetting { get; set; }
    }
}
