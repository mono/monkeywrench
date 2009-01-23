function addLane() {
    window.location = window.location.pathname + "?action=add&lane=" + document.getElementById("txtLane").value;
}
function cloneLane(id, lane) {
    var name = prompt("Enter the name of the new lane:", lane);
    if (name != null && name != "")
        window.location = window.location.pathname + "?action=clone&lane_id=" + id + "&lane=" + name;
}