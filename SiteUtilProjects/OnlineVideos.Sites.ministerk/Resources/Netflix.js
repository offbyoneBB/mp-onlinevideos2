function switchProfile(profiletoken) {
    var proflinks = document.querySelectorAll('a.profile-link');
    setTimeout(function () {
        for (var i = 0; i < proflinks.length; i++) {
            if (proflinks[i].href.indexOf(profiletoken) !== -1) {
                proflinks[i].click();
            };
        };
        setTimeout(function () {
            switchProfileCallback("{}")
        }, 1500);
    }, 500);
}

function switchProfileCallback(data) {
    window.location.href = "https://www.netflix.com/";
}

function doClickDelay() {
    setTimeout(function () {
        document.querySelectorAll('.login-button')[0].click()
    }, 500);
}
