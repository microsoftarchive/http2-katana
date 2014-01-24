function Timer()
{
    this.startTime = undefined;

    this.update = function ()
    {
        var currentTime = this.getCurrentTime();
        document.getElementById("timer").innerHTML = ((currentTime - this.startTime) / 1000).toFixed(2);
    };

    this.start = function ()
    {
        this.startTime = this.getCurrentTime();
    }

    this.getCurrentTime = function ()
    {
        if (window.performance && typeof window.performance.now !== "undefined")
        {
            return performance.now();
        }
        else
        {
            return new Date().getTime();
        };
    }
}