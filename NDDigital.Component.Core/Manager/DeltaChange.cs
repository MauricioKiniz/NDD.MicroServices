using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NDDigital.Component.Core.Manager
{
    public abstract class DeltaChange
    {
        public DeltaChangeEnum Kind { get; set; }
    }

    public class EndpointDeltaChange: DeltaChange
    {
        public string EndpointId { get; set; }

    }

    public class MapperDeltaChanger: DeltaChange
    {
        public List<string> Endpoints = new List<string>();
    }

    public enum DeltaChangeEnum
    {
        Deactivated = 0,
        Activated = 1,
        Changed = 2
    }
}
