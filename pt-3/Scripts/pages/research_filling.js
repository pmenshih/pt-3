﻿$('#formFilling').submit(function () {
    $('#btnNext').hide();
    $('#btnBack').hide();
    return true;
});

if ($('#softQ').val() == '1')
{
    $("[id^='softAnswer']").on("change", function () {
        $('#answer').val('');
        $("[id^='softAnswer']").each(function () {
            if (this.checked)
                $('#answer').val($('#answer').val() + ';' + this.value);
        });
        $('#answer').val($('#answer').val().replace(/^;/g, ''));
    });
}