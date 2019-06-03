$(document).ready(function () {
    var myTextArea = $('#workproccess');
    var result = $('#result');

    $('#subButton').on('click', function () {
        if ($("#agree").attr("checked") == 'checked') {
            myTextArea.val(result.text() + " Work is done!");
        } else {
            window.alert('Идите выполняйте свою работу!');

            myTextArea.val(result.text() + " Work in a proccess");
            $("#agree").css('border', '1px solid red');
        }
    });

    $('#submit').on('click', function () {
        if ($("#requests").val() == null) {
            $("#requests").val("parkowner/setwork?worker=" + $("#worker").val() + "&work=" + $("#work").val());;
        } else {
            $("#requests").val("parkowner/setwork?worker=" + $("#worker").val() + "&work=" + $("#work").val());
        }
    });
});