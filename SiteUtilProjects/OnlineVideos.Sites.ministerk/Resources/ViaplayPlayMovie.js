function myLogin(u, p) {
    $('#LoginForm_email').val(u);
    $('#LoginForm_password').val(p);
    $('#login-btn').click();
}

var __url = "dummy";

function myPlay() {
    var __m = $('a[href$="' + __url + '"]').first();
    if (__m.length > 0) {
        __m.click();
    } else {
        setTimeout("myPlay()", 250);
    }
};