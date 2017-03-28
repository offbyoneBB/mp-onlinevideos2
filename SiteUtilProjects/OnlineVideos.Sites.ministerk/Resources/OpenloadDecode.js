var decoded;
var document;
var window = this;
$ = function () {
    return {
        text: function (a) {
            if (a) {
                decoded = a;
            } else {
                return id;
            }
        },
        ready: function (a) {
            a()
        }
    }
};
function getUrl() {
    return 'https://openload.co/stream/' + decoded + '?mime=true';
}