using System;
using System.Collections.Generic;
using System.Text;

namespace GremlinStudio
{
    public class SampleService : ISampleService
    {
        public string GetCurrentDate() => DateTime.Now.ToLongDateString();
    }
}
