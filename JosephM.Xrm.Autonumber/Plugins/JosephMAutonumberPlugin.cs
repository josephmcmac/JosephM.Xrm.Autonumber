using JosephM.Xrm.Autonumber.Services;

namespace JosephM.Xrm.Autonumber.Plugins
{
    public class JosephMAutonumberPlugin : XrmEntityPlugin
    {
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