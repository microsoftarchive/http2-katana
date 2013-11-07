window.addEventListener("keydown", onKeyDown, false);

function onKeyDown(e)
{

    if (e.keyCode)
    {
        key = e.keyCode;
    } else if (document.all)
    {
        key = event.keyCode;
    } else
    {
        key = ev.charCode;
    }

    switch (key)
    {

        case 13: // RETURN: Start Benchmark
            if (benchmarkButton.style.display !== "none")
            {
                startBenchmark();
            }
            break;

        case 66: // B: SteveB Benchmark
            window.location.href = window.location.protocol + "//" + window.location.host + window.location.pathname +  "?SteveB";
            break;

        case 68: // D: Dean Benchmark
            window.location.href = window.location.protocol + "//" + window.location.host + window.location.pathname + "?Dean";
            break;

        case 82: // R: Reset Demo
            window.location.reload();
            break;
    }

}