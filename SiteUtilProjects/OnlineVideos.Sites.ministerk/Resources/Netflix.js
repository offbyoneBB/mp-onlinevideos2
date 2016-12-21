function fillPassword(pwd) {
    var field = jQuery("input[name=password]:visible");
    if (field !== null && field.length > 0) {
        field.val(pwd).change();
        return true;
    };
    return false;
}

function fillUser(user) {
    var field = jQuery("input[name=email]:visible");
    if (field !== null && field.length > 0) {
        field.val(user).change();
        return true;
    };
    return false;
}

function doClick() {
    jQuery(".login-button:visible").click();
}

function doLogin(user, pwd) {
    if (fillUser(user) && fillPassword(pwd)) {
        //Fill form in one step
        // Cannot click, form validation fails....
        document.getElementsByTagName('form')[0].submit();
    }
    else {
        //Fill in two steps
        doClick();
        setTimeout(function () {
            fillPassword(pwd);
            doClick();
        }, 1000);
    }
}