using System;

namespace MultiproviderTest.Domain
{
    public class EngineResult<T>
    {
        public string Provider { get; set; }
        public T Response { get; set; }

        public string Status { get; set; }
        public Exception Ex { get; set; }
    }
}