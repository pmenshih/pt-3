var divId = $('.devDiv div div').attr('id');
var dsTData = null;
var gridDiv = 'dataSectionTable';
var grid = null;

UI.EditablePass.OkClick = function (divId) {
    if ($('#' + divId + 'Error').html().length != 0) return;

    var formData = new FormData();
    var url = divId.replace(/_/g, '/');

    formData.append('val', $('#' + divId + 'inputval').val());
    formData.append('orgId', $.getUrlVar('orgId'));
    formData.append('researchId', $.getUrlVar('researchId'));
    formData.append('divId', divId);

    AjaxCall(url
            , formData
            , UI.EditablePass.ProcessServerAnswer);
};

<<<<<<< HEAD
UI.EditablePass.Create('_research_setpassword', 'codeword');
=======
UI.EditableInput.Create('_research_setpassword', 'codeword');
UI.EditableInput.Create('_research_setname', '', 'название исследования');
UI.EditableInput.Create('_research_setdescr', '', 'описание исследования');
>>>>>>> origin/Dev


$('#activeScenarioLnk').on("click", function () {
    Preloader('modalScenarioRaw', true);
    $('#modalScenarioDownloadLink').attr(
        'href', '/research/scenariodownload?scenarioId=' + $('#activeScenarioId').val()
                + '&orgId=' + $.getUrlVar('orgId')
                + '&researchId=' + $.getUrlVar('researchId'));

    let formData = new FormData();
    formData.append('orgId', $.getUrlVar('orgId'));
    formData.append('researchId', $.getUrlVar('researchId'));
    formData.append('scenarioId', $('#activeScenarioId').val());
    AjaxCall('/research/scenariogetraw'
            , formData
            , ScenarioRawLoaded);

    $('#modalScenarioHeader').html('Активный сценарий исследования');
    $('#modalScenarioScreen').show();
});

function ScenarioRawLoaded(response, pars)
{
    $('#modalScenarioRaw').html(response);
}

$("#modalScenarioCancel").click(function () {
    $('#modalScenarioScreen').hide();
});

$('#modalScenarioScreen').on("click", function () {
    $("#modalScenarioCancel").click();
});

$('#modalScenarioWindow').on("click", function (e) {
    e.stopPropagation();
});


function DSShowScenario(id) {
    Preloader('modalScenarioRaw', true);
    let scenarioId = id.replace("lnkShow", "");
    $('#modalScenarioDownloadLink').attr(
        'href', '/research/scenariodownload?scenarioId=' + scenarioId
                + '&orgId=' + $.getUrlVar('orgId')
                + '&researchId=' + $.getUrlVar('researchId'));

    let formData = new FormData();
    formData.append('orgId', $.getUrlVar('orgId'));
    formData.append('researchId', $.getUrlVar('researchId'));
    formData.append('scenarioId', scenarioId);
    AjaxCall('/research/scenariogetraw'
            , formData
            , ScenarioRawLoaded);

    $('#modalScenarioHeader').html('Сценарий среза от ' + $('#' + scenarioId + 'dateBegin')[0].innerText);
    $('#modalScenarioScreen').show();
}

function DSDeleteLnkClick(id) {
    $('#modalDeleteScreen').show();
    let scenarioId = id.replace("lnkDel", "");
    let Msg = "<p>Вы уверены, что хотите удалить срез данных <b> от #dateBegin?</p></b>\
                <p>Будут удалены результаты заполнения опросника и интерпретации.</p>\
                <input type='hidden' id='deletingScenarioId' value='#val'/>"
                    .replace("#dateBegin", $('#' + scenarioId + 'dateBegin')[0].innerText)
                    .replace("#val", scenarioId);
    $('#modalDeleteMessage').html(Msg);
}

function DSRawClick(id) {
    let scenarioId = id.replace("lnkRaw", "");

    let clearLoaderLnk = '<a href="javascript:void()" id="dataSectionTableLoaderCleaner">Очистить</a>';
    let loaderHtml = $('#dataSectionTableLoader').html().replace(clearLoaderLnk, '');
    loaderHtml += 'Построение результатов для среза от ' + $('#' + scenarioId + 'dateBegin')[0].innerText + '. Дождитесь начала загрузки файла.<br/>';
    loaderHtml += clearLoaderLnk;
    $('#dataSectionTableLoader').html(loaderHtml);
    $('#dataSectionTableLoaderCleaner').on("click", function () {
        $('#dataSectionTableLoader').empty();
    });
    
    let formData = new FormData();
    formData.append('orgId', $.getUrlVar('orgId'));
    formData.append('researchId', $.getUrlVar('researchId'));
    formData.append('scenarioId', scenarioId);

    $('[id^=lnkRaw]').hide();
    AjaxCall("/research/datasectionpreparedownloadraw?orgId=" + $.getUrlVar('orgId')
                + "&researchId=" + $.getUrlVar('researchId')
                + "&scenarioId=" + scenarioId
        , formData
        , DSRawDownload);
}

function DSRawDownload(response, pars) {
    let answer = jQuery.parseJSON(response);
    if (answer.result == 0) {
        window.location = "/research/datasectiondownloadraw?orgId=" + $.getUrlVar('orgId')
            + "&researchId=" + $.getUrlVar('researchId')
            + "&scenarioId=" + pars.get('scenarioId')
            + "&resultId=" + answer.data;
    }
    //!!!дописать
    else {

    }
    $('[id^=lnkRaw]').show();
}


$("#modalDeleteCancel").click(function () {
    $('#modalDeleteScreen').hide();
    $("#modalDeleteConfirmCheckBox").prop("checked", false);
    $('#modalDeleteOk').attr('disabled', true);
    $("#modalDeleteError").hide();
});

$("#modalDeleteConfirmCheckBox").on("change", function () {
    if (this.checked) $('#modalDeleteOk').attr('disabled', false);
    else $('#modalDeleteOk').attr('disabled', true);
});

$('#modalDeleteOk').on("click", function () {
    $('#modalDeleteOk').attr('disabled', true);
    let formData = new FormData();
    formData.append('orgId', $.getUrlVar('orgId'));
    formData.append('researchId', $.getUrlVar('researchId'));
    formData.append('scenarioId', $('#deletingScenarioId').val());
    AjaxCall('/research/datasectiondelete', formData, DSDeleteAjax);
});

$('#modalDeleteScreen').on("click", function () {
    $("#modalDeleteCancel").click();
});

$('#modalDeleteWindow').on("click", function (e) {
    e.stopPropagation();
});

function DSDeleteAjax(response, pars) {
    let answer = jQuery.parseJSON(response);
    if (answer.result == 0) {
        $("#modalDeleteCancel").click();
        let idx = $('#' + pars.get('scenarioId') + 'Idx').val();
        dsTData.splice(idx, 1);
        grid.Init();
        $("[id^='lnkDel']").click(function () { DSDeleteLnkClick(this.id); });
    }
    else {
        $("#modalDeleteError").show();
    }
}


function DSGetData() {
    let formData = new FormData();
    formData.append('orgId', $.getUrlVar('orgId'));
    formData.append('researchId', $.getUrlVar('researchId'));
    AjaxCall('/research/getdatasections', formData, DSTGetDataCallback);
}

function DSTGetDataCallback(response, pars) {
    let answer = jQuery.parseJSON(response);
    if (answer.result != 0) {
        let msg = "\
<div class='devValidateError'>\
<p>Ошибка построения таблицы срезов.</p>\
<p>Попробуйте перезагрузить страницу, или обратитесь в службу поддержки.</p>\
</div>";
        $('#' + gridDiv).html(msg);
        return;
    }

    dsTData = jQuery.parseJSON(answer.data);

    var columns = {};
    columns[0] = {
        title: 'Дата создания'
        , tpl: "<span id='@scenarioId@dateBegin'>@dateBegin@</span>"
        ,sortable: 'dateBegin'
    };
    columns[1] = {
        title: 'Кол-во ответов'
        , tpl: "<input type='hidden' id='@scenarioId@Idx' value='@@rowIdx@@'/>@answersCount@"
    };
    if ($('#dsTViewerCoachAdmin').val()) {
        columns[2] = {
            title: ''
        , tpl: "<a href='javascript:void(0)' id='lnkRaw@scenarioId@'>RAW</a> | <a href='javascript:void(0)'>Интерпретация</a>"
        };
    }
    if ($('#dsTManagerCoachAdmin').val()) {
        columns[3] = {
            title: ''
        , tpl: "<a href='javascript:void(0)' id='lnkShow@scenarioId@'>Сценарий</a>"
        };
    }
    if ($('#dsTManagerAdmin').val()) {
        columns[4] = {
            title: ''
        , tpl: "<a href='javascript:void(0)' id='lnkDel@scenarioId@'>Удалить</a>"
        };
    }
    grid = new Grid(gridDiv, dsTData, columns);
    grid.Init();
    $("[id^='lnkDel']").click(function () { DSDeleteLnkClick(this.id); });
    $("[id^='lnkShow']").click(function () { DSShowScenario(this.id); });
    $("[id^='lnkRaw']").click(function () { DSRawClick(this.id); });
}


$('#uploadScenarioLnk').on("click", function () {
    $('#modalScUploadNoFile').hide();
    $('#modalScUploadScreen').show();
});

$("#modalScUploadCancel").click(function () {
    $("#modalScUploadError").hide();
    $("#modalScUploadLog").empty();
    $('#modalScUploadScreen').hide();
    $("#modalScUploadFilename").replaceWith($("#modalScUploadFilename").val('').clone(true));
});

$('#modalScUploadScreen').on("click", function () {
    $("#modalScUploadCancel").click();
});

$('#modalScUploadWindow').on("click", function (e) {
    e.stopPropagation();
});

$('#modalScUploadButton').on("click", function () {
    UploadScenario();
})

function UploadScenario() {
    $('#modalScUploadNoFile').hide();
    if ($("#modalScUploadFilename").val() == '') {
        $('#modalScUploadNoFile').show();
        return;
    }

    $("#modalScUploadError").hide();
    $("#modalScUploadLog").empty();

    var fileData = $('#modalScUploadFilename').prop('files')[0];
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
                location.reload();
            }
        }
    });
}

function ShowUploadLog(data) {
    $("#modalScUploadError").show();
    var msgs = jQuery.parseJSON(data);
    var out = "<div class='devUEL'>" + msgs.excMes1;
    if (msgs.excMes2.length > 0) {
        out += "<div class='devUELI'>" + msgs.excMes2 + "</div>";
    }
    out += '</div>';
    $("#modalScUploadLog").append(out);
}

function GlobalGridInit() {
    grid.Init();
}

$(document).ready(function () {
    Preloader('dataSectionTable', true);
    DSGetData();
});