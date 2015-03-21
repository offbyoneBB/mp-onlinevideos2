var steps = [-10000000, -7200, -3600, -1800, -900, -600, -300, -180, -60, -30, -15, 0, 15, 30, 60, 180, 300, 600, 900, 1800, 3600, 7200, 10000000];
var stepNames = ['Start', '-2h', '-1h', '-30m', '-15m', '-10m', '-5m', '-3m', '-1m', '-30s', '-15s', ' ', '+15s', '+30s', '+1m', '+3m', '+5m', '+10m', '+15m', '+30m', '+1h', '+2h', 'End'];
var skipIndex = 11;
var minIndex = 0;
var maxIndex = steps.length - 1;
var skipTimeout = 3000;
var skipTimer = null;

var contentDiv = document.getElementById('content-wrapper');
var videoObject = document.getElementById('videoPlayer').content.MainPage;
var divHidden = true;

var mainDiv = document.createElement('div');
var h1 = document.createElement('h1');
mainDiv.setAttribute('id', '_MyMainDiv');

document.getElementsByTagName('body')[0].appendChild(mainDiv);
function showOsd(indexNameToShow) {
	if (skipTimer != null) {
		clearTimeout(skipTimer);
	};
	skipTimer = setTimeout(doSkip, skipTimeout);
	if (divHidden) {
		contentDiv.style.position = 'fixed';
		contentDiv.style.top = '-100px';
		mainDiv.setAttribute('style', 'position:fixed;left:0px;bottom:0px;width:100%;height:100px;background:#000;color:#fff;text-align: center;font-size: 50px;');
		divHidden = false;
	};
	mainDiv.innerText = stepNames[indexNameToShow];
};

function doSkip() {
	if (skipTimer != null) {
		clearTimeout(skipTimer);
	};
	skipTimer = null;
	contentDiv.style.position = '';
	contentDiv.style.top = '';
	mainDiv.setAttribute('style', 'display:none;');
	divHidden = true;
	var length = videoObject.JS_GetPlayerInfo().Length;
	var position = videoObject.JS_GetPlayerInfo().Position;
	if (skipIndex != 11) {
		if (videoObject.JS_GetPlayerInfo().IsLive) {
			videoObject.JS_Seek(position - length + (steps[skipIndex] * 1000));
		} else {
			var skip = position + (steps[skipIndex] * 1000);
			if (skip > length) {
				skip = length - 10000;
			};
			if (skip < 0) {
				skip = 0
			};
			videoObject.JS_Seek(skip);
		};
	};
	skipIndex = 11;
};
function pause() {
	videoObject.JS_Pause();
};
function play() {
	videoObject.JS_Play();
};
function back() {
	var length = videoObject.JS_GetPlayerInfo().Length;
	var position = videoObject.JS_GetPlayerInfo().Position;
	if (videoObject.JS_GetPlayerInfo().IsLive) {
		if (position - length + (steps[skipIndex] * 1000) <= -length) {
			return;
		};
	} else {
		if (position + (steps[skipIndex] * 1000) <= 0) {
			return;
		};
	};
	skipIndex = skipIndex - 1;
	skipIndex = skipIndex < minIndex ? minIndex : skipIndex;
	var indexNameToShow = skipIndex;
	if (videoObject.JS_GetPlayerInfo().IsLive) {
		if (position - length + (steps[skipIndex] * 1000) <= -length) {
			indexNameToShow = minIndex;
		};
	} else {
		if (position + (steps[skipIndex] * 1000) <= 0) {
			indexNameToShow = minIndex;
		};
	};
	showOsd(indexNameToShow);
};

function forward() {
	var length = videoObject.JS_GetPlayerInfo().Length;
	var position = videoObject.JS_GetPlayerInfo().Position;
	if (videoObject.JS_GetPlayerInfo().IsLive) {
		if ((position - length + (steps[skipIndex] * 1000)) >= 0) {
			return;
		};
	} else {
		if (position + (steps[skipIndex] * 1000) >= length) {
			return;
		};
	};
	skipIndex = skipIndex + 1;
	skipIndex = skipIndex > maxIndex ? maxIndex : skipIndex;
	var indexNameToShow = skipIndex;
	if (videoObject.JS_GetPlayerInfo().IsLive) {
		if ((position - length + (steps[skipIndex] * 1000)) >= 0) {
			indexNameToShow = maxIndex;
		};
	} else {
		if (position + (steps[skipIndex] * 1000) >= length) {
			indexNameToShow = maxIndex;
		};
	};
	showOsd(indexNameToShow);
};