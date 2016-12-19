var UI = UI || {};
UI.EditableRadio = UI.EditableRadio || {};

UI.EditableRadio.Create = function (divId, radioList) {
    $.ajax({
        url: '/content/elements/editablecheckbox.html',
        type: 'get',
        success: function (response) { UI.EditableRadio.Bind(divId, response); }
    }); 

    //построение группы чекбоксов
    var jsonData = JSON.parse(radioList);
    var rHtml = '';
    for (var i = 0; i < jsonData.length; i++) {
        rHtml += '<input type="radio" name="radio_' + divId + '"  id="radio_' + divId  + i + '" value="' + jsonData[i].value + '" /> <label>' + jsonData[i].name + '</label>';
    }
    $('#radiolist').html(rHtml);
};

UI.EditableRadio.Bind = function (divId, sHtml) {
   
    var eHtml = sHtml.replace(/divPref/g, divId);
    $('#' + divId).html(eHtml);  
    $('#' + divId).on("click", "[id$='btnyes']", function () { UI.EditableRadio.OkClick(divId); });
    UI.EditableRadio.Init(divId);
   
};

UI.EditableRadio.Init = function (divId) {
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
        UI.EditableRadio.OkClick(divId);
    });   
};

UI.EditableRadio.OkClick = function (divId) {
    
};

UI.EditableRadio.ProcessServerAnswer = function (response, formData) {
    var answer = jQuery.parseJSON(response);
    if (answer.result == '0') {
        var curVal = formData.get('val');
        $('#val' + formData.get('divId')).val(curVal);
        UI.EditableRadio.Init(formData.get('divId'));
    }
    else {
        alert(response);
    }
}
