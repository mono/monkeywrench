
var iframe_to_resize = null;

function pageY(elem)
{
    return elem.offsetParent ? (elem.offsetTop + pageY(elem.offsetParent)) : elem.offsetTop;
}

function resizeRightContent()
{
    resizeToFill(document.getElementById("right-content"));
    if (iframe_to_resize != null)
        resizeToFillIFrame(iframe_to_resize);
}

function resizeToFillIFrame(obj) {
    // there are 2 pixels at the bottom of the iframe I can't remove
    iframe_to_resize = obj;
    var height = document.documentElement.clientHeight;
    height -= pageY(obj) + 4;
    height = (height < 0) ? 0 : height;
    obj.style.height = height + 'px';
   // alert(obj.style.height);
}

function resizeToFill (obj) {
    var height = document.documentElement.clientHeight;
    height -= pageY(obj);
    height = (height < 0) ? 0 : height;
    obj.style.height = height + 'px';
}

function switchVisibility (varargs) {
    for (i = 0; i < arguments.length; i++) {
        var obj = document.getElementById(arguments [i]);
        if (obj.style.display == "block" || obj.style.display == null || obj.style.display == "") {
            obj.style.display = "none";
        } else if (obj.style.display == "none") {
            obj.style.display = "block";
        }
    }
}

window.onresize = resizeRightContent;
window.onload = resizeRightContent;