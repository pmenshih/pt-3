var UI = UI || {};
UI.EditableCheckbox = UI.EditableCheckbox || {};



UI.EditableCheckbox.Create = function (divId) {
    $.ajax({
        url: '/content/elements/editablecheckbox.html',
        type: 'get',
        success: function (response) { UI.EditableCheckbox.Bind(divId, response); }
    });
};

UI.EditableCheckbox.Bind = function (divId, sHtml) {
   
    var eHtml = sHtml.replace(/divPref/g, divId);
    $('#' + divId).html(eHtml);  
    $('#' + divId).on("click", "[id$='btnyes']", function () { UI.EditableCheckbox.OkClick(divId); });
    //$('#val' + divId).nextAll(':radio').on("change", function () { UI.EditableCheckbox.OkClick(divId); });
    

    UI.EditableCheckbox.Init(divId);
};

UI.EditableCheckbox.Init = function (divId) {
    var curVal = $('#val' + divId).val();
    $('#' + divId + 'inputval').val(curVal);   
    $(':radio').each(function () {
        var radioVal = $(this).val();
        if (curVal == radioVal) {
            $(this).prop('checked', true);
        }
    });
    $(':radio').on('change', function () {
        $('#' + divId + 'inputval').val(this.value);
    });
};

UI.EditableCheckbox.OkClick = function (divId) {
    
};

UI.EditableCheckbox.ProcessServerAnswer = function (response, formData) {
    var answer = jQuery.parseJSON(response);
    if (answer.result == '0') {
        var curVal = formData.get('val');
        $('#val' + formData.get('divId')).val(curVal);
        UI.EditableCheckbox.Init(formData.get('divId'));
    }
    else {
        alert(response);
    }
}

UI.EditableCheckbox.ValidateError = function (divId, errMsg) {
    $('#' + divId + 'inputval').removeClass('not_error').addClass('error');
    $('#' + divId + 'Error').html(errMsg);
}

UI.EditableCheckbox.OnChange = function (e, divId) {
    
    var errMsg = Validate($('#' + divId + 'inputval').val(), $('#' + divId + 'inputval').attr('class').split(" ")[0]);
    if (errMsg.length == 0) {
        $('#' + divId + 'inputval').addClass('not_error').removeClass('error');
        $('#' + divId + 'Error').html(errMsg);

        
    } else UI.EditableCheckbox.ValidateError(divId, errMsg);


}
