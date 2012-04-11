function confirmViewLaneAction(url, action) {
    var msg = "Are you sure you want to " + action + " the work while there is a bot working on this revision? " +
    "It may confuse the bot and require manual intervention to recover it. " +
    "The recommended way is to either wait until all work is done, or abort (and then wait until a step turns from 'aborted' to 'failed', " +
    "since that's when you know the bot has processed the abort request).";
    var rsp = confirm(msg);
    if (rsp)
        window.location = url;
}

