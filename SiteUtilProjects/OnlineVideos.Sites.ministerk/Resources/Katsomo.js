function myZoom() {
    document.getElementById('katsomo-navi').style.zIndex = 0;
    document.getElementById('video0_desktop-player').style.position = 'fixed';
    document.getElementById('video0_desktop-player').style.zIndex = 101;
    document.getElementById('video0_desktop-player').style.left = "0px";
    document.getElementById('video0_desktop-player').style.top = "0px";
    document.getElementById('video0_desktop-player').style.width = (window.innerWidth) + 'px';
    document.getElementById('video0_desktop-player').style.height = (window.innerHeight) + 'px';
}

function myToggleZoom() {
    if (document.getElementById('video0_desktop-player').style.position == 'fixed') {
        document.getElementById('video0_desktop-player').style.height = '';
        document.getElementById('video0_desktop-player').style.width = '';
        document.getElementById('video0_desktop-player').style.top = '';
        document.getElementById('video0_desktop-player').style.left = '';
        document.getElementById('video0_desktop-player').style.position = '';
        document.getElementById('video0_desktop-player').style.zIndex = '';
        document.getElementById('katsomo-navi').style.zIndex = '';
    } else {
        myZoom();
    }
}
function myPause() {
    document.getElementsByTagName('object')[0].content.MainPlayer.Pause();
}

function myPlay() {
    document.getElementsByTagName('object')[0].content.MainPlayer.Play();
}

function myBack() {
    var pos = document.getElementsByTagName('object')[0].content.MainPlayer.RelativePosition;
    if (pos > 30) {
        document.getElementsByTagName('object')[0].content.MainPlayer.RelativePosition -= 30;
    } else {
        document.getElementsByTagName('object')[0].content.MainPlayer.RelativePosition = 0;
    }
}

function myForward() {
    var dur = document.getElementsByTagName('object')[0].content.MainPlayer.Duration;
    var pos = document.getElementsByTagName('object')[0].content.MainPlayer.RelativePosition;
    if (dur - pos > 30) {
        document.getElementsByTagName('object')[0].content.MainPlayer.RelativePosition += 30;
    }
}

