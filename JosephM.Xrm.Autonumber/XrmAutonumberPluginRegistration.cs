using JosephM.Xrm.Autonumber.Plugins;

namespace JosephM.Xrm.Autonumber
{
    public class XrmAutonumberPluginRegistration : XrmPluginRegistration
    {
        public override XrmPlugin CreateEntityPlugin(string entityType, bool isRelationship)
        {
            return new XrmAutonumberPlugin();
        }
    }
}