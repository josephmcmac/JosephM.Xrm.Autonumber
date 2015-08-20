﻿#region

using System;
using System.Linq;
using Microsoft.Xrm.Sdk;

#endregion

namespace JosephM.Xrm.Autonumber
{
    public abstract class XrmPluginRegistration : IPlugin
    {
        #region IPlugin Members

        protected const string XRMRETRIEVEMULTIPLEFAKESCHEMANAME = "XRMRETRIEVEMULTIPLE";

        public void Execute(IServiceProvider serviceProvider)
        {
            string entityType;
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var message = context.MessageName;
            var isRelationship = message == PluginMessage.Associate || message == PluginMessage.Disassociate;
            entityType = GetTypeSchemaName(context, isRelationship);

            var plugin = CreateEntityPlugin(entityType, isRelationship);
            if (plugin != null)
            {
                plugin.ServiceProvider = serviceProvider;
                RegisterTypes(plugin);
                XrmPlugin.Go(plugin);
            }
        }

        private static string GetTypeSchemaName(IPluginExecutionContext context, bool isRelationship)
        {
            string entityType;
            if (isRelationship)
                entityType = ((Relationship)context.InputParameters["Relationship"]).SchemaName;
            else if (context.MessageName == PluginMessage.RetrieveMultiple)
                entityType = XRMRETRIEVEMULTIPLEFAKESCHEMANAME;
            else if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                entityType = ((Entity)context.InputParameters["Target"]).LogicalName;
            else if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is EntityReference)
                entityType = ((EntityReference)context.InputParameters["Target"]).LogicalName;
            else if (context.InputParameters.Contains("EntityMoniker") &&
                     context.InputParameters["EntityMoniker"] is EntityReference)
                entityType = ((EntityReference)context.InputParameters["EntityMoniker"]).LogicalName;
            else if (
                new[] { PluginMessage.AddMembers, PluginMessage.AddMember, PluginMessage.AddListMember }.Contains(
                    context.MessageName))
                entityType = "list";
            else if (context.InputParameters.Contains("LeadId") &&
                     context.InputParameters["LeadId"] is EntityReference)
                entityType = "lead";
            else if (context.InputParameters.Contains("EmailId"))
                entityType = "email";

            else
            {
                var args = "";
                args = args + "Message: " + context.MessageName;
                foreach (var item in context.InputParameters)
                {
                    if (args != "")
                        args = args + "\n" + item.Key + ": " + item.Value;
                    else
                        args = args + item.Key + ": " + item.Value;
                }
                throw new InvalidPluginExecutionException("Error Extracting Plugin Entity Type:\n" + args);
            }
            return entityType;
        }

        #endregion

        public virtual XrmPlugin CreateEntityPlugin(string entityType, bool isRelationship)
        {
            if (isRelationship)
                return new XrmNNPlugin();
            else
                return new XrmEntityPlugin();
        }

        public virtual void RegisterTypes(XrmPlugin plugin)
        {
        }
    }
}