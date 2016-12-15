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

function AjaxCall(url, pars, onsuccess)
{
    $.ajax({
        url: url,
        dataType: 'text',
        cache: false,
        contentType: false,
        processData: false,
        data: pars,
        type: 'post',
        success: function (response) {
            if (typeof onsuccess == 'function') {
                onsuccess(response, pars);
            }
        }
    });
}

function Validate(val, type)
{
    var errMsg = '';

    switch (type)
    {
        case 'text':
            var regexp = /^.{1,30}$/i;
            errMsg = 'Ошибка заполнения поля';
            if (!regexp.test(val)) return errMsg;
            else return '';
            break;
        case 'email':
            var regexp = /^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*@((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$/i;
            errMsg = 'Неккоректный формат адреса почты';
            if (!regexp.test(val)) return errMsg;
            else return '';
            break;
        default:
            return '';
    }
}

/*$(document).ready(function(){
    $('input, textarea').unbind().blur( function(){       
        var idValid = $(this).attr('id');
        var typeValid = $(this).attr('type');
        var valValid = $(this).val();
        switch(typeValid) {
            case 'text':
                var rv_text = /^.{0,30}$/i;
                if (valValid.length > 2 && valValid != '' )
                {
                    $(this).addClass('not_error').removeClass('error');
                    $(this).prev('.error-box').text('');
                }
                else
                {
                    $(this).removeClass('not_error').addClass('error');
                    $(this).prev('.error-box').html('Ошибка заполнения поля');

                }
                break;

            case 'email':
                var rv_email = /^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*@((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$/i;
                if (valValid != '' && rv_email.test(valValid))
                {
                    $(this).addClass('not_error').removeClass('error');
                    $(this).prev('.error-box').text('');
                }
                else
                {
                    $(this).removeClass('not_error').addClass('error');
                    $(this).prev('.error-box').html('Ошибка заполнения поля');
                }
                break;

            case 'password':
                var rv_password = /^(.{0,}(([a-zA-Z][^a-zA-Z])|([^a-zA-Z][a-zA-Z])).{4,})|(.{1,}(([a-zA-Z][^a-zA-Z])|([^a-zA-Z][a-zA-Z])).{3,})|(.{2,}(([a-zA-Z][^a-zA-Z])|([^a-zA-Z][a-zA-Z])).{2,})|(.{3,}(([a-zA-Z][^a-zA-Z])|([^a-zA-Z][a-zA-Z])).{1,})|(.{4,}(([a-zA-Z][^a-zA-Z])|([^a-zA-Z][a-zA-Z])).{0,})$/i;
                if (valValid != '' && rv_password.test(valValid))
                {
                    $(this).addClass('not_error').removeClass('error');
                    $(this).prev('.error-box').text('');
                }
                else
                {
                    $(this).removeClass('not_error').addClass('error');
                    $(this).prev('.error-box').html('Ошибка заполнения поля');
                }
                break;
        }
        switch (idValid) {

            case 'phone':
                var rv_phone = /^\d{11,15}$/i;
                if (valValid != '' && rv_phone.test(valValid)) {
                    $(this).addClass('not_error').removeClass('error');
                    $(this).prev('.error-box').text('');
                }
                else {
                    $(this).removeClass('not_error').addClass('error');
                    $(this).prev('.error-box').html('Ошибка заполнения поля');
                }
                break;

        }

    }); 
});*/