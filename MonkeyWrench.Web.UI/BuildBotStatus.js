var host_id = null;
var table_releases = null;

function selectBuildBotRelease(version, hid, tblReleases) {
    table_releases = tblReleases;
    host_id = hid;
    document.getElementById(tblReleases).style = "display: block";
}

function executeBuildBotSelection(release_id) {
    document.getElementById(table_releases).style = "display: none";
    window.location = window.location.pathname + "?action=select-release&release_id=" + release_id + "&host_id=" + host_id;
}