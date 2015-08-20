using System;
using System.Linq;
using JosephM.Xrm.Autonumber.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Schema;

namespace JosephM.Xrm.Autonumber.Test
{
    [TestClass]
    public class AutonumberTests : JosephMXrmTest
    {
        [TestMethod]
        public void AutonumberConfigurationValidateTest()
        {
            var parentAutonumber = CreateStandardStringAutonumber();

            var parentType = parentAutonumber.GetStringField(Fields.jmcg_autonumber_.jmcg_entitytype);

            //type not int or string
            var autonumber = InitialiseAutonumber("account", "address1_latitude");
            VerifyCreateOrUpdateError(autonumber);
            autonumber.SetField(Fields.jmcg_autonumber_.jmcg_autonumberfield, "numberofemployees");
            //prefix numbers
            autonumber.SetField(Fields.jmcg_autonumber_.jmcg_prefix, "a1A");
            VerifyCreateOrUpdateError(autonumber);
            autonumber.SetField(Fields.jmcg_autonumber_.jmcg_prefix, null);
            //separator number or digit
            autonumber.SetField(Fields.jmcg_autonumber_.jmcg_separator, "1");
            VerifyCreateOrUpdateError(autonumber);
            autonumber.SetField(Fields.jmcg_autonumber_.jmcg_separator, "a");
            VerifyCreateOrUpdateError(autonumber);
            autonumber.SetField(Fields.jmcg_autonumber_.jmcg_separator, null);
            //create as integer
            var integerAutonumber = CreateAndRetrieve(autonumber);

            //cannot change type and field
            integerAutonumber.SetField(Fields.jmcg_autonumber_.jmcg_autonumberfield, "firstname");
            VerifyCreateOrUpdateError(integerAutonumber);
            integerAutonumber.SetField(Fields.jmcg_autonumber_.jmcg_autonumberfield, "numberofemployees");
            //todo jmm  cannot change fields 
            
            //child autonumber tests
            const string childType = "contact";
            const string childField = "firstname";
            const string referenceField = "parentcustomerid";
            DeleteAutonumbersAndRegistrations(childType);
            //initialise valid
            var autonumberEntity = new Entity(Entities.jmcg_autonumber);
            autonumberEntity.SetField(Fields.jmcg_autonumber_.jmcg_entitytype, childType);
            autonumberEntity.SetField(Fields.jmcg_autonumber_.jmcg_autonumberfield, childField);
            autonumberEntity.SetField(Fields.jmcg_autonumber_.jmcg_prefix, "CO");
            autonumberEntity.SetField(Fields.jmcg_autonumber_.jmcg_separator, "-");
            autonumberEntity.SetField(Fields.jmcg_autonumber_.jmcg_numberofnumbercharacters, 3);
            autonumberEntity.SetLookupField(Fields.jmcg_autonumber_.jmcg_parentautonumber, parentAutonumber);
            autonumberEntity.SetField(Fields.jmcg_autonumber_.jmcg_parentautonumberlinks, referenceField + ":" + parentType);

            //separator required
            autonumberEntity.SetField(Fields.jmcg_autonumber_.jmcg_separator, null);
            VerifyCreateOrUpdateError(autonumberEntity);
            autonumberEntity.SetField(Fields.jmcg_autonumber_.jmcg_separator, "-");

            //parent invalid type
            autonumberEntity.SetLookupField(Fields.jmcg_autonumber_.jmcg_parentautonumber, integerAutonumber);
            VerifyCreateOrUpdateError(autonumberEntity);
            autonumberEntity.SetLookupField(Fields.jmcg_autonumber_.jmcg_parentautonumber, parentAutonumber);

            //invalid field type
            autonumberEntity.SetField(Fields.jmcg_autonumber_.jmcg_autonumberfield, "numberofchildren");
            VerifyCreateOrUpdateError(autonumberEntity);
            autonumberEntity.SetField(Fields.jmcg_autonumber_.jmcg_autonumberfield, childField);

            //no links
            autonumberEntity.SetField(Fields.jmcg_autonumber_.jmcg_parentautonumberlinks, null);
            VerifyCreateOrUpdateError(autonumberEntity);
            //invalid links
            autonumberEntity.SetField(Fields.jmcg_autonumber_.jmcg_parentautonumberlinks, "abc");
            VerifyCreateOrUpdateError(autonumberEntity);
            autonumberEntity.SetField(Fields.jmcg_autonumber_.jmcg_parentautonumberlinks, "abc:" + parentType);
            VerifyCreateOrUpdateError(autonumberEntity);
            //valid link
            autonumberEntity.SetField(Fields.jmcg_autonumber_.jmcg_parentautonumberlinks, referenceField + ":" + parentType);
            autonumberEntity = CreateAndRetrieve(autonumberEntity);

            var parent = CreateTestRecord(parentType);
            var child = new Entity(childType);
            child.SetLookupField(referenceField, parent);
            child = CreateAndRetrieve(child);
            var autonValue = child.GetField(childField);
        }

        private Entity CreateStandardStringAutonumber()
        {
            const string type = "account";
            const string field = "accountnumber";

            foreach (var entityType in EntitiesToDelete)
            {
                DeleteAll(entityType);
            }

            DeleteAutonumbersAndRegistrations(type);

            return CreateAutonumber(type, field, "AC", "-", 8);
        }

        [TestMethod]
        public void AutonumberConfigurationCreateRegistrationsTest()
        {
            const string type = "account";
            const string field = "accountnumber";

            foreach (var entityType in EntitiesToDelete)
            {
                DeleteAll(entityType);
            }

            DeleteAutonumbersAndRegistrations(type);

            var autonumber = CreateAutonumber(type, field, "AC", "-", 8);
            var registration = AutonumberService.GetExistingRegistration(type);
            Assert.IsNotNull(registration);

            XrmService.Deactivate(autonumber);
            registration = AutonumberService.GetExistingRegistration(type);
            Assert.IsNull(registration);

            XrmService.Activate(autonumber);
            registration = AutonumberService.GetExistingRegistration(type);
            Assert.IsNotNull(registration);

            XrmService.Delete(autonumber);
            registration = AutonumberService.GetExistingRegistration(type);
            Assert.IsNull(registration);
        }

        [TestMethod]
        public void AutonumberConfigurationSetFieldTypeTest()
        {
            const string type = "account";
            const string field = "accountnumber";

            foreach (var entityType in EntitiesToDelete)
            {
                DeleteAll(entityType);
            }

            DeleteAutonumbersAndRegistrations(type);

            var autonumber = CreateAutonumber(type, field, "AC", "-", 8);
            var xrmAutonumbers = AutonumberService.GetActiveAutonumbersForType(type);
            Assert.AreEqual(OptionSets.Autonumber.AutonumberFieldType.String, xrmAutonumbers.First().AutonumberFieldType);
        }

        public void AutonumberConfigurationPluginActiveInactiveTest()
        {
            const string type = "account";
            const string field = "accountnumber";

            foreach (var entityType in EntitiesToDelete)
            {
                DeleteAll(entityType);
            }

            DeleteAutonumbersAndRegistrations(type);

            var autonumberEntity = CreateAutonumber(type, field, "AC", "-", 8);
            var account = CreateAccount();
            Assert.IsFalse(account.GetStringField("accountnumber").IsNullOrWhiteSpace());

            XrmService.Deactivate(autonumberEntity);
            account = CreateAccount();
            Assert.IsTrue(account.GetStringField("accountnumber").IsNullOrWhiteSpace());

            XrmService.Activate(autonumberEntity);
            account = CreateAccount();
            Assert.IsFalse(account.GetStringField("accountnumber").IsNullOrWhiteSpace());
        }

        [TestMethod]
        public void AutonumberForIntegerTest()
        {
                        //create autonumber
            var intAutonumberEntityType = "account";
            var intAutonumberField = "numberofemployees";
            var intAutonumberEntity = CreateAutonumber(intAutonumberEntityType, intAutonumberField);

            var entity1 = CreateTestRecord(intAutonumberEntityType);
            var entity2 = CreateTestRecord(intAutonumberEntityType);
            var entity3 = CreateTestRecord(intAutonumberEntityType);
            Assert.AreEqual(1, entity1.GetInt(intAutonumberField));
            Assert.AreEqual(2, entity2.GetInt(intAutonumberField));
            Assert.AreEqual(3, entity3.GetInt(intAutonumberField));

            Assert.IsFalse(intAutonumberEntity.GetBoolean(Fields.jmcg_autonumber_.jmcg_overwriteifpopulated));
            var entity = new Entity(intAutonumberEntityType);
            entity.SetField(intAutonumberField, 999999);
            entity = CreateAndRetrieve(entity);
            Assert.AreEqual(999999, entity.GetInt(intAutonumberField));
        }

        [TestMethod]
        public void AutonumberForStringStandardAndChildTest()
        {
            const string type = "account";
            const string field = "accountnumber";

            foreach (var entityType in EntitiesToDelete)
            {
                DeleteAll(entityType);
            }

            DeleteAutonumbersAndRegistrations(type);

            //create autonumber
            var autonumberEntity = CreateAutonumber(type, field, "AC", "-", 8);
            var xrmAutonumber = AutonumberService.GetActiveAutonumbersForType(type).First();
            //verify the regsitration was cerated for the entity type
            var registration = AutonumberService.GetExistingRegistration(type);
            Assert.IsNotNull(registration);
            //verify autonumbers set for new entities
            var account = CreateTestRecord(type);
            //AutonumberService.SetAutonumbers(account);
            Assert.AreEqual("AC-00000001", account.GetField(field));
            account = CreateTestRecord(type);
            Assert.AreEqual("AC-00000002", account.GetField(field));

            //THIS PART CHECK FOR AUTONUMBER OPTION OF OVERWRITE VALUES ALREADY SET
            Assert.IsFalse(xrmAutonumber.OverwriteIfPopulated);
            var testOverwriteAccounts = new Entity(type);
            testOverwriteAccounts.SetField(field, "SETEXPLICIT");
            testOverwriteAccounts = CreateAndRetrieve(testOverwriteAccounts);
            Assert.AreEqual("SETEXPLICIT", testOverwriteAccounts.GetStringField(field));

            autonumberEntity.SetField(Fields.jmcg_autonumber_.jmcg_overwriteifpopulated, true);
            autonumberEntity = UpdateAndRetreive(autonumberEntity, new[] { Fields.jmcg_autonumber_.jmcg_overwriteifpopulated });
            testOverwriteAccounts = new Entity(type);
            testOverwriteAccounts.SetField(field, "SETEXPLICITAGAIN");
            testOverwriteAccounts = CreateAndRetrieve(testOverwriteAccounts);
            Assert.AreEqual("AC-00000003", testOverwriteAccounts.GetField(field));

            //delete all autonumbers and registrations for type
            const string childType = "contact";
            const string childField = "firstname";
            const string referenceField = "parentcustomerid";
            DeleteAutonumbersAndRegistrations(childType);

            //child autonumber
            var childAutonumberEntity = CreateAutonumber(childType, childField, "CO", "-", 3, autonumberEntity,
                referenceField + ":" + type);
            //registered
            registration = AutonumberService.GetExistingRegistration(childType);
            Assert.IsNotNull(registration);
            //number set
            var testEntity = new Entity(childType);
            testEntity.SetLookupField(referenceField, account);
            //var thisautonumber = AutonumberService.GetActiveAutonumbersForType(testEntity.LogicalName).First();
            //thisautonumber.SetAutonumber(testEntity, XrmService, AutonumberService);
            testEntity = CreateAndRetrieve(testEntity);
            Assert.AreEqual("CO-00000002-001", testEntity.GetStringField(childField));
            testEntity = new Entity(childType);
            testEntity.SetLookupField(referenceField, account);
            testEntity = CreateAndRetrieve(testEntity);
            Assert.AreEqual("CO-00000002-002", testEntity.GetStringField(childField));

            XrmService.Delete(childAutonumberEntity);
            Assert.IsNull(AutonumberService.GetExistingRegistration(childType));

            DeleteMyToday();
        }

        private void DeleteAutonumbersAndRegistrations(string type)
        {
            //delete all autonumbers and registrations for type
            foreach (var autonumber in AutonumberService.GetAutonumbersForType(type, true))
            {
                XrmService.Delete(Entities.jmcg_autonumber, autonumber.AutonumberId);
            }

            while (AutonumberService.GetExistingRegistration(type) != null)
            {
                Delete(AutonumberService.GetExistingRegistration(type));
            }
        }

        private Entity CreateAutonumber(string type, string field)
        {
            return CreateAutonumber(type, field, null, null, null, null, null);
        }

        private Entity CreateAutonumber(string type, string field, string prefix, string separator, int numberCharacters)
        {
            return CreateAutonumber(type, field, prefix, separator, numberCharacters, null, null);
        }

        private Entity CreateAutonumber(string type, string field, string prefix, string separator, int? numberCharacters, Entity parentAutonumber, string linksToParent)
        {
            var autonumberEntity = InitialiseAutonumber(type, field, prefix, separator, numberCharacters, parentAutonumber, linksToParent);
            autonumberEntity = CreateAndRetrieve(autonumberEntity);
            return autonumberEntity;
        }

        private static Entity InitialiseAutonumber(string type, string field)
        {
            return InitialiseAutonumber(type, field, null, null, null, null, null);
        }

        private static Entity InitialiseAutonumber(string type, string field, string prefix, string separator,
            int? numberCharacters, Entity parentAutonumber, string linksToParent)
        {
            var autonumberEntity = new Entity(Entities.jmcg_autonumber);
            autonumberEntity.SetField(Fields.jmcg_autonumber_.jmcg_entitytype, type);
            autonumberEntity.SetField(Fields.jmcg_autonumber_.jmcg_autonumberfield, field);
            autonumberEntity.SetField(Fields.jmcg_autonumber_.jmcg_prefix, prefix);
            autonumberEntity.SetField(Fields.jmcg_autonumber_.jmcg_separator, separator);
            autonumberEntity.SetField(Fields.jmcg_autonumber_.jmcg_numberofnumbercharacters, numberCharacters);
            if (parentAutonumber != null)
            {
                autonumberEntity.SetLookupField(Fields.jmcg_autonumber_.jmcg_parentautonumber, parentAutonumber);
                autonumberEntity.SetField(Fields.jmcg_autonumber_.jmcg_parentautonumberlinks, linksToParent);
            }
            return autonumberEntity;
        }
    }
}
