//Created by NitroXenon
//Github: https://github.com/NitroXenon/Terrarium-Public/blob/gh-pages/openload2.js
var decodedValue = "";
var r = "r";
var document = {};
var $ = function (selector) {
    if (selector == '#streamurl') {
        return {
            text: function (result) {
                result = 'https://openload.co/stream/' + result + '?mime=true';
                decodedValue = result;
            }
        }
    } else if (selector == '#' + r) {
        return {
            text: function () {
                return encoded;
            }
        }
    } else if (selector == document) {
        return {
            ready: function (func) {
                func();
            }
        }
    }
}
function getDecodedValue() {
    return decodedValue;
}