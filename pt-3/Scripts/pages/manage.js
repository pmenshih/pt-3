var divId = $('dd div').attr('id');

UI.EditableInput.OkClick = function (divId) {
    //проверка валидатором (если нужна):
    //проверяем нужна ли этому полю проверка
    //накладываем валидатор. если ответ функции валидации пустая строка, то всё нормально. 
    //если не пустая, то вставляем её в элемент соощения об ошибке и показываем его
    
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

UI.EditableInput.Create('_manage_surnamechange');

UI.EditableInput.Create('_manage_namechange');

UI.EditableInput.Create('_manage_patronimchange');

