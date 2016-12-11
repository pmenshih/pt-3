$.extend({
    getUrlVars: function () {
        var vars = [], hash;
        var hashes = window.location.href.slice(window.location.href.indexOf('?') + 1).split('&');
        for (var i = 0; i < hashes.length; i++) {
            hash = hashes[i].split('=');
            vars.push(hash[0]);
            vars[hash[0]] = hash[1];
        }
        return vars;
    },
    getUrlVar: function (name) {
        return $.getUrlVars()[name];
    }
});

var editBlockHtml = "<input autofocus type='text' id='textValueForAjaxChange'></input>\
<span class='devSpanLnk' onclick='Save(\"parentId\")'>save</span>\
&nbsp;&nbsp;&nbsp;<span class='devSpanLnk' onclick='Initialize(\"parentId\")'>cancel</span>";
var startBlockHtml = "<span onClick='ShowEditBlock(\"parentId\")' class='devSpanLnk'>задать</span>";
var a = 'pref';
var b = 'val';
var c = 'textValueForAjaxChange';

$("[id^='" + a + "']").each(function () {
    parentId = $(this).attr('id');
    Initialize(parentId);
})

function Initialize(parentId)
{
    var curVal = $('#'+parentId.replace(a, b)).val();
    $('#' + parentId).empty();
    if (!curVal) $('#' + parentId).html(startBlockHtml.replace('parentId', parentId));
    else $('#' + parentId).append(curVal + startBlockHtml
            .replace('parentId', parentId)
            .replace('задать', 'изменить'));
}

function ShowEditBlock(parentId)
{
    $('#' + parentId).html(editBlockHtml.replace(/parentId/g, parentId));
    var curVal = $('#'+parentId.replace(a, b)).val();
    $('#' + c).val(curVal);
}

function Save(parentId) {
    var formData = new FormData();
    var url = parentId.replace(a, '').replace(/_/g, '/');
    formData.append('val', $('#' + c).val());
    formData.append('orgId', $.getUrlVar('orgId'));
    formData.append('researchId', $.getUrlVar('researchId'));
    $.ajax({
        url: url,
        dataType: 'text',
        cache: false,
        contentType: false,
        processData: false,
        data: formData,
        type: 'post',
        success: function (response) {
            var answer = jQuery.parseJSON(response);
            if (answer.result == '0') {
                var curVal = formData.get('val');
                $('#' + parentId.replace(a, b)).val(curVal);
                Initialize(parentId);
            }
            else {
                alert('указанное кодовое слово не является уникальным');
            }
        }
    });
}