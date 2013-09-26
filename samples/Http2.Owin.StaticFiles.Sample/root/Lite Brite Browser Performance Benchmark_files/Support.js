var upgrade_button = document.getElementById("upgrade_button");
var dismiss_button = document.getElementById("dismiss_button");

upgrade_button.addEventListener("click", function upgrade() { window.location = "http://msdn.microsoft.com/en-us/ie/"; }, false);
dismiss_button.addEventListener("click", function dismiss() { hideUpgradeNotice(); }, false);

window.addEventListener("resize", resize);

function hideUpgradeNotice()
{
    var dialog = document.getElementById("upgrade");
    if (dialog)
    {
        dialog.style.display = "none";
    }
}

function checkBrowser()
{
    var browserInfo = getBrowserInformation();
    document.getElementById("browserName").innerText = browserInfo.browserName + " " + browserInfo.browserVersion;

    if (!browserSupportsMultilineFlex())
    {
        document.getElementById("upgrade").style.display = "block";
    }
    resize();
}

function resize()
{
    if (window.innerHeight < 780)
    {
        document.getElementById("DetailsPanel").style.display = "none";
    }
    else
    {
        document.getElementById("DetailsPanel").style.display = "block";
    }
}


function browserSupportsMultilineFlex()
{
    var mlFlexSupported = false;

    var detect = document.createElement('div');

    mlFlexSupported |= ('flexWrap' in detect.style);
    mlFlexSupported |= ('msFlexWrap' in detect.style);
    mlFlexSupported |= ('MozFlexWrap' in detect.style);
    mlFlexSupported |= ('webkitFlexWrap' in detect.style);

    return mlFlexSupported;
}

function getBrowserInformation()
{
    var browserCheck, browserName, browserVersion;

    var UA = navigator.userAgent.toLowerCase();
    var index;

    if (document.documentMode)
    {
        browserCheck = "IE";
        browserName = "Internet Explorer";
        browserVersion = "" + document.documentMode;
    }
    else if (UA.indexOf('chrome') > -1)
    {
        index = UA.indexOf('chrome');
        browserCheck = "Chrome";
        browserName = "Google Chrome";
        browserVersion = "" + parseFloat('' + UA.substring(index + 7));
    }
    else if (UA.indexOf('firefox') > -1)
    {
        index = UA.indexOf('firefox');
        browserCheck = "Firefox";
        browserName = "Mozilla Firefox";
        browserVersion = "" + parseFloat('' + UA.substring(index + 8));
    }
    else if (UA.indexOf('minefield') > -1)
    {
        index = UA.indexOf('minefield');
        browserCheck = "Firefox";
        browserName = "Mozilla Firefox Minefield";
        browserVersion = "" + parseFloat('' + UA.substring(index + 10));
    }
    else if (UA.indexOf('opera') > -1)
    {
        browserCheck = "Opera";
        browserName = "Opera";
        browserVersion = "";
    }
    else if (UA.indexOf('safari') > -1)
    {
        index = UA.indexOf('safari');
        browserCheck = "Safari";
        browserName = "Apple Safari";
        browserVersion = "" + parseFloat('' + UA.substring(index + 7));
    }

    return { browserName: browserName, browserVersion: browserVersion };
}

window.requestAnimFrame = (
    function ()
    {
        return window.requestAnimationFrame ||
               window.msRequestAnimationFrame ||
               window.mozRequestAnimationFrame ||
               window.oRequestAnimationFrame ||
               window.webkitRequestAnimationFrame ||
               function (callback) { window.setTimeout(callback, 1000 / 60); };
    }
)();