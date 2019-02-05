/// <reference path="jmcg_an_pageutility.js" />
/// <reference path="jmcg_an_webserviceutility.js" />

autoNumberJs = new Object();

autoNumberJs.RunOnLoad = function () {
    autonumberPageUtility.CommonForm(autoNumberJs.RunOnChange, autoNumberJs.RunOnSave);
    autoNumberJs.PopulateTypeList();
    autoNumberJs.PopulateFieldsList();
    autoNumberJs.RefreshVisibility();
};

autoNumberJs.RunOnChange = function (fieldName) {
    switch (fieldName) {
        case "jmcg_entitytypeselectionfield":
            autoNumberJs.SetEntitySelection();
            break;
        case "jmcg_autonumberfieldselectionfield":
            autoNumberJs.SetFieldSelection();
            break;
        case "jmcg_entitytype":
            autoNumberJs.PopulateFieldsList();
            autoNumberJs.RefreshVisibility();
            break;
    }
};

autoNumberJs.RunOnSave = function () {
};

autoNumberJs.RefreshVisibility = function () {
    var entityTypePopulated = autonumberPageUtility.GetFieldValue("jmcg_entitytype") != null;
    autonumberPageUtility.SetFieldDisabled("jmcg_autonumberfieldselectionfield", !entityTypePopulated);
};

autoNumberJs.EntityTypes = null;
autoNumberJs.PopulateTypeList = function () {
    var compare = function (a, b) {
        if (a.DisplayName < b.DisplayName)
            return -1;
        if (a.DisplayName > b.DisplayName)
            return 1;
        return 0;
    };

    var processResults = function (results) {
        results.sort(compare);
        autoNumberJs.EntityTypes = results;
        var entityOptions = new Array();
        entityOptions.push(new autonumberPageUtility.PicklistOption(0, "Select to change the selected entity type"));
        for (var i = 1; i <= autoNumberJs.EntityTypes.length; i++) {
            entityOptions.push(new autonumberPageUtility.PicklistOption(i, autoNumberJs.EntityTypes[i - 1]["DisplayName"]));
        }
        autonumberPageUtility.SetPicklistOptions("jmcg_entitytypeselectionfield", entityOptions);
        autonumberPageUtility.SetFieldValue("jmcg_entitytypeselectionfield", 0);
    };
    autonumberServiceUtility.GetAllEntityMetadata(processResults);
};

autoNumberJs.SetEntitySelection = function () {
    var selectedoption = Xrm.Page.getAttribute("jmcg_entitytypeselectionfield").getSelectedOption();
    if (selectedoption != null && parseInt(selectedoption.value) != 0) {
        var value = selectedoption.value;
        var selectedEntity = autoNumberJs.EntityTypes[parseInt(value) - 1];
        var selectedEntityName = selectedEntity["LogicalName"];
        autonumberPageUtility.SetFieldValue("jmcg_entitytype", selectedEntityName);
        autonumberPageUtility.SetFieldValue("jmcg_entitytypeselectionfield", 0);
    }
};

autoNumberJs.FieldList = null;
autoNumberJs.PopulateFieldsList = function () {
    var entityType = autonumberPageUtility.GetFieldValue("jmcg_entitytype");
    var processResults = function (results) {
        var newArray = new Array();
        var validTypes = ["String"];
        var ignoreFields = [];
        for (var j = 0; j < results.length; j++) {
            if (autonumberPageUtility.ArrayContains(validTypes, results[j].FieldType)
                && !autonumberPageUtility.ArrayContains(ignoreFields, results[j].LogicalName)
                && results[j].Createable == true) {
                newArray.push(results[j]);
            }
        }
        autoNumberJs.FieldList = newArray;
        var fieldOptions = new Array();
        fieldOptions.push(new autonumberPageUtility.PicklistOption(0, "Select to change the autonumber field"));
        for (var i = 1; i <= autoNumberJs.FieldList.length; i++) {
            fieldOptions.push(new autonumberPageUtility.PicklistOption(i, autoNumberJs.FieldList[i - 1]["DisplayName"]));
        }
        autonumberPageUtility.SetPicklistOptions("jmcg_autonumberfieldselectionfield", fieldOptions);
        autonumberPageUtility.SetFieldValue("jmcg_autonumberfieldselectionfield", 0);
    };
    if (entityType != null) {
        autonumberServiceUtility.GetFieldMetadata(entityType, processResults);
    }
};

autoNumberJs.SetFieldSelection = function () {
    var selectedoption = Xrm.Page.getAttribute("jmcg_autonumberfieldselectionfield").getSelectedOption();
    if (selectedoption != null && parseInt(selectedoption.value) != 0) {
        var value = selectedoption.value;
        var selectedField = autoNumberJs.FieldList[parseInt(value) - 1];
        var selectedFieldName = selectedField["LogicalName"];
        autonumberPageUtility.SetFieldValue("jmcg_autonumberfield", selectedFieldName);
        autonumberPageUtility.SetFieldValue("jmcg_autonumberfieldselectionfield", 0);
    }
};