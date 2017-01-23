function switchProfile(switchUrl, index) {
    if (document.querySelectorAll('a.profile-link').length > index) {
        setTimeout(function () {
            document.querySelectorAll('a.profile-link')[index].click();
            setTimeout(function () {
                switchProfileCallback("{}");
            }, 1500);
        }, 500);
    } else if (window.jQuery) {
        jQuery.ajax({
            url: switchUrl,
            dataType: 'json',
            cache: false,
            type: 'GET',
            success: switchProfileCallback
        })
    }
}

function switchProfileCallback(data) {
    window.location.href = "https://www.netflix.com/";
}

function doClickDelay() {
    setTimeout(function () {
        document.querySelectorAll('.login-button')[0].click()
    }, 500);
}
