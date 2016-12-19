var divId = $('dd div').attr('id');

UI.EditableInput.OkClick = function (divId) {
    if ($('#' + divId + 'Error').html().length != 0) return;

    var formData = new FormData();
    var url = divId.replace(/_/g, '/');

    formData.append('val', $('#' + divId + 'inputval').val());
    formData.append('newval', formData.get('val'));
    formData.append('userId', $('#userId').val());
    formData.append('divId', divId);

    AjaxCall(url
            , formData
            , UI.EditableInput.ProcessServerAnswer);
};

UI.EditableRadio.OkClick = function (divId) {
    var formData = new FormData();
    var url = divId.replace(/_/g, '/');  
    formData.append('val', $('#' + divId + 'inputval').val());
    formData.append('newval', formData.get('val'));
    formData.append('userId', $('#userId').val());
    formData.append('divId', divId);

    AjaxCall(url
            , formData
            , UI.EditableInput.ProcessServerAnswer);
};
  
UI.EditableInput.Create('_manage_surnamechange', 'text');
UI.EditableInput.Create('_manage_namechange', 'text');
UI.EditableInput.Create('_manage_patronimchange', 'text');
UI.EditableInput.Create('_manage_emailchange', 'email');
UI.EditableInput.Create('_manage_phonechange', 'phone');
UI.EditableRadio.Create('_manage_sexchange', '[{"value":"0","name":"Женский"},{"value":"1","name":"Мужской"}]');

var orgsListJson;
AjaxCall("/organisation/listall", null, function (response, pars) {
    orgsListJson = response;
});

function GetArrayOfOrgsSuggestions(term)
{
    var result = [];
    var orgs = jQuery.parseJSON(orgsListJson);
    for (var i = 0; i < orgs.length; i++)
    {
        if (orgs[i].name.toLowerCase().indexOf(term) >= 0)
            result.push(orgs[i]);
    }
    return result;
}

$('#orgName').autoComplete({
    jsonp: "callback",
    dataType: "jsonp",
    minChars: 1,
    source: function (term, response) {
        term = term.toLowerCase();
        var suggestions = GetArrayOfOrgsSuggestions(term);
        response(suggestions);
    },
    renderItem: function (item, search) {
        search = search.replace(/[-\/\\^$*+?.()|[\]{}]/g, '\\$&');
        var re = new RegExp("(" + search.split(' ').join('|') + ")", "gi");
        return '<div class="autocomplete-suggestion" data-id="' + item.id + '" data-val="' + item.name + '">' + item.name.replace(re, "<b>$1</b>") + '</div>';
    },
    onSelect: function (e, term, item) {
        var orgId = $('.autocomplete-suggestion').data('id');
        $('#orgId').val(orgId);      
    }
});


$("#formOrg form").submit(function (divId) {

    divId.preventDefault();
    var formData = new FormData();
    var url = '/organisation/joinrequest';   
    formData.append('orgId', $('#orgId').val());
    formData.append('userEmail', $('#userEmail').val());

    AjaxCall(url
            , formData
            , orgSubmit.ProcessServerAnswer);   
   
});

orgSubmit.ProcessServerAnswer = function (response, formData) {
    var answer = jQuery.parseJSON(response);
    if (answer.result == '0') {
        $('#formOrg form').hide();
        $('#formOrg .hidden').show();
        $('#formOrg .hidden').text('Ваш запрос на присоединение к организации ' + $('#orgName').val() + ' отправлен. Ожидайте ответа от менеджера.');
    }
    else if (answer.result == '2') {
        $('#formOrg form').hide();
        $('#formOrg .hidden').show();
        $('#formOrg .hidden').text('Вы уже сделали запрос на присоединение.');
    }
    else {
        alert(response);
    }
}



// /organisation/joinrequest?orgId=[val]&userEmail=[val]