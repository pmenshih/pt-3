var divId = $('.devDiv div div').attr('id');
var dsTData = null;
var gridDiv = 'dataSectionTable';
var grid = null;

UI.EditableInput.OkClick = function (divId) {
    if ($('#' + divId + 'Error').html().length != 0) return;

    var formData = new FormData();
    var url = divId.replace(/_/g, '/');

    formData.append('val', $('#' + divId + 'inputval').val());
    formData.append('orgId', $.getUrlVar('orgId'));
    formData.append('researchId', $.getUrlVar('researchId'));
    formData.append('divId', divId);

    AjaxCall(url
            , formData
            , UI.EditableInput.ProcessServerAnswer);
};

UI.EditableInput.Create('_research_setpassword', 'codeword');


$('#activeScenarioLnk').on("click", function () {
    $('#modalScenarioDownloadLink').attr(
        'href', '/research/scenariodownload?scenarioId=' + $('#activeScenarioId').val()
                + '&orgId=' + $.getUrlVar('orgId')
                + '&researchId=' + $.getUrlVar('researchId'));
    $('#modalScenarioRaw').html($('#activeScenarioRaw').val());
    $('#modalScenarioHeader').html('Активный сценарий исследования');
    $('#modalScenarioScreen').show();
});

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
    let scenarioId = id.replace("lnkShow", "");
    $('#modalScenarioDownloadLink').attr(
        'href', '/research/scenariodownload?scenarioId=' + scenarioId
                + '&orgId=' + $.getUrlVar('orgId')
                + '&researchId=' + $.getUrlVar('researchId'));
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
    };
    columns[1] = {
        title: 'Кол-во ответов'
        , tpl: "<input type='hidden' id='@scenarioId@Idx' value='@@rowIdx@@'/>@answersCount@"
    };
    if ($('#dsTViewerCoachAdmin').val()) {
        columns[2] = {
            title: ''
        , tpl: "<a href=''>RAW</a> | <a href=''>Интерпретация</a>"
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
}

$(document).ready(function () {
    Preloader('dataSectionTable', true);
    DSGetData();
});