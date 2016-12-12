UI.EditableInput.OkClick = function (divId) {
    divId = '_manage_surnamechange';
    var formData = new FormData();
    var url = divId.replace(/_/g, '/');

    formData.append('newval', $('#' + divId + 'inputval').val());
    formData.append('divId', divId);
    formData.append('userId', $('#userId').val());

    AjaxCall(url
            , formData
            , UI.EditableInput.ProcessServerAnswer);
};

UI.EditableInput.Create('_manage_surnamechange');
