
function addCommand(lane_id, sequence) {
    window.location = window.location.pathname + "?lane_id=" + lane_id + "&action=addCommand&command=" + document.getElementById("txtCreateCommand_name").value + "&sequence=" + sequence;
}

function editCommandFilename(lane_id, command_id, saved, def) {
    var filename = prompt("Set the new filename:", def);
    if (filename != null && filename != "" && filename != def) {
        window.location = window.location.pathname + "?lane_id=" + lane_id + "&action=editCommandFilename&command_id=" + command_id + "&filename=" + filename;
    }
}

function editCommandArguments(lane_id, command_id, saved, def) {
    var arguments = prompt("Set the new arguments:", def);
    if (arguments != null && arguments != "" && arguments != def) {
        window.location = window.location.pathname + "?lane_id=" + lane_id + "&action=editCommandArguments&command_id=" + command_id + "&arguments=" + arguments;
    }
}

function editCommandSequence(lane_id, command_id, saved, def) {
    var sequence = prompt("Set the new sequence (must be a positive number):", def);
    if (sequence != null && sequence != "" && sequence != def) {
        window.location = window.location.pathname + "?lane_id=" + lane_id + "&action=editCommandSequence&command_id=" + command_id + "&sequence=" + sequence;
    }
}

function editCommandTimeout(lane_id, command_id, saved, def) {
    var timeout = prompt("Set the new timeout (in minutes):", def);
    if (arguments != null && arguments != "" && arguments != def) {
        window.location = window.location.pathname + "?lane_id=" + lane_id + "&action=editCommandTimeout&command_id=" + command_id + "&timeout=" + timeout;
    }
}

function createFile(lane_id) {
    window.location = window.location.pathname + "?lane_id=" + lane_id + "&action=createFile&filename=" + document.getElementById("txtCreateFileName").value;
}

function addFile(lane_id) {
    window.location = window.location.pathname + "?lane_id=" + lane_id + "&action=addFile&lanefile_id=" + document.getElementById("cmbExistingFiles").value;
}

function addHost(lane_id) {
    window.location = window.location.pathname + "?lane_id=" + lane_id + "&action=addHost&host_id=" + document.getElementById("lstHosts").value;
}

function addDependency(lane_id) {
    window.location = window.location.pathname + "?lane_id=" + lane_id + "&action=addDependency&dependent_lane_id=" + document.getElementById("lstDependentLanes").value + "&condition=" + document.getElementById("lstDependencyConditions").value;

}

function editDependencyFilename(lane_id, lanedependency_id, filename) {
    var fn = prompt("Set the new filename:", filename);
    if (fn != null && fn != "" && fn != filename) {
        window.location = window.location.pathname + "?lane_id=" + lane_id + "&action=editDependencyFilename&lanedependency_id=" + lanedependency_id + "&filename=" + fn;
    }
}

function deleteDependency(lane_id, dependency_id) {
    window.location = window.location.pathname + "?lane_id=" + lane_id + "&action=deleteDependency&dependency_id=" + dependency_id;
}