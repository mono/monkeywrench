function clearRevisions() {
    try {
        var div = document.getElementById("buildtable");
        var table;
        var tbody;
        var tr;
        var td;
        var names = "";

        table = div.firstChild;
        tbody = table.lastChild;
        tr = tbody.firstChild;

        do {

            td = tr.childNodes[3];
            tr = tr.nextSibling;

            if (td != null && td.firstChild != null && td.firstChild.localName == "INPUT" && td.firstChild.checked)
                names += td.firstChild.name + ";";

            if (tr == null)
                break;
        } while (true);
        alert(names);
    } catch (e) {
        alert(e);
    }
}