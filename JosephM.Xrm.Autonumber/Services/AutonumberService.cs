using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Schema;

namespace JosephM.Xrm.Autonumber.Services
{
    public class AutonumberService
    {
        public XrmService XrmService { get; set; }

        public AutonumberService(XrmService xrmService)
        {
            XrmService = xrmService;
        }

        public void SetAutonumbers(Entity entity)
        {
            var autonumbers = GetActiveAutonumbersForType(entity.LogicalName);
            foreach (var autonumber in autonumbers)
            {
                if (autonumber.OverwriteIfPopulated
                    || !entity.Contains(autonumber.AutonumberField))
                    SetAutonumber(entity, autonumber);
            }
        }

        public IEnumerable<XrmAutonumber> GetActiveAutonumbersForType(string entityType)
        {
            return GetAutonumbersForType(entityType, false);
        }

        private static string ParentAlias
        {
            get { return "PARENT"; }
        }

        public IEnumerable<XrmAutonumber> GetAutonumbersForType(string entityType, bool includeInactive)
        {
            var conditions = new List<ConditionExpression>();
            conditions.Add(new ConditionExpression(Fields.jmcg_autonumber_.jmcg_entitytype, ConditionOperator.Equal, entityType));
            if (!includeInactive)
                conditions.Add(new ConditionExpression(Fields.jmcg_autonumber_.statecode, ConditionOperator.Equal,
                    XrmPicklists.State.Active));

            return GetAutonumbers(conditions);
        }

        private IEnumerable<XrmAutonumber> GetAutonumbers(IEnumerable<ConditionExpression> conditions)
        {
            var query = new QueryExpression(Entities.jmcg_autonumber);
            query.ColumnSet = new ColumnSet(true);
            if (conditions != null)
            {
                foreach (var item in conditions)
                    query.Criteria.AddCondition(item);
            }

            var link = query.AddLink(Entities.jmcg_autonumber, Fields.jmcg_autonumber_.jmcg_parentautonumber,
                Fields.jmcg_autonumber_.jmcg_autonumberid);
            link.JoinOperator = JoinOperator.LeftOuter;
            link.EntityAlias = ParentAlias;
            link.Columns = new ColumnSet(true);
            return XrmService.RetrieveAll(query)
                .Select(e => new XrmAutonumber(e))
                .ToArray();
        }

        public class XrmAutonumber
        {
            private Entity AutonumberEntity { get; set; }

            public XrmAutonumber(Entity autonumberEntity)
            {
                AutonumberEntity = autonumberEntity;
            }

            public bool OverwriteIfPopulated
            {
                get { return AutonumberEntity.GetBoolean(Fields.jmcg_autonumber_.jmcg_overwriteifpopulated); }
            }

            public string AutonumberField
            {
                get { return AutonumberEntity.GetStringField(Fields.jmcg_autonumber_.jmcg_autonumberfield); }
            }


            public string FirstLinkLookup
            {
                get { return FirstLink.LinkFieldSource; }
            }

            public LinkToParent FirstLink
            {
                get
                {
                    if (LinksToParent.LinkToParents.Any())
                        return LinksToParent.LinkToParents.First();
                    throw new NullReferenceException("There are no linked to parent in the autonumber");
                }
            }

            public LinkToParent LastLink
            {
                get
                {
                    if (LinksToParent.LinkToParents.Any())
                        return LinksToParent.LinkToParents.Last();
                    throw new NullReferenceException("There are no linked to parent in the autonumber");
                }
            }


            public string LastLinkTarget
            {
                get { return LastLink.LinkTarget; }
            }

            private LinksToParent _linksToParent;
            public LinksToParent LinksToParent
            {
                get
                {
                   if(_linksToParent == null)
                        _linksToParent =
                            new LinksToParent(AutonumberEntity.GetStringField(Fields.jmcg_autonumber_.jmcg_parentautonumberlinks));
                    return _linksToParent;
                    ;
                }
            }

            public bool HasParent
            {
                get { return AutonumberEntity.GetLookupGuid(Fields.jmcg_autonumber_.jmcg_parentautonumber).HasValue; }
            }

            public string ParentAutonumberField
            {
                get
                {
                    return
                        (string)
                            AutonumberEntity.GetFieldValue(string.Format("{0}.{1}", ParentAlias,
                                Fields.jmcg_autonumber_.jmcg_autonumberfield));
                }
            }

            public Guid AutonumberId { get { return AutonumberEntity.Id; } }

            public string Separator
            {
                get { return AutonumberEntity.GetStringField(Fields.jmcg_autonumber_.jmcg_separator); }
            }

            public string ParentSeparator
                            {
                get
                {
                    return
                        (string)
                            AutonumberEntity.GetFieldValue(string.Format("{0}.{1}", ParentAlias,
                                Fields.jmcg_autonumber_.jmcg_separator));
                }
            }


            public string AutonumberPrefix
            {
                get { return AutonumberEntity.GetStringField(Fields.jmcg_autonumber_.jmcg_prefix); }
            }

            public string EntityType
            {
                get { return AutonumberEntity.GetStringField(Fields.jmcg_autonumber_.jmcg_entitytype); }
            }

            public int AutonumberCharacters
            {
                get { return AutonumberEntity.GetInt(Fields.jmcg_autonumber_.jmcg_numberofnumbercharacters); }
            }

            public int AutonumberFieldType
            {
                get { return AutonumberEntity.GetOptionSetValue(Fields.jmcg_autonumber_.jmcg_autonumberfieldtype); }
            }
        }

        public void SetAutonumbers(XrmEntityPlugin plugin)
        {
            if (plugin.IsMessage(PluginMessage.Create) && plugin.IsStage(PluginStage.PreOperationEvent))
            {
                SetAutonumbers(plugin.TargetEntity);
            }
        }

        public Entity GetPluginType()
        {
            var entity = XrmService.GetFirst("plugintype", "typename", PluginQualifiedName);
            if (entity == null)
                throw new NullReferenceException(string.Format("No {0} Exists With {1} = {2}",
                    XrmService.GetEntityLabel("plugintype"), XrmService.GetFieldLabel("typename", "plugintype"),
                    PluginQualifiedName));
            return entity;
        }


        public Entity GetPluginMessage()
        {
            return XrmService.GetFirst("sdkmessage", "name", PluginMessage.Create);
        }

        public Entity GetPluginFilter(string entityType)
        {
            var pluginFilters = XrmService.RetrieveAllAndClauses("sdkmessagefilter", new[]
            {
                new ConditionExpression("primaryobjecttypecode", ConditionOperator.Equal,
                    XrmService.GetObjectTypeCode(entityType)),
                new ConditionExpression("sdkmessageid", ConditionOperator.Equal, GetPluginMessage().Id)
            });

            if (pluginFilters.Count() != 1)
                throw new InvalidPluginExecutionException(string.Format(
                    "Error Getting {0} for {1} {2} and type {3}",
                    XrmService.GetEntityLabel("sdkmessagefilter"), XrmService.GetEntityLabel("sdkmessage"), PluginMessage.Create,
                    XrmService.GetEntityLabel(entityType)));
            return pluginFilters.First();
        }

        public Entity GetExistingRegistration(string entityType)
        {
            var existingRegistration = XrmService.RetrieveAllAndClauses("sdkmessageprocessingstep", new[]
            {
                new ConditionExpression("sdkmessagefilterid", ConditionOperator.Equal, GetPluginFilter(entityType).Id),
                new ConditionExpression("plugintypeid", ConditionOperator.Equal, GetPluginType().Id),
                new ConditionExpression("stage", ConditionOperator.Equal, PluginStage.PreOperationEvent)
            });
            return existingRegistration.Any() ? existingRegistration.First() : null;
        }

        /// <summary>
        ///     Return the entity record which is configured as the logical parent of this records autonumber
        /// </summary>
        public Entity GetParentEntity(Entity entity, XrmAutonumber autonumber, IEnumerable<string> fields)
        {
            //Need to split the links to the parent and create a query which returns the parent record via the links
            if (!entity.Contains(autonumber.FirstLinkLookup))
                return null;

            //Create a query which traverses through the keys and entities we defined in our autonumber links
            var query = new QueryExpression();
            query.ColumnSet = XrmService.CreateColumnSet(fields);
            LinkEntity carry = null;
            //for each foreign key entity pairing work from last to first
            for (var i = autonumber.LinksToParent.LinkToParents.Count(); i > 0; i--)
            {
                var thisLink = autonumber.LinksToParent.LinkToParents.ElementAt(i - 1);
                //if this is the last item we need to create it as the type of entity we are returning
                if (i == autonumber.LinksToParent.LinkToParents.Count())
                    query.EntityName = thisLink.LinkTarget;
                //otherwise if this is not the last item we need to add a link from the previous type to this type
                else
                {
                    var previousPair = autonumber.LinksToParent.LinkToParents.ElementAt(i);
                    if (carry == null)
                        carry = query.AddLink(thisLink.LinkTarget, previousPair.LinkFieldTarget, previousPair.LinkFieldSource);
                    else
                        carry = carry.AddLink(thisLink.LinkTarget, previousPair.LinkFieldTarget, previousPair.LinkFieldSource);
                }
                //if this is the first item we need to add a filter on the first id with the value in the lookup from the record we are creating the autonumber for
                if (i == 1)
                {
                    var thisLookupId = entity.GetLookupGuid(thisLink.LinkFieldSource);
                    if (!thisLookupId.HasValue)
                        return null;
                    if (autonumber.LinksToParent.LinkToParents.Count() != 1)
                        carry.LinkCriteria.AddCondition(carry.LinkToEntityName + "id", ConditionOperator.Equal, thisLookupId.Value);
                    else
                        query.Criteria.AddCondition(query.EntityName + "id", ConditionOperator.Equal, thisLookupId.Value);
                }
            }
            //Run the query and if a result return it
            var parent = XrmService.RetrieveMultiple(query);
            if (parent.Entities.Count > 0)
                return parent.Entities[0];
            return null;
        }

        /// <summary>
        ///     Sets the autonumber fields configured for this record type with the next logical value
        /// </summary>
        public void SetAutonumber(Entity entity, XrmAutonumber autonumber)
        {
            //lock the autonumber record for the current plugin
            XrmService.SetField(Entities.jmcg_autonumber, autonumber.AutonumberId, Fields.jmcg_autonumber_.modifiedon, DateTime.UtcNow);
            if (autonumber.HasParent)
            {
                SetParentedAutonumber(entity, autonumber);
            }
            else
            {
                //for child autonumber need to refresh the autonumber record for the current position
                SetIndividualAutonumber(entity, autonumber);
            }
        }

        /// <summary>
        ///     Gets the previous child autonumber of this records parent then sets this records autonumber to the next logical
        ///     value
        /// </summary>
        private void SetParentedAutonumber(Entity entity, XrmAutonumber autonumber)
        {
            var parentRecord = GetParentEntity(entity, autonumber, new[] { autonumber.ParentAutonumberField });
            if (parentRecord != null && parentRecord.Contains(autonumber.ParentAutonumberField))
            {
                var maxId = 0;

                var parentNumber = GetNumberPartFromParent(parentRecord, autonumber);

                var parentPrefix = autonumber.AutonumberPrefix + autonumber.ParentSeparator + parentNumber + autonumber.Separator;

                var conditions = new[]
                {
                    new ConditionExpression(autonumber.AutonumberField, ConditionOperator.BeginsWith,
                        parentPrefix)
                };
                var getHighestExistingQuery = XrmService.BuildQueryActive(autonumber.EntityType,
                    new[] { autonumber.AutonumberField },
                    conditions, null);
                getHighestExistingQuery.AddOrder(autonumber.AutonumberField, OrderType.Descending);
                var highestExisting = XrmService.RetrieveFirst(getHighestExistingQuery);

                if (highestExisting != null)
                {
                    var id = highestExisting.GetStringField(autonumber.AutonumberField);
                    maxId = int.Parse(id.Substring(id.LastIndexOf(autonumber.Separator) + 1));
                }

                if ((maxId + 1).ToString().Length > autonumber.AutonumberCharacters)
                {
                    //if the next autonumber length is greater than the configured length then we cannot rely on a string sort (e.g. 999>1000)
                    //in this case we need to get all, parse into an int and then sort descending
                    var query = new QueryExpression(entity.LogicalName);
                    query.ColumnSet.AddColumn(autonumber.AutonumberField);
                    query.Criteria.AddCondition(autonumber.AutonumberField, ConditionOperator.BeginsWith,
                        autonumber.AutonumberPrefix + autonumber.ParentSeparator + parentNumber + autonumber.Separator);
                    var allSameParentIds = XrmService.RetrieveAll(query);
                    var autonumbers = new List<int>();
                    foreach (var item in allSameParentIds)
                    {
                        var id = item.GetStringField(autonumber.AutonumberField);
                        autonumbers.Add(int.Parse(id.Substring(id.LastIndexOf(autonumber.Separator) + 1)));
                    }
                    if (autonumbers.Count > 0)
                    {
                        autonumbers.Sort();
                        maxId = autonumbers.Last();
                    }
                }
                var newId = autonumber.AutonumberPrefix + autonumber.ParentSeparator + parentNumber + autonumber.Separator +
                            (maxId + 1).ToString("D" + autonumber.AutonumberCharacters);
                entity.SetField(autonumber.AutonumberField, newId);
            }
            else
            {
                throw new InvalidPluginExecutionException(
                    "Autonumber error - this records autonumber is configured to include the parent records autonumber but no parent autonumber value was found");
            }
        }

        /// <summary>
        ///     Sets this records autonumber field and increments the counter in the autonumber configuration
        /// </summary>
        private void SetIndividualAutonumber(Entity entity, XrmAutonumber autonumber)
        {
            var currentPosition = (int?)
                    XrmService.LookupField(Entities.jmcg_autonumber, autonumber.AutonumberId,
                        Fields.jmcg_autonumber_.jmcg_currentnumberposition);
            currentPosition = FormatAndSetAutonumber(entity, currentPosition, autonumber.AutonumberCharacters, autonumber.AutonumberPrefix, autonumber.Separator, autonumber);
            XrmService.SetField(Entities.jmcg_autonumber, autonumber.AutonumberId,
                Fields.jmcg_autonumber_.jmcg_currentnumberposition, currentPosition);
            //var useAutonumber = !PrefixFromParent;
            //if (PrefixFromParent)
            //{
            //    //need to get the parent records prefix
            //    var parentRecord = GetParentEntity(entity, service);
            //    if (parentRecord == null && IfNotLinkedUseAutonumberConfig)
            //        useAutonumber = true;
            //    else
            //    {
            //        if (parentRecord == null)
            //            throw new NullReferenceException(
            //                string.Format(
            //                    "Error Generating {0}. The {0} For Field {1} Is Configured To Autonumber From A Parent Record But The Links To That Parent Did Not Return A Record"
            //                    , "Autonumber", service.GetFieldLabel(AutonumberFieldName, EntityType)));
            //        //lock for the autonumber
            //        service.SetField(parentRecord.LogicalName, parentRecord.Id, "modifiedon", DateTime.Now);
            //        parentRecord = GetParentEntity(entity, service);
            //        //then get the current max
            //        var prefix = !ParentPrefixField.IsNullOrWhiteSpace() &&
            //                     !parentRecord.GetStringField(ParentPrefixField).IsNullOrWhiteSpace()
            //            ? parentRecord.GetStringField(ParentPrefixField)
            //            : AutonumberPrefix;
            //        var separator = !ParentSeparatorField.IsNullOrWhiteSpace() &&
            //                        !parentRecord.GetStringField(ParentSeparatorField).IsNullOrWhiteSpace()
            //            ? parentRecord.GetStringField(ParentSeparatorField)
            //            : Separator;
            //        var numberOfCharacters = !ParentNumberOfCharactersField.IsNullOrWhiteSpace() &&
            //                                 parentRecord.GetField(ParentPrefixField) != null
            //            ? parentRecord.GetField(ParentNumberOfCharactersField)
            //            : AutonumberCharacters;
            //        if (numberOfCharacters == null)
            //            numberOfCharacters = AutonumberCharacters;
            //        if (ParentCurrentPositionField.IsNullOrWhiteSpace())
            //            throw new NullReferenceException(string.Format(
            //                "Error Generating {0}. The {0} For Field {1} Is Configured To Autonumber From A Parent Record But {2} Is Empty On The {0} Record"
            //                , "Autonumber", service.GetFieldLabel(AutonumberFieldName, EntityType),
            //                service.GetFieldLabel(AutonumberFields.ParentCurrentPositionField,
            //                    AutonumberFields.AutonumberEntityName)));
            //        var currentPosition = (int?)parentRecord.GetField(ParentCurrentPositionField);
            //        currentPosition = FormatAndSetAutonumber(entity, currentPosition, (int)numberOfCharacters, prefix,
            //            separator);
            //        parentRecord.SetField(ParentCurrentPositionField, currentPosition);
            //        service.Update(parentRecord, new[] { ParentCurrentPositionField });
            //    }
            //}
            //if (useAutonumber)
            //{
            //    var prefix = AutonumberPrefix;
            //    var separator = Separator;
            //    var numberOfCharacters = AutonumberCharacters;
            //    var currentPosition = (int?)
            //            service.LookupField(autonumberFields.AutonumberEntityName, AutonumberId,
            //                autonumberFields.AutonumberEntityCurrentPositionField);
            //    currentPosition = FormatAndSetAutonumber(entity, currentPosition, numberOfCharacters, prefix, separator);
            //    service.SetField(autonumberFields.AutonumberEntityName, AutonumberId,
            //        autonumberFields.AutonumberEntityCurrentPositionField, currentPosition);
            //}
        }

        private int FormatAndSetAutonumber(Entity entity, int? currentPosition, int numberOfCharacters, string prefix,
            string separator, XrmAutonumber autonumber)
        {
            if (!currentPosition.HasValue)
                currentPosition = 1;
            else
                currentPosition = currentPosition.Value + 1;
            object newValue = null;
            if (autonumber.AutonumberFieldType == OptionSets.Autonumber.AutonumberFieldType.String)
            {
                string autonumberString;
                if (numberOfCharacters > 0)
                    autonumberString = prefix + separator +
                                       currentPosition.Value.ToString("D" + numberOfCharacters);
                else
                    autonumberString = prefix + separator + currentPosition;
                newValue = autonumberString;
            }
            else
            {
                newValue = currentPosition;
            }
            entity.SetField(autonumber.AutonumberField, newValue);
            return currentPosition.Value;
        }

        public string GetNumberPartFromParent(Entity parent, XrmAutonumber autonumber)
        {
            var temp = parent.GetStringField(autonumber.ParentAutonumberField);
            if (string.IsNullOrWhiteSpace(temp))
                throw new NullReferenceException("Error The Parent Autonumber Is empty");

            var startIndex = temp.IndexOfAny(new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' });
            return temp.Substring(startIndex);
        }

        public string GetNumberPartFromThisType(Entity entity, XrmAutonumber autonumber)
        {
            var temp = entity.GetStringField(autonumber.AutonumberField);
            if (string.IsNullOrWhiteSpace(temp))
                throw new NullReferenceException("Error The Parent Autonumber Is empty");

            var startIndex = temp.IndexOfAny(new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' });
            return temp.Substring(startIndex);
        }

        public class LinksToParent
        {
            public LinksToParent(string linksToParent)
            {
                var linksList = new List<LinkToParent>();
                var links = linksToParent.Split(';');
                //for each foreign key entity pairing work from last to first
                for (var i = 0; i < links.Length; i++)
                {
                    var thisPair = links[i].Split(':');
                    if (thisPair.Length == 2)
                        linksList.Add(new LinkToParent(thisPair[0], thisPair[1] + "id", thisPair[1]));
                    else if (thisPair.Length == 3)
                        linksList.Add(new LinkToParent(thisPair[0], thisPair[1], thisPair[2]));
                    else
                        throw new ArgumentException("Error Parsing Links To Parent For Autonumber. Value Is Required To Contain Colon Separated Pairs Each Of Length 2 Or 3 (e.g. accountid:account or account:accountid:account", "linksToParent");
                }
                LinkToParents = linksList;
            }

            public IEnumerable<LinkToParent> LinkToParents { get; set; }
        }

        public class LinkToParent
        {
            public string LinkFieldSource { get; set; }
            public string LinkFieldTarget { get; set; }
            public string LinkTarget { get; set; }

            public LinkToParent(string linkFieldSource, string linkFieldTarget, string linkTarget)
            {
                LinkFieldSource = linkFieldSource;
                LinkFieldTarget = linkFieldTarget;
                LinkTarget = linkTarget;
            }
        }

        public void RefreshPluginRegistrations(string entityType)
        {
            var autonumbers = GetActiveAutonumbersForType(entityType);
            if (!autonumbers.Any())
            {
                while (true)
                {
                    var registration = GetExistingRegistration(entityType);
                    if (registration == null)
                        break;

                    XrmService.Delete(registration);
                }
            }
            else
            {
                if (GetExistingRegistration(entityType) == null)
                {
                    var newAutonumber = new Entity("sdkmessageprocessingstep");
                    newAutonumber.SetLookupField("plugintypeid", GetPluginType());
                    newAutonumber.SetOptionSetField("stage", PluginStage.PreOperationEvent);
                    newAutonumber.SetLookupField("sdkmessagefilterid", GetPluginFilter(entityType));
                    newAutonumber.SetLookupField("sdkmessageid", GetPluginMessage());
                    newAutonumber.SetField("rank", 1);
                    newAutonumber.SetField("name", string.Format("Autonumber For {0}", XrmService.GetEntityLabel(entityType)));
                    XrmService.Create(newAutonumber);
                }
            }
        }

        public string PluginQualifiedName { get { return PluginTypes.XrmAutonumberPluginRegistration; } }

        public void ValidateLinksToParent(XrmAutonumber autonumber)
        {
            try
            {
                var dummyEntity = new Entity(autonumber.EntityType);
                var firstLookup = autonumber.FirstLinkLookup;
                if (!XrmService.FieldExists(firstLookup, autonumber.EntityType))
                    throw new NullReferenceException(string.Format("There Is No Field Named {0} On The {1} Record Type", firstLookup, autonumber.EntityType));
                if (XrmService.IsLookup(firstLookup, autonumber.EntityType))
                {
                    dummyEntity.SetLookupField(firstLookup, Guid.Empty, autonumber.FirstLinkLookup);
                    GetParentEntity(dummyEntity, autonumber, new string[0]);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("There Was An Error Validating The {0} Field - {1}", XrmService.GetFieldLabel(Fields.jmcg_autonumber_.jmcg_parentautonumberlinks, Entities.jmcg_autonumber), ex.Message), ex);
            }
        }

        public XrmAutonumber GetAutonumber(Guid autonumberId)
        {
            var conditions = new[]
            {
                new ConditionExpression(Fields.jmcg_autonumber_.jmcg_autonumberid, ConditionOperator.Equal, autonumberId)
            };
            var autonumbers = GetAutonumbers(conditions);
            if (!autonumbers.Any())
                throw new NullReferenceException(string.Format("Error Leading Autonumber For Id {0}", autonumberId));
            return autonumbers.First();
        }
    }
}