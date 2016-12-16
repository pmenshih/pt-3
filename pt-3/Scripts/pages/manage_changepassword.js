$('#btnSubmit').click(function (e) {
    if (!canSubmit) e.preventDefault();
});

$(':password').on("propertychange change click keyup input paste", function (e) {
    passwordlValidated = FormValidate(this.id, 'password');
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
    if (passwordlValidated) {
        $('#btnSubmit').attr('disabled', false);
    }
    else { $('#btnSubmit').attr('disabled', true); }
}


$(function () {

    var confirmpass = $('#ConfirmPassword');

    var passFields = $(':password'),
    validResult = $("#validpass");
    passFields.on('input', comparingPasswords);
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


});
