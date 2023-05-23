function openOverlay() {
    $("#SiteOverlay").show();
}

function closeOverlay() {
    if (localStorage) {
        localStorage.dontShowOverlay = $('#dontShowOverlay')[0].checked ? 1 : 0;
    }

    $("#SiteOverlay").hide();
}

$(function () {
    if (localStorage) {
        if (localStorage.dontShowOverlay > 0) {
            $("#SiteOverlay").hide();
        } else {
            $("#SiteOverlay").show();
        }
    }

    function updateState() {
        var isVisible = $('#Password').val().length > 0 && $('#Email').val().length > 0;
        $("#sbmtBtn").prop('disabled', !isVisible);
    }

    if (!$('#UrlHash').val()) {
        $('#UrlHash').val(window.location.hash);
    }

    $('#Password,#Email').on('input', updateState);
    updateState();
});