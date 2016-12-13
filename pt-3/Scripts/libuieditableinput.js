var UI = UI || {};
UI.EditableInput = UI.EditableInput || {};

UI.EditableInput.Create = function (divId) {
    $.ajax({
        url: '/content/elements/editableinput.html',
        type: 'get',
        success: function (response) { UI.EditableInput.Bind(divId, response); }
    });
};

UI.EditableInput.Bind = function (divId, sHtml) {
    var eHtml = sHtml.replace(/divPref/g, divId);

    $('#' + divId).html(eHtml);
    $('#' + divId).on("click", "[id$='btnno']", function () { UI.EditableInput.Init(divId); });
    $('#' + divId).on("click", "[id$='btnyes']", function () { UI.EditableInput.OkClick(divId); });

    UI.EditableInput.Init(divId);
};

UI.EditableInput.Init = function (divId) {
    var curVal = $('#val' + divId).val();
    $('#' + divId + 'inputval').val(curVal);
};

UI.EditableInput.OkClick = function (divId) {
};

UI.EditableInput.ProcessServerAnswer = function (response, formData) {
    var answer = jQuery.parseJSON(response);
    if (answer.result == '0') {
        var curVal = formData.get('val');
        $('#val' + formData.get('divId')).val(curVal);
        UI.EditableInput.Init(formData.get('divId'));
    }
    else {
        alert(response);
    }
}