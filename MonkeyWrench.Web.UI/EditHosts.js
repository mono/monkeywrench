function addHost() {
    window.location = window.location.pathname + "?action=add&host=" + document.getElementById("txtHost").value;
}