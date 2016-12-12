var divId = $('.devDiv div div').attr('id');

UI.EditableInput.OkClick = function (divId) {
    //проверка валидатором (если нужна)

    var formData = new FormData();
    var url = divId.replace(/_/g, '/');

    formData.append('val', $('#' + divId + 'inputval').val());
    formData.append('orgId', $.getUrlVar('orgId'));
    formData.append('researchId', $.getUrlVar('researchId'));
    formData.append('divId', divId);

    AjaxCall(url
            , formData
            , UI.EditableInput.ProcessServerAnswer);
};

UI.EditableInput.Create('_research_setpassword');

