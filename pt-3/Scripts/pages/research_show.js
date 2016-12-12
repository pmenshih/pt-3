//$.getScript("/scripts/libcore.js");

var editBlockHtml = "<input autofocus type='text' id='textValueForAjaxChange'></input>\
<span class='devSpanLnk' onclick='Save(\"parentId\")'>save</span>\
&nbsp;&nbsp;&nbsp;<span class='devSpanLnk' onclick='Initialize(\"parentId\")'>cancel</span>";
var startBlockHtml = "<span onClick='ShowEditBlock(\"parentId\")' class='devSpanLnk'>задать</span>";
var a = 'ajaxchangetextvalue';
var b = 'val';
var c = 'textValueForAjaxChange';

$("[id^='" + a + "']").each(function () {
    parentId = $(this).attr('id');
    Initialize(parentId);
})

var str = "flkdsfjlksd";

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
    formData.append('parentId', parentId);
    formData.append('orgId', $.getUrlVar('orgId'));
    formData.append('researchId', $.getUrlVar('researchId'));

    AjaxCall(url
            ,formData
            ,ProcessServerAnswer);
}

function ProcessServerAnswer(response, formData)
{
    var answer = jQuery.parseJSON(response);
    if (answer.result == '0') {
        var curVal = formData.get('val');
        $('#' + formData.get('parentId').replace(a, b)).val(curVal);
        Initialize(formData.get('parentId'));
    }
    else {
        alert('указанное кодовое слово не является уникальным');
    }
}