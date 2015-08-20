using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JosephM.Xrm.Autonumber.Core;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Schema;

namespace JosephM.Xrm.Autonumber.Plugins
{
    public class AutonumberConfigPlugin : JosephMAutonumberPlugin
    {
        public override void GoExtention()
        {
            SetFieldType();
            CheckPluginRegistration();
            Validate();
            ReadOnlyFields();
        }

        private void ReadOnlyFields()
        {
            if (IsMessage(PluginMessage.Update) && IsStage(PluginStage.PostEvent) && IsMode(PluginMode.Synchronous))
            {
                if(FieldChanging(Fields.jmcg_autonumber_.jmcg_entitytype, Fields.jmcg_autonumber_.jmcg_autonumberfield))
                    throw new InvalidPluginExecutionException(string.Format("{0} and {1} cannot be changed. Create a new {2} record"
                        , GetFieldLabel(Fields.jmcg_autonumber_.jmcg_entitytype), GetFieldLabel(Fields.jmcg_autonumber_.jmcg_autonumberfield), GetEntityLabel()));
            }
        }

        private void Validate()
        {
            // !!!! DONT CHANGE TO PREOPERATION AS RELOADS THROUGH WEB SERVICE !!!!
            if (IsMessage(PluginMessage.Create, PluginMessage.Update) && IsStage(PluginStage.PostEvent) && IsMode(PluginMode.Synchronous))
            {
                if (FieldChanging(Fields.jmcg_autonumber_.jmcg_parentautonumber, Fields.jmcg_autonumber_.jmcg_parentautonumberlinks, Fields.jmcg_autonumber_.statecode))
                {
                    var xrmAutonumber = AutonumberService.GetAutonumber(TargetId);
                    var isChildAutonumber = xrmAutonumber.HasParent;
                    if (isChildAutonumber)
                    {
                        //validate the query
                        var linksToParentString = GetStringField(Fields.jmcg_autonumber_.jmcg_parentautonumberlinks);
                        if (linksToParentString.IsNullOrWhiteSpace())
                            throw new NullReferenceException(string.Format("{0} Is Required",
                                GetFieldLabel(Fields.jmcg_autonumber_.jmcg_parentautonumberlinks)));
                        AutonumberService.ValidateLinksToParent(xrmAutonumber);
                        //validate the parent autonumber a string
                        var parentField = xrmAutonumber.ParentAutonumberField;
                        var parentType = xrmAutonumber.LastLinkTarget;
                        if(XrmService.GetFieldType(parentField, parentType) != AttributeTypeCode.String)
                            throw new InvalidPluginExecutionException(string.Format("The {0} is required to be for a field of type string", GetFieldLabel(Fields.jmcg_autonumber_.jmcg_parentautonumber)));
                        //validate has separator
                        if (GetStringField(Fields.jmcg_autonumber_.jmcg_separator).IsNullOrWhiteSpace())
                            throw new InvalidPluginExecutionException(
                                string.Format("{0} is required when {1} is populated",
                                    GetFieldLabel(Fields.jmcg_autonumber_.jmcg_separator),
                                    GetFieldLabel(Fields.jmcg_autonumber_.jmcg_parentautonumber)));
                    }
                }
                if (FieldChanging(Fields.jmcg_autonumber_.jmcg_prefix))
                {
                    var theString = GetStringField(Fields.jmcg_autonumber_.jmcg_prefix);
                    if(theString.Any(char.IsDigit))
                        throw new InvalidPluginExecutionException(string.Format("{0} cannot contain any numeric characters", GetFieldLabel(Fields.jmcg_autonumber_.jmcg_prefix)));
                }
                if (FieldChanging(Fields.jmcg_autonumber_.jmcg_separator))
                {
                    var theString = GetStringField(Fields.jmcg_autonumber_.jmcg_separator);
                    if (theString.Any(char.IsLetterOrDigit))
                        throw new InvalidPluginExecutionException(string.Format("{0} cannot contain any alphanumeric numeric characters", GetFieldLabel(Fields.jmcg_autonumber_.jmcg_separator)));
                }
            }
        }

        private void CheckPluginRegistration()
        {
            if (IsMessage(PluginMessage.Create) && IsStage(PluginStage.PostEvent) && IsMode(PluginMode.Synchronous))
            {
                AutonumberService.RefreshPluginRegistrations(GetStringField(Fields.jmcg_autonumber_.jmcg_entitytype));
            }
            else if (IsMessage(PluginMessage.Update) && IsStage(PluginStage.PostEvent) && IsMode(PluginMode.Synchronous)
                && FieldChanging(Fields.jmcg_autonumber_.statecode))
            {
                AutonumberService.RefreshPluginRegistrations(GetStringField(Fields.jmcg_autonumber_.jmcg_entitytype));
            }
            else if (IsMessage(PluginMessage.Delete) && IsStage(PluginStage.PostEvent) && IsMode(PluginMode.Synchronous))
            {
                AutonumberService.RefreshPluginRegistrations((string)GetFieldFromPreImage(Fields.jmcg_autonumber_.jmcg_entitytype));
            }
        }

        private void SetFieldType()
        {
            if (IsMessage(PluginMessage.Create) && IsStage(PluginStage.PreOperationEvent))
            {
                var fieldType = XrmService.GetFieldType(GetStringField(Fields.jmcg_autonumber_.jmcg_autonumberfield),
                    GetStringField(Fields.jmcg_autonumber_.jmcg_entitytype));
                if (GetLookupGuid(Fields.jmcg_autonumber_.jmcg_parentautonumber).HasValue
                    && fieldType != AttributeTypeCode.String)
                    throw new InvalidPluginExecutionException(string.Format("{0} is required to be a field of type string where {1} is populated", GetFieldLabel(Fields.jmcg_autonumber_.jmcg_autonumberfield), GetFieldLabel(Fields.jmcg_autonumber_.jmcg_parentautonumber)));

                switch (fieldType)
                {
                    case AttributeTypeCode.String:
                        SetOptionSetField(Fields.jmcg_autonumber_.jmcg_autonumberfieldtype, OptionSets.Autonumber.AutonumberFieldType.String);
                        break;

                    case AttributeTypeCode.Integer:
                    case AttributeTypeCode.BigInt:
                        SetOptionSetField(Fields.jmcg_autonumber_.jmcg_autonumberfieldtype, OptionSets.Autonumber.AutonumberFieldType.Integer);
                        break;

                    default:
                        throw new InvalidPluginExecutionException(string.Format("Error setting {0}. Field {1} is an invalid type"
                            , GetFieldLabel(Fields.jmcg_autonumber_.jmcg_autonumberfieldtype), XrmService.GetFieldLabel(GetStringField(Fields.jmcg_autonumber_.jmcg_autonumberfield), GetStringField(Fields.jmcg_autonumber_.jmcg_entitytype))));
                }

            }
        }
    }
}
