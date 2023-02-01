using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expresso.Components
{
    internal interface IWorkflowStatusReporter
    {
        public void Update(string status);
    }
}
