using System.Collections.Generic;
using JosephM.Xrm.Autonumber.Services;

namespace JosephM.Xrm.Autonumber.Test
{
    public abstract class JosephMXrmTest : XrmTest
    {
        protected override IEnumerable<string> EntitiesToDelete
        {
            get { return new [] { "account"}; }
        }

        private AutonumberService _autonumberService;
        public AutonumberService AutonumberService
        {
            get
            {
                if (_autonumberService == null)
                    _autonumberService = new AutonumberService(XrmService);
                return _autonumberService;
            }
        }
    }
}