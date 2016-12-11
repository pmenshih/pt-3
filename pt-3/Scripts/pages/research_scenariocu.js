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

function ShowChosenBlock(id) {
    $("[id^='block']").hide();
    $("#" + id.replace("lnk", "block")).show();
}

function UploadScenario() {
    if ($("#filename").val() == '') {
        alert('Не выбран файл для загрузки');
        return;
    }

    $("#uploadError").hide();
    $("#uploadLog").empty();

    var fileData = $('#filename').prop('files')[0];
    var formData = new FormData();
    formData.append('filename', fileData);
    formData.append('orgId', $.getUrlVar('orgId'));
    formData.append('researchId', $.getUrlVar('researchId'));
    $.ajax({
        url: '/research/uploadscenario',
        dataType: 'text',
        cache: false,
        contentType: false,
        processData: false,
        data: formData,
        type: 'post',
        success: function (response) {
            var answer = jQuery.parseJSON(response);
            if (answer.result != '0') ShowUploadLog(answer.data);
            else {
                window.location = "/research/show?orgId=" + $.getUrlVar('orgId')
                    + "&researchId=" + $.getUrlVar('researchId');
                return;
            }
        }
    });
}

function ShowUploadLog(data)
{
    $("#uploadError").show();
    var msgs = jQuery.parseJSON(data);
    var out = "<div class='devUEL'>" + msgs.excMes1;
    if (msgs.excMes2.length > 0) {
        out += "<div class='devUELI'>" + msgs.excMes2 + "</div>";
    }
    out += '</div>';
    $("#uploadLog").append(out);
}

$("#uploadFileButton").click(function () { UploadScenario(); });
$("[id^='lnk']").click(function () { ShowChosenBlock(jQuery(this).attr("id")); });