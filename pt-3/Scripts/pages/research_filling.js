$('#formFilling').submit(function () {
    $('#btnNext').hide();
    $('#btnBack').hide();
    /*$('#btnNext').attr('disabled', true);
    $('#btnBack').attr('disabled', true);*/
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