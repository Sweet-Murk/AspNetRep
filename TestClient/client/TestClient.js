var total = 0,
    delays = 0,
    speed = 10;

var requests = ["parkowner/time", "parkowner/setwork?worker=John&work=clear toilet", "parkowner/exception?msg=some exception text"];

$(document).ready(function () {
    $("#speed").val(speed);
    $("#speed").change(function () {
        speed = $(this).val();
    });
    $("#reset").click(function () {
        total = 0;
        delays = 0;
    });
    $("#submit").click(function () {
        sendRequest();
    });
    $("#tabs").tabs();
    $("button").button();

    setTimer(1000/speed);

    $("c").each(function (i) {
        $(this).replaceWith("<span class='code'>" + $(this).html() + "</span>");
    });

    $("#requests").autocomplete({
        source: requests
    });

    $("#requests").keypress(function (e) {
        if (e.which == 13) {
            sendRequest();
        }
    });
});

function sendRequest() {
    var value = $.trim($("#requests").val().toLowerCase());
    if (value.length < 1)
        return;
    $.ajax({
        url: "data/" + value,
        success: function (data) {
            $("#result").html(data);
        },
        error: function (jqXHR, textStatus, errorThrown) {
            $("#result").html("<span style='color:Red;'>Requests must be in the form of segment1/segment2</span>");
        }
    });
}

function setTimer(delay) {
    if (delay < 1) {
        delay = 1;
    }
    setTimeout(function () {
        processTimer();
    }, delay);
}

function processTimer() {
    var d1 = new Date();
    var start = d1.getTime();

    $.get("data/simple/time?dummy=" + start, function (data) {
        var d2 = new Date();
        var end = d2.getTime();
        var delay = end - start;
        delays += delay;
        total++;
        $("#time").html(data);
        $("#calls").html(total);
        $("#average").html(round(delays / total));
        setTimer((1000 / speed) - delay - 1);
    });
}

function round(num) {
    return Math.round(num * Math.pow(10, 2)) / Math.pow(10, 2);
}