
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

