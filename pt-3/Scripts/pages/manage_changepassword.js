
UI.EditablePass.Create('idpass', '[{"id":"OldPassword","name":"Старый пароль"},{"id":"NewPassword","name":"Новый пароль"},{"id":"ConfirmPassword","name":"Новый пароль еще раз"}]');


$(':password').on("propertychange change click keyup input paste", function (e) {
    FormValidate(this.id, 'password');
    SubmitButtonDisable();
});

function FormValidate(id, type) {
    if (Validate($('#' + id).val(), type).length != 0) {
        $('#' + id).addClass('devValidateError');
        return false;
    } else {
        $('#' + id).removeClass('devValidateError');
        return true;
    }
}

function SubmitButtonDisable() {
    var cnt = 0;
    $('[type="password"]').each(function () {
        if(FormValidate(this.id, 'password')) cnt++;
    })
    if (cnt == 3) $('#btnSubmit').attr('disabled', false);
    else $('#btnSubmit').attr('disabled', true);
}

function comparingPasswords(e) {
    var output = '',
        err = false,
        p1 = $.trim(passFields.eq(1).val()),
        p2 = $.trim(passFields.eq(2).val());
    $('#btnSubmit').attr('disabled', false);
    confirmpass.removeClass('devValidateError').addClass('ok');
    if (p1 == '' || p2 == '') {
        output = 'Заполните поля!';
        err = true;
        $('#btnSubmit').attr('disabled', true);
        confirmpass.addClass('devValidateError');
    } else {
        if (p1 != p2) {
            output = 'Пароли не совпадают';
            err = true;
            $('#btnSubmit').attr('disabled', true);
            confirmpass.addClass('devValidateError');
        }
    }
    validResult.text(output);
    if (err) e.preventDefault();
}

var confirmpass = $('#ConfirmPassword');
var passFields = $(':password'),
validResult = $("#validpass");
passFields.on('input', comparingPasswords);



