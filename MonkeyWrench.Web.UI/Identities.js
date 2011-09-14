function changeircservers (id, current) {
    var servers = prompt("Enter servers:", current);
    if (servers != null && servers != "" && servers != current)
        window.location = window.location.pathname + "?action=changeircservers&id=" + id + "&servers=" + encodeURIComponent(servers);
}

function changeircname(id, current) {
    var newvalue = prompt("Enter name:", current);
    if (newvalue != null && newvalue != "" && newvalue != current)
        window.location = window.location.pathname + "?action=changeircname&id=" + id + "&name=" + encodeURIComponent(newvalue);
}

function changeircchannels(id, current) {
    var newvalue = prompt("Enter channels:", current);
    if (newvalue != null && newvalue != "" && newvalue != current)
        window.location = window.location.pathname + "?action=changeircchannels&id=" + id + "&channels=" + encodeURIComponent(newvalue);
}

function changeircnicks(id, current) {
    var newvalue = prompt("Enter nicks:", current);
    if (newvalue != null && newvalue != "" && newvalue != current)
        window.location = window.location.pathname + "?action=changeircnicks&id=" + id + "&nicks=" + encodeURIComponent(newvalue);
}

function changeircpassword(id, current) {
    var newvalue = prompt("Enter password:", current);
    if (newvalue != null && newvalue != "" && newvalue != current)
        window.location = window.location.pathname + "?action=changeircpassword&id=" + id + "&password=" + encodeURIComponent(newvalue);
}

