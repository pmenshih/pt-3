var surnameValidated = true;
var emailValidated = true;

$('#email').on("propertychange change click keyup input paste", function (e) {
    emailValidated = FormValidate(this.id, 'email');
    SubmitButtonDisable();
});
$('#surname').on("propertychange change click keyup input paste", function (e) {
    surnameValidated = FormValidate(this.id, 'text');
    SubmitButtonDisable();
});

function FormValidate(id,type)
{
    if (Validate($('#' + id).val(), type).length !== 0) {
        $('#' + id).addClass('devValidateError');
        return false;
    } else {
        $('#' + id).removeClass('devValidateError');
        return true;
    }
}

function SubmitButtonDisable()
{
    if (surnameValidated
        && emailValidated) {
        $('#btnSubmit').attr('disabled', false);
    }
    else { $('#btnSubmit').attr('disabled', true); }
}
