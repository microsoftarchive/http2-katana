var audio;
var context;
var activeTemplate = 0;

var boxes = [];
var spans = [];

var timer = new Timer();
var imageManager = new ImageManager();

var lightBox = document.getElementById("lightBox");
var benchmarkCombo = document.getElementById("selectBenchmark");
var benchmarkButton = document.getElementById("benchmarkButton");
var benchmarkType = document.getElementById("benchmarkType");

benchmarkCombo.addEventListener("change", selectionChanged);
benchmarkButton.addEventListener("click", startBenchmark);

window.addEventListener("load", load);

function load()
{
    checkBrowser();
    startAudio();
    loadTemplates();
    imageManager.loadImages();
    resetBoard();
}

function startAudio()
{
    audio = new Audio('Audio/LiteBrite.mp3');
    audio.loop = true;

    if (audio.play)
    {
        audio.play();
    }
}

function loadTemplates()
{
    var templateId = 11; //default

    if (window.location.search && window.location.search.length > 1)
    {
        templateId = window.location.search.substring(1);
    }

    var xhr = new XMLHttpRequest();
    xhr.open("GET", "Templates/" + templateId + ".js", false);
    xhr.send();

    context = JSON.parse(xhr.responseText);

    updateComboChoices();
}

function updateComboChoices()
{
    var options = "";

    for (var i = 0; i < context.templates.length; i++)
    {
        options += "<option value='" + i + "' class='info'>" + context.templates[i].displayName + "</option>";
    }
    options += "<option value='" + i + "' class='info'>All</option>"
    benchmarkCombo.innerHTML = options;

    if (context.defaultSelection)
    {
        benchmarkCombo.selectedIndex = context.defaultSelection;
    }

    selectionChanged();
}

function resetBoard()
{
    lightBox.style.opacity = 0.0;
    lightBox.style.width = ((context.cols) * context.BOX_WIDTH) + 4 + 'px';

    imageManager.currentPixelIndex = 0;
    

    var newContent = "";
    var id = 0;
    var startVal = "&nbsp;";

    for (var r = 0; r < context.rows; r++)
    {
        if (r % 2 === 1)
        {
            newContent += "<div class='inset'></div><div class='inset'></div>";
        }
        for (var c = 0; c < context.cols; c++)
        {
            newContent += '<div id="box' + id + '" class="byteBox"><span id="span' + id + '" class="textStyle">' + startVal + '</span></div>';
            id++;
        }
    }

    lightBox.innerHTML = newContent;

    numberBoxes();

    lightBox.style.opacity = 1.0;
}

function numberBoxes()
{
    var id = 0;
    for (var r = 0; r < context.rows; r++)
    {
        for (var c = 0; c < context.cols; c++)
        {
            boxes[id] = document.getElementById("box" + id);
            spans[id] = document.getElementById("span" + id);
            id++;
        }
    }
}

function selectionChanged()
{
    activeTemplate = benchmarkCombo.selectedIndex;
    imageManager.runAllMode = (activeTemplate === context.templates.length);

    if (imageManager.runAllMode)
    {
        benchmarkType.innerText = context.templates[0].benchmarkName;
    }
    else
    {
        benchmarkType.innerText = context.templates[activeTemplate].benchmarkName;
    }
}

function startBenchmark()
{
    if (timer.startTime !== undefined)
    {
        resetBoard();
    }
    setButtonVisibility("none");

    timer.start();
    if (imageManager.runAllMode)
    {
        activeTemplate = 0;
    }
    imageManager.renderLoop(imageManager);
}

function setButtonVisibility(newDisplayValue)
{
    benchmarkButton.style.display = newDisplayValue;
    benchmarkCombo.style.display = newDisplayValue;
}