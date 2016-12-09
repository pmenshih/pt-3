function ShowChosenBlock(id) {
    $("[id^='block']").hide();
    $("#" + id.replace("lnk", "block")).show();
}

function UploadScenario() {
    if ($("#filename").val() == '') {
        alert('Не выбран файл для загрузки');
        return;
    }

    var fileData = $('#filename').prop('files')[0];
    var formData = new FormData();
    formData.append('filename', fileData);
    formData.append('orgId', $("#orgId").val());
    $.ajax({
        url: '/research/uploadscenario',
        dataType: 'text',
        cache: false,
        contentType: false,
        processData: false,
        data: formData,
        type: 'post',
        success: function (response) {
            alert(response);
        }
    });
}

$("#uploadFileButton").click(function () { UploadScenario(); });
$("[id^='lnk']").click(function () { ShowChosenBlock(jQuery(this).attr("id")); });