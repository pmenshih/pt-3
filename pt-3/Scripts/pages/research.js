//быдлокод
//переменная, содержащая JSON данных для таблицы
var rTData = null;
var gridDiv = 'resTable';
var grid = null;

/**** ОБЛАСТЬ МОДАЛЬНОГО ОКНА ПОДТВЕРЖДЕНИЯ УДАЛЕНИЯ ИССЛЕДОВАНИЯ *****/
//биндим показ модального окна на нажатие кнопки "Удалить"
function ResearchDeleteLnkClick(id) {
    //показываем окно
    $('#modalDeleteScreen').show();

    //быдлокод
    //подмена заглушек в тексте реальными значениями, чтобы текст казался более "адресным"
    //для этого получим id элемента (кнопки удаления), чтобы получить идентификатор исследования,
    //по которому мы сможем получить его название и дату создания
    let researchId = id.replace("lnkDel", "");
    let Msg = "<p>Вы уверены, что хотите удалить исследование <b>«#research» от #researchDate?</p></b>\
                <p>Будут удалены все сценарии, срезы данных и интерпретации.</p>\
                <input type='hidden' id='deletingResearchId' value='#val'/>"
                    .replace("#research", $('#' + researchId + 'Name')[0].innerText)
                    .replace("#researchDate", $('#' + researchId + 'dateCreate')[0].innerText)
                    //researchId нужен для того, чтобы можно было отправить запрос на сервер
                    .replace("#val", researchId);
    $('#modalDeleteMessage').html(Msg);
}

//кнопка "отмена"
$("#modalDeleteCancel").click(function () {
    //прячем окно
    $('#modalDeleteScreen').hide();
    //быдлокод:
    //  — убираем галочку "я понимаю, что делаю"
    $("#modalDeleteConfirmCheckBox").prop("checked", false);
    //  — дизаблим кнопку "удалить"
    $('#modalDeleteOk').attr('disabled', true);
    //  — прячем сообщение об ошибке
    $("#modalDeleteError").hide();
});

//обработчик установки галочки "я понимаю, что делаю"
$("#modalDeleteConfirmCheckBox").on("change", function () {
    //при выборе активируем кнопку "удалить"
    if (this.checked) $('#modalDeleteOk').attr('disabled', false);
    else $('#modalDeleteOk').attr('disabled', true);
});

//кнопка "удалить"
$('#modalDeleteOk').on("click", function () {
    //дизаблим кнопку "удалить"
    $('#modalDeleteOk').attr('disabled', true);
    //собственно "удаление"
    //собираем параметры
    let formData = new FormData();
    formData.append('orgId', $.getUrlVar('orgId'));
    formData.append('researchId', $('#deletingResearchId').val());
    //отправляем запрос
    AjaxCall('/research/delete', formData, ResearchDeleteAjax);
});

//нажатие куда-нибудь мимо модального окна, нужно его (окно) спрятать
$('#modalDeleteScreen').on("click", function () {
    $("#modalDeleteCancel").click();
});

//если кликают непосредственно на модальное окно, то событие нужно отменить
//, чтобы оно не попало в $('#modalDeleteScreen').on("click"
$('#modalDeleteWindow').on("click", function (e) {
    e.stopPropagation();
});

//колбэк успешного запроса на удаление исследования к серверу
function ResearchDeleteAjax(response, pars)
{
    let answer = jQuery.parseJSON(response);
    //всё хорошо, исследование удалено
    if (answer.result == 0) {
        //прячем модальное окно
        $("#modalDeleteCancel").click();
        //получаем индекс элемента для удаления из JSON-массива
        let idx = $('#' + pars.get('researchId') + 'Idx').val();
        //удаляем
        rTData.splice(idx, 1);
        //перестраиваем грид
        grid.Init();
        //обновляем обработчики онклик для кнопок "удалить"
        $("[id^='lnkDel']").click(function () { ResearchDeleteLnkClick(this.id); });
    }
    //что-то пошло не так
    else {
        //показываем сообщение об ошибке
        $("#modalDeleteError").show();
    }
}
/**** КОНЕЦ ОБЛАСТИ МОДАЛЬНОГО ОКНА ПОДТВЕРЖДЕНИЯ УДАЛЕНИЯ ИССЛЕДОВАНИЯ *****/


/**** ОБЛАСТЬ КОМПОНЕНТА ТАБЛИЦЫ ИССЛЕДОВАНИЙ *****/
//получение JSON'а с данными для таблицы
function RTGetData()
{
    //параметры запроса
    let formData = new FormData();
    formData.append('orgId', $.getUrlVar('orgId'));
    //запрос
    AjaxCall('/research/getall', formData, RTGetDataCallback);
}

//колбэк успешного запроса данных для таблицы
function RTGetDataCallback(response, pars)
{
    //запрос
    let answer = jQuery.parseJSON(response);
    //что-то пошло не так, скажем об этом и всё
    if (answer.result != 0) {
        let msg = "\
<div class='devValidateError'>\
    <p>Ошибка построения таблицы исследований.</p>\
    <p>Попробуйте перезагрузить страницу, или обратитесь в службу поддержки.</p>\
</div>";
        $('#' + gridDiv).html(msg);
        return;
    }
    
    //парсим ответ
    rTData = jQuery.parseJSON(answer.data);

    //заполняем столбцы
    var columns = {};
    columns[0] = {
        title: 'Название'
        ,tpl: "<input type='hidden' id='@id@Idx' value='@@rowIdx@@'/><a href='/research/show?orgId=" + $.getUrlVar('orgId') + "&researchId=@id@' id='@id@Name'>@name@</a>"
    };
    columns[1] = {
        title: 'Информация'
        ,tpl: "@info@"
    };
    columns[2] = {
        title: 'Дата создания'
        , tpl: "<span id='@id@dateCreate'>@dateCreate@</span>"
    };
    columns[3] = {
        title: 'Тип'
        , tpl: "@typeDescr@"
    };
    columns[4] = {
        title: 'Статус'
        , tpl: "@statusDescr@"
    };
    //добавляем админский столбец
    if ($('#rTAllowCRUD').val()) {
        columns[5] = {
            title: ''
        , tpl: "<a href='javascript:void(0)' id='lnkDel@id@'>Удалить</a>"
        };
    }
    //инициализируем грид
    grid = new Grid(gridDiv, rTData, columns);
    grid.Init();
    //биндим событие клика по ссылке удаления исследования
    $("[id^='lnkDel']").click(function () { ResearchDeleteLnkClick(this.id); });
}
/**** КОНЕЦ ОБЛАСТИ КОМПОНЕНТА ТАБЛИЦЫ ИССЛЕДОВАНИЙ *****/


$(document).ready(function () {
    Preloader(gridDiv, true);
    RTGetData();
});
