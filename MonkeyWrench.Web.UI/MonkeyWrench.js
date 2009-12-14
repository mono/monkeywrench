
function editEnvironmentVariable(lane_id, host_id, old_value, id, name) {
    var new_value = prompt("Enter the new value of the environment variable", old_value);
    if (new_value != old_value && new_value != null && new_value != undefined) {
        window.location = window.location.pathname + "?host_id=" + host_id + "&lane_id=" + lane_id + "&action=editEnvironmentVariableValue&id=" + id + "&value=" + encodeURIComponent(new_value) + "&name=" + encodeURIComponent(name);
    }
}