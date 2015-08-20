using JosephM.Xrm.Autonumber.Plugins;
using Microsoft.Xrm.Sdk;
using Schema;

namespace JosephM.Xrm.Autonumber
{
    public class AutonumberConfigPluginRegistration : XrmPluginRegistration
    {
        public override XrmPlugin CreateEntityPlugin(string entityType, bool isRelationship)
        {
            switch (entityType)
            {
                case Entities.jmcg_autonumber : return new AutonumberConfigPlugin();
            }
            throw new InvalidPluginExecutionException(string.Format("Expected entity of type {0} for {1} plugin. Actual type was {2}", Entities.jmcg_autonumber, GetType().Name, entityType));
        }
    }
}