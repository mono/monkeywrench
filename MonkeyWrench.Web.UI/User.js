function adduseremail(selflink) {
    var email = prompt("Enter email:");
    if (email != null && email != "")
        window.location = selflink + "&action=addemail&email=" + encodeURIComponent(email);
}