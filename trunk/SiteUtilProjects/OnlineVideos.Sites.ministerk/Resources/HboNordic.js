function __deleteAllCookiesAndStorage(rurl) {
    sessionStorage.clear();
    localStorage.clear();
    var cookies = document.cookie.split(";");
    for (var i = 0; i < cookies.length; i++) {
        var cookie = cookies[i];
        var eqPos = cookie.indexOf("=");
        var name = eqPos > -1 ? cookie.substr(0, eqPos) : cookie;
        document.cookie = name + "=;expires=Thu, 01 Jan 1970 00:00:00 GMT";
    }
    window.location.href = rurl;
}

function __login(u, p, rurl) {
    var linput = $('#main-login-username');
    if (linput.length > 0 && linput.is(':visible')) {
        linput.val(u);
        $('#main-login-password').val(p);
        $('.submit-button').click();
    } else if (window.location.href != rurl) {
        window.location.href = rurl;
    }
}

function __startWatch() {
    $('div.media-item-container:first').find($('a.watch-action')).click();
}

function __removeFromWatchlist() {
    $('div.media-item-container:first').find($('a.watchlist-action')).click();
}

function __guid() {
    var a = "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx";
    return a.replace(/[xy]/g, function(a) {
        var b = 16 * Math.random() | 0, c = "x" === a ? b: 3 & b | 8;
        return c.toString(16)
    })
}
