function clearRevisions(lane_id, host_id) {
    try {
        var td;
        var names = "";
        var chk_id = 0;

        do {
            td = document.getElementById("id_revision_chk_" + chk_id);
            chk_id++;

            if (td == null)
                break;            
            
            if (td.localName == "INPUT" && td.checked)
                names += td.name + ";";
        } while (true);
        window.location = window.location.pathname + "?lane_id=" + lane_id + "&host_id=" + host_id + "&action=clearrevisions&revisions=" + names;
    } catch (e) {
        alert(e);
    }
}