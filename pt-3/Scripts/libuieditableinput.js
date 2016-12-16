var UI = UI || {};
UI.EditableInput = UI.EditableInput || {};



UI.EditableInput.Create = function (divId, divClass) { 
    $.ajax({
        url: '/content/elements/editableinput.html',
        type: 'get',
        success: function (response) { UI.EditableInput.Bind(divId, divClass, response); }
    });
};

UI.EditableInput.Bind = function (divId, divClass, sHtml) {
    var eHtml = sHtml.replace(/divPref/g, divId);
    $('#' + divId).html(eHtml);
    $('#' + divId + 'inputval').addClass(divClass);
    $('#' + divId).on("click", "[id$='btnno']", function () { UI.EditableInput.Init(divId); });
    $('#' + divId).on("click", "[id$='btnyes']", function () { UI.EditableInput.OkClick(divId); });
    $('#' + divId).on("propertychange change click keyup input paste", "[id$='inputval']", function (e) { UI.EditableInput.OnChange(e, divId); });
    
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

UI.EditableInput.ValidateError = function (divId, errMsg)
{
    $('#' + divId + 'inputval').removeClass('not_error').addClass('error');
    $('#' + divId + 'Error').html(errMsg);
}

UI.EditableInput.OnChange = function (e, divId)
{
    var errMsg = Validate($('#' + divId + 'inputval').val(), $('#' + divId + 'inputval').attr('class').split(" ")[0]);
    if (errMsg.length == 0) {
        $('#' + divId + 'inputval').addClass('not_error').removeClass('error');
        $('#' + divId + 'Error').html(errMsg);

        if (e.which == 13) {
            UI.EditableInput.OkClick(divId);
        }
    } else UI.EditableInput.ValidateError(divId, errMsg);

    
}