﻿#region

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;

#endregion

namespace JosephM.Xrm.Autonumber
{
    public abstract class XrmNNPluginRegistration
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var relationshipSchemaName = "";
            if (context.InputParameters.Contains("Relationship") &&
                context.InputParameters["Relationship"] is Relationship)
                relationshipSchemaName = ((Relationship)context.InputParameters["Relationship"]).SchemaName;
            var plugin = CreateNNPlugin(relationshipSchemaName);
            plugin.ServiceProvider = serviceProvider;
            plugin.Go();
        }

        public abstract XrmNNPlugin CreateNNPlugin(string relationshipName);
    }

    /// <summary>
    ///     Class storing properties and methods common for plugins on all entity types
    ///     A specific entity may extend this class to implement plugins specific for that entity type
    /// </summary>
    public class XrmNNPlugin : XrmPlugin
    {
        public override string TargetType
        {
            get { return Target.LogicalName; }
        }

        public override void Go()
        {
            GoExtention();
        }

        public virtual void GoExtention()
        {
            Trace("In base nnplugin Go not overridden for type");
        }

        #region instance properties

        public string RelationshipName
        {
            get { return ((Relationship)Context.InputParameters["Relationship"]).SchemaName; }
        }

        public bool IsTargetTypeReferencingRole
        {
            get
            {
                return ((Relationship)Context.InputParameters["Relationship"]).PrimaryEntityRole ==
                       EntityRole.Referencing;
            }
        }

        /// <summary>
        ///     The target of the message
        /// </summary>
        internal EntityReference Target
        {
            get { return ((EntityReference)Context.InputParameters["Target"]); }
        }

        /// <summary>
        ///     The id of the target entity reference
        /// </summary>
        internal Guid TargetId
        {
            get { return Target.Id; }
        }

        /// <summary>
        ///     The related ids for the message
        /// </summary>
        internal Guid[] RelatedEntities
        {
            get
            {
                var ids = new List<Guid>();
                foreach (var item in RelatedEntityReferenceCollection)
                    ids.Add(item.Id);
                return ids.ToArray();
            }
        }

        public string RelatedType
        {
            get
            {
                if (RelatedEntities != null && RelatedEntities.Any())
                    return RelatedEntityReferenceCollection.ElementAt(0).LogicalName;
                return "";
            }
        }

        private EntityReferenceCollection RelatedEntityReferenceCollection
        {
            get { return (EntityReferenceCollection)Context.InputParameters["RelatedEntities"]; }
        }

        #endregion
    }
}