function ImageManager()
{
    this.sourceCanvas = document.getElementById("sourceCanvas");
    this.sourceContext = sourceCanvas.getContext("2d");
    this.currentPixelIndex = 0;
    this.imagesLoaded = 0;
    this.runAllMode = false;

    this.loadImages = function ()
    {
        for (var i = 0; i < context.templates.length; i++)
        {
            var image = new Image();
            image.templateIndex = i;

            var self = this;
            image.addEventListener("load", function (e) { self.imageLoaded.call(self, e); }, false);
            image.src = context.templates[i].imagePath;
        }

    }

    this.imageLoaded = function imageLoaded(e)
    {
        var image = e.target;
        var templateIndex = image.templateIndex;

        var sortedPixels = [];
        var imageData = this.getImageData(image);

        for (var i = 0; i < imageData.length; i += 4)
        {
            var n = i / 4;
            var r = Math.floor(n / context.cols);
            var c = n % context.cols;

            var colorTone = (imageData[i] + imageData[i + 1] + imageData[i + 2]) / 3;
            var luminance = this.convertRGB2HSL(imageData[i], imageData[i + 1], imageData[i + 2])[2];

            if (colorTone !== 0)
            {
                sortedPixels.push({
                    char: context.templates[templateIndex].char,
                    colorTone: colorTone,
                    luminance: luminance,
                    colorString: this.getColorString(imageData[i + 0],
                                                     imageData[i + 1],
                                                     imageData[i + 2],
                                                     imageData[i + 3]),
                    domID: r * context.cols + c
                });
            }
        }

        sortedPixels.sort(function (a, b)
        {
            return b.luminance - a.luminance;
        });

        context.templates[templateIndex].sortedPixels = sortedPixels;
        this.imagesLoaded++;

        if (this.imagesLoaded === context.templates.length)
        {
            document.body.removeChild(this.sourceCanvas);
            setButtonVisibility("block");
        }
    }

    this.getImageData = function (image)
    {
        this.sourceContext.clearRect(0, 0, context.cols, context.rows);
        this.sourceContext.drawImage(image, 0, 0, context.cols, context.rows);

        return this.sourceContext.getImageData(0, 0, context.cols, context.rows).data;
    }

    this.getColorString = function (r, g, b, a)
    {
        return "rgba(" + r + ", " + g + ", " + b + ", " + a + ")";
    }

    this.renderLoop = function ()
    {
        timer.update();

        var sortedPixels = context.templates[activeTemplate].sortedPixels;

        for (var i = 0; i < 10 && this.currentPixelIndex < sortedPixels.length; i++)
        {
            var currentPixel = sortedPixels[this.currentPixelIndex];

            spans[currentPixel.domID].innerText = currentPixel.char;
            boxes[currentPixel.domID].style.backgroundColor = currentPixel.colorString;

            this.currentPixelIndex++;
        }

        var that = this;

        if (this.currentPixelIndex < sortedPixels.length)
        {
            window.requestAnimFrame(function () { that.renderLoop(); });
        }
        else
        {
            if (this.runAllMode && activeTemplate < (context.templates.length) - 1)
            {
                activeTemplate++;
                benchmarkType.innerText = context.templates[activeTemplate].benchmarkName;
                resetBoard();
                window.requestAnimFrame(function () { that.renderLoop(); });
            }
            else
            {
                setButtonVisibility("block");
            }
        }
    }

    this.convertRGB2HSL = function (r, g, b)
    {
        var r1 = r / 255;
        var g1 = g / 255;
        var b1 = b / 255;

        var maxColor = Math.max(r1, g1, b1);
        var minColor = Math.min(r1, g1, b1);

        //Calculate L:
        var L = (maxColor + minColor) / 2;
        var S = 0;
        var H = 0;

        if (maxColor != minColor)
        {
            //Calculate S:       
            if (L < 0.5)
            {
                S = (maxColor - minColor) / (maxColor + minColor);
            } else
            {
                S = (maxColor - minColor) / (2.0 - maxColor - minColor);
            }

            //Calculate H:       
            if (r1 == maxColor)
            {
                H = (g1 - b1) / (maxColor - minColor);
            } else if (g1 == maxColor)
            {
                H = 2.0 + (b1 - r1) / (maxColor - minColor);
            } else
            {
                H = 4.0 + (r1 - g1) / (maxColor - minColor);
            }
        }

        L = L * 100;
        S = S * 100;
        H = H * 60;

        if (H < 0)
        {
            H += 360;
        }
        return [H, S, L];
    }
}