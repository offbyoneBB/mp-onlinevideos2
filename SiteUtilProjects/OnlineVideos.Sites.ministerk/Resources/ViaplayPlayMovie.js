function myLogin(u, p) {
    $('#LoginForm_email').val(u);
    $('#LoginForm_password').val(p);
    $('#submit-login-password').click();
}

var __url = "dummy";

function myPlay() {
    var __m = $('a[class="play-link large"]').first();
    if (__m.length > 0) {
        __m.click();
    } else {
        setTimeout("myPlay()", 250);
    }
};