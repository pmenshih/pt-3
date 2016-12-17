var UI = UI || {};
UI.EditableCheckbox = UI.EditableCheckbox || {};

UI.EditableCheckbox.Create = function (divId) {
    $.ajax({
        url: '/content/elements/editablecheckbox.html',
        type: 'get',
        success: function (response) { UI.EditableCheckbox.Bind(divId, response); }
    });

    //построение группы чекбоксов
};

UI.EditableCheckbox.Bind = function (divId, sHtml) {
   
    var eHtml = sHtml.replace(/divPref/g, divId);
    $('#' + divId).html(eHtml);  
    $('#' + divId).on("click", "[id$='btnyes']", function () { UI.EditableCheckbox.OkClick(divId); });
    $('#' + divId).on("change", "[id^='sex']", function () { UI.EditableCheckbox.OkClick(divId); });

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
