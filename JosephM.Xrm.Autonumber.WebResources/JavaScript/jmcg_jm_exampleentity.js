jmExampleEntity = new Object();

jmExampleEntity.RunOnLoad = function () {
    josephMPageUtility.CommonForm(jmExampleEntity.RunOnChange, jmExampleEntity.RunOnSave);
}

jmExampleEntity.RunOnChange = function (fieldName) {
    switch (fieldName) {
        case "fieldname":
            break;
    }
}

jmExampleEntity.RunOnSave = function () {
}