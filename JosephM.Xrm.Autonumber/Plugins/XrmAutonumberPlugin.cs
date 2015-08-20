using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JosephM.Xrm.Autonumber.Plugins
{
    public class XrmAutonumberPlugin : JosephMAutonumberPlugin
    {
        public override void GoExtention()
        {
            AutonumberService.SetAutonumbers(this);
        }
    }
}
