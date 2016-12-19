var UI = UI || {};
UI.EditablePass = UI.EditablePass || {};

UI.EditablePass.Create = function (divId, idPass) {
    var jsonData = JSON.parse(idPass);
    var eHtml = '';
    for (var i = 0; i < jsonData.length; i++) {
        eHtml += '<dt><label id=' + jsonData[i].id + 'label" for="' + jsonData[i].id + '">' + jsonData[i].name + '</label></dt>'
                    + '<dd><span class="error-box" id="' + jsonData[i].id + 'Error"></span>'
                    + '<input id="' + jsonData[i].id + '" name="' + jsonData[i].id + '" type="password"></dd>';
    }
    $('#' + divId).html(eHtml);
};

/*
UI.EditablePass.Create = function (divId, idPass) {
    $.ajax({
        url: '/content/elements/passwordchanger.html',
        type: 'get',
        success: function (response) { UI.EditablePass.Bind(divId, idPass, response);}   
    });
   
};

UI.EditablePass.Bind = function (divId, idPass, sHtml) { 
    var jsonData = JSON.parse(idPass);
    var eHtml = '';
    for (var i = 0; i < jsonData.length; i++) {
        eHtml += sHtml.replace(/divPref/g, jsonData[i].id);
    }
    $('#' + divId).html(eHtml);
    for (var i = 0; i < jsonData.length; i++) {    
        $('#' + jsonData[i].id + 'label').text(jsonData[i].name);
    }

};*/

